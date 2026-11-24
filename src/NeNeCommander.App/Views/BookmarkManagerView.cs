using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Settings;
using NeNeCommander.Presentation.WinUI.Bookmarks;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.App.Views;

/// <summary>
/// Renders the bookmark manager on its transparent scrim, forwards typed bookmark actions, and
/// owns the scrim: a tap outside the panel takes the existing <c>Escape</c> route once, a tap inside
/// does nothing (ADR-0080).
/// </summary>
internal sealed class BookmarkManagerView
{
    private readonly Grid _root;
    private readonly ViewTextResources _resources;
    private readonly Action<UserIntent> _forward;
    private readonly Action _restoreFileListFocus;
    private readonly TextBlock _status;
    private readonly TextBlock _saveStatus;
    private readonly Border _warning;
    private readonly TextBlock _warningText;
    private readonly Button _retryNavigation;
    private readonly Button _cancelNavigation;
    private readonly BookmarkBrowseView _browse;
    private readonly BookmarkDetailsView _details;
    private readonly BookmarkKeyboardView _keyboard;
    private readonly BookmarkInputOwnership _input;
    private BookmarkEditorDetails? _renderedDetails;
    private Type? _renderedStateType;
    private bool _rendering;

    internal BookmarkManagerView(Grid root, ViewTextResources resources, Action<UserIntent> forward, Action restoreFileListFocus)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(forward);
        ArgumentNullException.ThrowIfNull(restoreFileListFocus);
        _root = root;
        _resources = resources;
        _forward = forward;
        _restoreFileListFocus = restoreFileListFocus;
        _status = Find<TextBlock>("BookmarkStatus");
        _saveStatus = Find<TextBlock>("BookmarkSaveStatus");
        _warning = Find<Border>("BookmarkWarning");
        _warningText = Find<TextBlock>("BookmarkWarningText");
        _retryNavigation = Find<Button>("BookmarkNavigationRetry");
        _cancelNavigation = Find<Button>("BookmarkNavigationCancel");
        _browse = new BookmarkBrowseView(root, resources, forward, () => _rendering);
        _details = new BookmarkDetailsView(root, resources, forward, () => _rendering);
        _keyboard = new BookmarkKeyboardView(_browse, Find<ItemsControl>("BookmarkBrowseFooter"), forward);
        _input = new BookmarkInputOwnership(root, CancelPendingFocus);
        _retryNavigation.Click += (_, _) => _browse.NavigateSelected();
        _cancelNavigation.Click += (_, _) => Submit(BookmarkEditorAction.Cancel);
        Find<Border>("BookmarkManagerSurface").Tapped += OnSurfaceTapped;
        _root.Tapped += OnScrimTapped;
        _root.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(OnPointerPressed), true);
    }

    internal bool IsOpen => _root.Visibility == Visibility.Visible;

    internal bool IsComposing => IsOpen && _input.IsComposing;

    internal bool HasOpenSelector => IsOpen && _input.HasOpenSelector;

    /// <summary>Preserves the current field after the key map yields actual input to it.</summary>
    internal void ObserveNativeInput()
    {
        if (IsOpen)
        {
            _input.ObserveNativeInput();
        }
    }

    /// <summary>Lets later native focus navigation supersede queued focus and guards pending confirmation.</summary>
    internal bool ObserveInput(KeyboardInput input)
    {
        if (!IsOpen)
        {
            return false;
        }
        if (input.Key == KeyboardKey.Tab && input.RepeatState == KeyRepeatState.Initial)
        {
            CancelPendingFocus();
        }
        return input.Key == KeyboardKey.Enter && _details.IsFocusPending;
    }

    /// <summary>
    /// Gets the browse, search or draft context the manager owns now; confirmation and navigation
    /// failures return <see langword="null"/> so the shared modal context applies.
    /// </summary>
    internal KeyboardContext? OwnedKeyboardContext =>
        IsOpen && _renderedDetails is BookmarkEditorDetails details ? _keyboard.ContextFor(details) : null;

    internal void Render(SettingsSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        BookmarkManagerPresentation presentation = BookmarkManagerPresenter.Present(
            snapshot,
            _resources.GetString("BookmarkAllCategories"),
            _resources.GetString("BookmarkUncategorized"));
        bool stateChanged = _renderedStateType != snapshot.BookmarksEditor.GetType();
        _rendering = true;
        if (!presentation.IsOpen && IsOpen)
        {
            _restoreFileListFocus();
        }
        _root.Visibility = presentation.IsOpen ? Visibility.Visible : Visibility.Collapsed;
        _browse.Render(presentation);
        RenderStatus(_status, presentation.Details.StatusResourceKey);
        _details.Render(presentation.Details, _renderedStateType, snapshot.BookmarksEditor.GetType());
        RenderPersistence(presentation.Persistence);
        _renderedDetails = presentation.Details;
        _rendering = false;
        if (presentation.IsOpen && stateChanged)
        {
            Focus(presentation.Details.InitialFocus);
        }
        _renderedStateType = snapshot.BookmarksEditor.GetType();
    }

    /// <summary>
    /// Applies one action the bookmark-manager key map produced, unless a bookmark field is
    /// composing text through an input method, in which case the key stays with that field.
    /// </summary>
    /// <param name="action">Mapped action.</param>
    /// <returns>Whether the manager handled the key.</returns>
    internal bool Forward(BookmarkManagerKeyAction action)
    {
        if (IsComposing)
        {
            return false;
        }
        _keyboard.Forward(action);
        return true;
    }

    /// <summary>Applies the action of one pressed key hint button.</summary>
    /// <param name="sender">Hint button whose data context is the pressed hint.</param>
    internal void Invoke(object sender)
    {
        if (sender is FrameworkElement { DataContext: BookmarkManagerKeyHint hint })
        {
            _keyboard.Invoke(hint);
        }
    }

    private void RenderPersistence(BookmarkPersistencePresentation persistence)
    {
        RenderStatus(_saveStatus, persistence.SaveStatus.ResourceKey);
        if (persistence.Warning.IsVisible)
        {
            _warning.Visibility = Visibility.Visible;
            RenderStatus(_warningText, persistence.Warning.ResourceKey);
            return;
        }
        _warning.Visibility = Visibility.Collapsed;
        _warningText.Text = string.Empty;
    }

    private void RenderStatus(TextBlock control, string resourceKey)
    {
        string text = _resources.GetString(resourceKey);
        if (control.Text == text)
        {
            return;
        }
        control.Text = text;
        if (IsOpen && text.Length > 0)
        {
            FrameworkElementAutomationPeer.FromElement(control)?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
        }
    }

    private void Focus(BookmarkManagerFocusTarget target)
    {
        if (_details.Focus(target))
        {
            return;
        }
        if (target == BookmarkManagerFocusTarget.RetryNavigation)
        {
            _ = _retryNavigation.Focus(FocusState.Programmatic);
            return;
        }
        if (target == BookmarkManagerFocusTarget.BookmarkList)
        {
            _keyboard.FocusBookmarkList();
            return;
        }
        _browse.FocusSearch();
    }

    private void OnScrimTapped(object sender, TappedRoutedEventArgs args)
    {
        _ = sender;
        _ = args;
        if (IsOpen)
        {
            _forward(UserIntent.Escape);
        }
    }

    private void OnSurfaceTapped(object sender, TappedRoutedEventArgs args)
    {
        _ = sender;
        args.Handled = true;
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs args)
    {
        _ = sender;
        _ = args;
        CancelPendingFocus();
    }

    private void CancelPendingFocus()
    {
        _browse.CancelPendingFocus();
        _details.CancelPendingFocus();
    }

    private void Submit(BookmarkEditorAction action)
    {
        _forward(UserIntent.ManageBookmarks(action));
    }

    private T Find<T>(string name)
        where T : DependencyObject
    {
        return _root.FindName(name) is T control
            ? control
            : throw new InvalidOperationException($"Bookmark manager control '{name}' is missing.");
    }
}
