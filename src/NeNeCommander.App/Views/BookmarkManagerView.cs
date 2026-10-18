using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Settings;
using NeNeCommander.Presentation.WinUI.Bookmarks;

namespace NeNeCommander.App.Views;

/// <summary>Renders the BookmarkManagerView controls and forwards typed bookmark actions.</summary>
internal sealed class BookmarkManagerView
{
    private readonly Grid _root;
    private readonly ResourceLoader _resources;
    private readonly Action<UserIntent> _forward;
    private readonly TextBlock _status;
    private readonly TextBlock _saveStatus;
    private readonly Border _warning;
    private readonly TextBlock _warningText;
    private readonly Button _retryNavigation;
    private readonly Button _cancelNavigation;
    private readonly BookmarkBrowseView _browse;
    private readonly BookmarkDetailsView _details;
    private Type? _renderedStateType;
    private bool _rendering;

    internal BookmarkManagerView(Grid root, ResourceLoader resources, Action<UserIntent> forward)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(forward);
        _root = root;
        _resources = resources;
        _forward = forward;
        _status = Find<TextBlock>("BookmarkStatus");
        _saveStatus = Find<TextBlock>("BookmarkSaveStatus");
        _warning = Find<Border>("BookmarkWarning");
        _warningText = Find<TextBlock>("BookmarkWarningText");
        _retryNavigation = Find<Button>("BookmarkNavigationRetry");
        _cancelNavigation = Find<Button>("BookmarkNavigationCancel");
        _browse = new BookmarkBrowseView(root, resources, forward, () => _rendering);
        _details = new BookmarkDetailsView(root, resources, forward, () => _rendering);
        _retryNavigation.Click += (_, _) => _browse.NavigateSelected();
        _cancelNavigation.Click += (_, _) => Submit(BookmarkEditorAction.Cancel);
    }

    internal bool IsOpen => _root.Visibility == Visibility.Visible;

    internal void Render(SettingsSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        BookmarkManagerPresentation presentation = BookmarkManagerPresenter.Present(
            snapshot,
            _resources.GetString("BookmarkAllCategories"),
            _resources.GetString("BookmarkUncategorized"));
        bool stateChanged = _renderedStateType != snapshot.BookmarksEditor.GetType();
        _rendering = true;
        _root.Visibility = presentation.IsOpen ? Visibility.Visible : Visibility.Collapsed;
        _browse.Render(presentation);
        _status.Text = _resources.GetString(presentation.Details.StatusResourceKey);
        _details.Render(presentation.Details, _renderedStateType, snapshot.BookmarksEditor.GetType());
        RenderPersistence(presentation.Persistence);
        _rendering = false;
        if (presentation.IsOpen && stateChanged)
        {
            Focus(presentation.Details.InitialFocus);
        }
        _renderedStateType = snapshot.BookmarksEditor.GetType();
    }

    private void RenderPersistence(BookmarkPersistencePresentation persistence)
    {
        _saveStatus.Text = _resources.GetString(persistence.SaveStatus.ResourceKey);
        if (persistence.Warning.IsVisible)
        {
            _warning.Visibility = Visibility.Visible;
            _warningText.Text = _resources.GetString(persistence.Warning.ResourceKey);
            return;
        }
        _warning.Visibility = Visibility.Collapsed;
        _warningText.Text = string.Empty;
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
        _browse.FocusSearch();
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
