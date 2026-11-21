using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.ApplicationModel.Resources;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.App.Views;

/// <summary>Renders the sole address modal and qualifies native text and dismissal with its rendered state.</summary>
internal sealed class AddressEditorView
{
    private readonly Grid _overlay;
    private readonly ResourceLoader _resources;
    private readonly Action<UserIntent> _forward;
    private readonly Action<PaneSide> _focusFileList;
    private readonly TextBox _input;
    private readonly TextBlock _status;
    private AddressEditorPresentation? _rendered;

    internal AddressEditorView(Grid overlay, ResourceLoader resources, Action<UserIntent> forward, Action<PaneSide> focusFileList)
    {
        ArgumentNullException.ThrowIfNull(overlay);
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(forward);
        ArgumentNullException.ThrowIfNull(focusFileList);
        _overlay = overlay;
        _resources = resources;
        _forward = forward;
        _focusFileList = focusFileList;
        _input = Find<TextBox>("AddressEditorInput");
        _status = Find<TextBlock>("AddressEditorStatus");
        Find<ItemsControl>("AddressEditorKeyHints").ItemsSource =
            KeyHintPresenter.Present(KeyboardContext.AddressEntry, KeyHintVisibility.Shown);
        _input.TextCompositionStarted += OnCompositionStarted;
        _input.TextCompositionEnded += OnCompositionEnded;
        Find<Border>("AddressEditorModal").Tapped += OnSurfaceTapped;
        _overlay.Tapped += OnScrimTapped;
    }

    internal bool IsOpen => _rendered?.EditingSide is not null;

    internal bool IsComposing { get; private set; }

    internal void Render(AddressEditorState state)
    {
        AddressEditorPresentation presentation = AddressEditorPresenter.Present(state, _rendered);
        if (ReferenceEquals(presentation, _rendered))
        {
            return;
        }
        _rendered = presentation;
        if (!IsOpen)
        {
            IsComposing = false;
            _overlay.Visibility = Visibility.Collapsed;
            if (presentation.FileListFocusSide is PaneSide side)
            {
                _focusFileList(side);
            }
            return;
        }
        _overlay.Visibility = Visibility.Visible;
        _status.Text = presentation.Status is PaneStatus status ? _resources.GetString(status.ResourceKey) : string.Empty;
        _status.Visibility = presentation.Status is null ? Visibility.Collapsed : Visibility.Visible;
        if (presentation.ReplacementText is string text)
        {
            _input.Text = text;
        }
        _ = _input.Focus(FocusState.Programmatic);
        if (presentation.SelectAll)
        {
            _input.SelectAll();
        }
    }

    internal UserIntent Qualify(UserIntent intent)
    {
        return _rendered is AddressEditorPresentation { EditingSide: not null } presentation
            ? intent == UserIntent.Confirm
                ? UserIntent.SubmitAddress(presentation.SourceState, _input.Text)
                : intent == UserIntent.Escape ? UserIntent.CancelAddress(presentation.SourceState) : intent
            : intent;
    }

    internal void FocusAndSelectAll()
    {
        _ = _input.Focus(FocusState.Programmatic);
        _input.SelectAll();
    }

    private void OnCompositionStarted(TextBox sender, TextCompositionStartedEventArgs args)
    {
        _ = sender;
        _ = args;
        IsComposing = true;
    }

    private void OnCompositionEnded(TextBox sender, TextCompositionEndedEventArgs args)
    {
        _ = sender;
        _ = args;
        IsComposing = false;
    }

    private void OnScrimTapped(object sender, TappedRoutedEventArgs args)
    {
        _ = sender;
        _ = args;
        if (IsOpen)
        {
            _forward(Qualify(UserIntent.Escape));
        }
    }

    private void OnSurfaceTapped(object sender, TappedRoutedEventArgs args)
    {
        _ = sender;
        args.Handled = true;
    }

    private T Find<T>(string name)
        where T : DependencyObject
    {
        return _overlay.FindName(name) is T control
            ? control
            : throw new InvalidOperationException($"Address editor control '{name}' is missing.");
    }
}
