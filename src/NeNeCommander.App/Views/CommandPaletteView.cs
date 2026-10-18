using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.ApplicationModel.Resources;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;
using NeNeCommander.Presentation.WinUI.Commands;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.App.Views;

/// <summary>Renders the existing CommandPaletteView controls and forwards their typed intents.</summary>
internal sealed class CommandPaletteView
{
    private readonly Grid _root;
    private readonly ResourceLoader _resources;
    private readonly Action<UserIntent> _forward;
    private readonly Action<PaneSide> _focusFileList;
    private CommandPaletteViewState? _commandPaletteView;
    private bool _renderingCommandPalette;
    private readonly Border _commandPaletteSurface;
    private readonly TextBox _commandPaletteSearch;
    private readonly ListView _commandPaletteCandidateList;
    private readonly TextBlock _commandPaletteContext;
    private readonly TextBlock _commandPaletteZeroResults;
    private readonly TextBlock _commandPaletteDetail;
    private readonly ItemsControl _commandPaletteKeyHints;

    internal CommandPaletteView(Grid root, ResourceLoader resources, Action<UserIntent> forward, Action<PaneSide> focusFileList)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(forward);
        ArgumentNullException.ThrowIfNull(focusFileList);
        _root = root;
        _resources = resources;
        _forward = forward;
        _focusFileList = focusFileList;
        _commandPaletteSurface = Find<Border>("CommandPaletteSurface");
        _commandPaletteSearch = Find<TextBox>("CommandPaletteSearch");
        _commandPaletteCandidateList = Find<ListView>("CommandPaletteCandidateList");
        _commandPaletteContext = Find<TextBlock>("CommandPaletteContext");
        _commandPaletteZeroResults = Find<TextBlock>("CommandPaletteZeroResults");
        _commandPaletteDetail = Find<TextBlock>("CommandPaletteDetail");
        _commandPaletteKeyHints = Find<ItemsControl>("CommandPaletteKeyHints");
        _commandPaletteKeyHints.ItemsSource = CommandPaletteKeyHintPresenter.Present();
        _root.Tapped += OnCommandPaletteScrimTapped;
        _commandPaletteSurface.Tapped += OnCommandPaletteSurfaceTapped;
        _commandPaletteSearch.TextChanged += OnCommandPaletteQueryChanged;
        _commandPaletteSearch.TextCompositionStarted += OnCommandPaletteCompositionStarted;
        _commandPaletteSearch.TextCompositionEnded += OnCommandPaletteCompositionEnded;
        _commandPaletteCandidateList.ItemClick += OnCommandPaletteCandidateClick;
        _commandPaletteCandidateList.SelectionChanged += OnCommandPaletteSelectionChanged;
    }

    internal bool IsComposing { get; private set; }

    internal void Render(CommanderSnapshot snapshot)
    {
        CommandPaletteState state = snapshot.Scopes.CommandPalette;
        if (state is not CommandPaletteOpen open)
        {
            bool wasOpen = _commandPaletteView is not null;
            _commandPaletteView = null;
            IsComposing = false;
            _root.Visibility = Visibility.Collapsed;
            if (wasOpen &&
                state is CommandPaletteClosed { FileListFocusSide: PaneSide focusSide })
            {
                _focusFileList(focusSide);
            }
            else if (wasOpen && PaletteExecutionLeavesFileListOwner(snapshot))
            {
                _focusFileList(snapshot.Panes.ActiveSide);
            }
            return;
        }

        bool opening = _commandPaletteView is null ||
            !ReferenceEquals(_commandPaletteView.SourceState, open);
        if (opening)
        {
            _commandPaletteView = CommandPalettePresenter.Present(open, _resources.GetString);
            _renderingCommandPalette = true;
            _commandPaletteSearch.Text = string.Empty;
            _renderingCommandPalette = false;
        }
        _root.Visibility = Visibility.Visible;
        RenderCommandPaletteView();
        if (opening)
        {
            _ = _commandPaletteSearch.Focus(FocusState.Programmatic);
        }
    }

    private static bool PaletteExecutionLeavesFileListOwner(CommanderSnapshot snapshot)
    {
        return snapshot.Settings.Editor == SettingsEditorState.Closed &&
            snapshot.Scopes.AddressEditor is AddressEditorClosed &&
            snapshot.Panes.Operation is not (
                OperationAwaitingConfirmation or OperationAwaitingName or OperationAwaitingConflict);
    }

    private void RenderCommandPaletteView()
    {
        if (_commandPaletteView is not CommandPaletteViewState view)
        {
            return;
        }
        _commandPaletteContext.Text = view.Context;
        _renderingCommandPalette = true;
        _commandPaletteCandidateList.ItemsSource = view.Rows;
        _commandPaletteCandidateList.SelectedItem = view.SelectedRow;
        _renderingCommandPalette = false;
        _commandPaletteZeroResults.Visibility = view.Rows.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
        _commandPaletteDetail.Text = view.SelectedRow?.Detail ?? string.Empty;
        if (view.SelectedRow is CommandPaletteRow selected)
        {
            _commandPaletteCandidateList.ScrollIntoView(selected);
        }
    }

    private void OnCommandPaletteQueryChanged(object sender, TextChangedEventArgs args)
    {
        _ = args;
        if (_renderingCommandPalette ||
            sender is not TextBox search ||
            _commandPaletteView is not CommandPaletteViewState view)
        {
            return;
        }
        view.UpdateQuery(search.Text);
        RenderCommandPaletteView();
    }

    private void OnCommandPaletteCompositionStarted(
        TextBox sender,
        TextCompositionStartedEventArgs args)
    {
        _ = sender;
        _ = args;
        IsComposing = true;
    }

    private void OnCommandPaletteCompositionEnded(
        TextBox sender,
        TextCompositionEndedEventArgs args)
    {
        _ = sender;
        _ = args;
        IsComposing = false;
    }

    private void OnCommandPaletteCandidateClick(object sender, ItemClickEventArgs args)
    {
        _ = sender;
        if (_commandPaletteView is not CommandPaletteViewState view ||
            args.ClickedItem is not CommandPaletteRow row ||
            !view.Select(row))
        {
            return;
        }
        RenderCommandPaletteView();
        if (row.IsAvailable)
        {
            _forward(UserIntent.SubmitCommand(view.SourceState, row.Intent));
        }
    }

    private void OnCommandPaletteSelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        _ = sender;
        if (_renderingCommandPalette ||
            _commandPaletteView is not CommandPaletteViewState view)
        {
            return;
        }
        if (args.AddedItems.Count == 1 &&
            args.AddedItems[0] is CommandPaletteRow row &&
            view.Select(row))
        {
            RenderCommandPaletteView();
            return;
        }
        RenderCommandPaletteView();
    }

    private void OnCommandPaletteScrimTapped(object sender, TappedRoutedEventArgs args)
    {
        _ = sender;
        _ = args;
        if (_commandPaletteView is CommandPaletteViewState view)
        {
            _forward(UserIntent.CancelCommandPalette(view.SourceState));
        }
    }

    private void OnCommandPaletteSurfaceTapped(object sender, TappedRoutedEventArgs args)
    {
        _ = sender;
        args.Handled = true;
    }

    internal bool Forward(CommandPaletteKeyAction action)
    {
        if (_commandPaletteView is not CommandPaletteViewState view)
        {
            return true;
        }
        if (action == CommandPaletteKeyAction.MovePrevious)
        {
            view.MovePrevious();
            RenderCommandPaletteView();
            return true;
        }
        if (action == CommandPaletteKeyAction.MoveNext)
        {
            view.MoveNext();
            RenderCommandPaletteView();
            return true;
        }
        if (action == CommandPaletteKeyAction.Cancel)
        {
            _forward(UserIntent.CancelCommandPalette(view.SourceState));
            return true;
        }
        if (action == CommandPaletteKeyAction.MoveFocus)
        {
            return MoveCommandPaletteFocus();
        }
        if (action == CommandPaletteKeyAction.Execute &&
            view.SelectedRow is CommandPaletteRow { IsAvailable: true } selected)
        {
            _forward(UserIntent.SubmitCommand(view.SourceState, selected.Intent));
        }
        return true;
    }

    private bool MoveCommandPaletteFocus()
    {
        if (_commandPaletteView is null)
        {
            return false;
        }
        object? focused = FocusManager.GetFocusedElement(_root.XamlRoot);
        _ = ReferenceEquals(focused, _commandPaletteSearch)
            ? _commandPaletteCandidateList.Focus(FocusState.Keyboard)
            : _commandPaletteSearch.Focus(FocusState.Keyboard);
        return true;
    }

    private T Find<T>(string name)
        where T : DependencyObject
    {
        return _root.FindName(name) is T control
            ? control
            : throw new InvalidOperationException($"Command palette control '{name}' is missing.");
    }
}
