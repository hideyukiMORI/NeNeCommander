using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using NeNeCommander.App.Input;
using NeNeCommander.App.Windowing;
using NeNeCommander.Application.Commands;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;
using NeNeCommander.Application.Windowing;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Presentation.WinUI.Commands;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Lifecycle;
using NeNeCommander.Presentation.WinUI.Locations;
using NeNeCommander.Presentation.WinUI.Panes;
using NeNeCommander.Presentation.WinUI.Settings;

namespace NeNeCommander.App.Views;

/// <summary>Hosts the design-neutral dual-pane shell, forwards typed keyboard intents, and renders progress as it is reported.</summary>
public sealed partial class CommanderWindow : Window, ICommanderProgressObserver
{
    private readonly FileSystemPath _initialLeftLocation;
    private readonly FileSystemPath _initialRightLocation;
    private readonly KeyboardIntentMapper _keyboardIntentMapper;
    private readonly PanePointerMapper _pointerMapper = new();
    private readonly CommanderSession _session;
    private readonly ViewTextResources _resources;
    private readonly AsyncWorkOwner _paneWork;
    private readonly BookmarkManagerView _bookmarkView;
    private readonly WindowAdjustmentView _windowAdjustmentView;
    private readonly AppWindowPlacementAdapter _placementAdapter;
    private readonly KeyboardHelpView _keyboardHelpView;
    private readonly SettingsEditorView _settingsView;
    private readonly SettingsThemePickerView _themePicker;
    private readonly CaptionBarView _captionBarView;
    private readonly CommandEntryView _commandEntryView;
    private readonly LocationsView _locationsView;
    private readonly CommandPaletteView _paletteView;
    private readonly CommandMenuView _contextView;
    private readonly OperationBarView _operationView;
    private readonly CommanderPaneView _leftPaneView;
    private readonly CommanderPaneView _rightPaneView;
    private readonly EntryMetadataFormat _metadataFormat;
    private readonly AddressEditorView _addressView;
    private readonly PaneFilterView _filterView;
    private KeyboardContext _operationContext = KeyboardContext.FileList;
    private DualPanePresentation? _presentation;
    private ColorScheme? _renderedScheme;
    private bool _stopping;

    /// <summary>Initializes the shell with the sole keyboard mapping and pane coordination mechanisms.</summary>
    /// <param name="dependencies">Complete host dependencies supplied by the composition root.</param>
    /// <param name="initialLeftLocation">Validated location read into the left pane when the shell loads.</param>
    /// <param name="initialRightLocation">Validated location read into the right pane when the shell loads.</param>
    public CommanderWindow(
        CommanderWindowDependencies dependencies,
        FileSystemPath initialLeftLocation,
        FileSystemPath initialRightLocation)
    {
        ArgumentNullException.ThrowIfNull(dependencies);
        ArgumentNullException.ThrowIfNull(initialLeftLocation);
        ArgumentNullException.ThrowIfNull(initialRightLocation);
        _keyboardIntentMapper = dependencies.KeyboardIntentMapper;
        _session = dependencies.Session;
        _initialLeftLocation = initialLeftLocation;
        _initialRightLocation = initialRightLocation;
        _paneWork = new AsyncWorkOwner(dependencies.DefectObserver);
        InitializeComponent();
        _resources = InitializeTextResources();
        _metadataFormat = EntryMetadataFormat.Create(dependencies.LocalZone, _resources.GetString);
        _bookmarkView = new BookmarkManagerView(BookmarkOverlay, _resources, ForwardIntent,
            () => FocusFileList(_session.Current.Panes.ActiveSide));
        _placementAdapter = new AppWindowPlacementAdapter(AppWindow, InputSurface);
        _windowAdjustmentView = new WindowAdjustmentView(
            WindowAdjustmentOverlay,
            _resources,
            new WindowAdjustmentRoute(dependencies.Session, _placementAdapter, dependencies.DefectObserver),
            FocusFileList);
        _locationsView = new LocationsView(LocationsOverlay, _resources, ForwardIntent, FocusFileList);
        _keyboardHelpView = new KeyboardHelpView(KeyboardHelpOverlay, _resources, dependencies.Session, FocusFileList);
        _settingsView = new SettingsEditorView(InputSurface, _resources, ForwardIntent,
            () => FocusFileList(_session.Current.Panes.ActiveSide));
        _themePicker = new SettingsThemePickerView(ThemePickerOverlay, _resources, ForwardIntent, RenderColorScheme);
        _captionBarView = CreateCaptionBar(dependencies.DefectObserver);
        _commandEntryView = CreateCommandEntry();
        _paletteView = new CommandPaletteView(CommandPaletteOverlay, _resources, ForwardIntent, FocusFileList);
        _addressView = new AddressEditorView(AddressEditorOverlay, _resources, ForwardIntent, FocusFileList);
        _filterView = new PaneFilterView(InputSurface, _resources, ForwardIntent, ReturnFilterFocus);
        _operationView = new OperationBarView(WindowFrame, _resources, ForwardIntent);
        _contextView = new CommandMenuView(InputSurface, _resources, ForwardIntent, ReturnPointerFocus);
        PanePointerBindings pointer = new(_pointerMapper, ForwardIntent, _contextView.Request);
        _leftPaneView = new CommanderPaneView(InputSurface, _resources, PaneSide.Left, pointer);
        _rightPaneView = new CommanderPaneView(InputSurface, _resources, PaneSide.Right, pointer);
        _renderedScheme = dependencies.Session.Current.Settings.Settings.Display.ColorScheme;
        _placementAdapter.Initialize(_session.Current.Settings.Settings.WindowPlacement);
    }

    private ViewTextResources InitializeTextResources()
    {
        ViewTextResources resources = (ViewTextResources)WindowFrame.Resources["ViewTextResources"];
        resources.ApplyLanguage(_session.Current.Settings.Settings.Display.InterfaceLanguage);
        WindowFrame.Language = resources.LanguageTag;
        return resources;
    }

    private CaptionBarView CreateCaptionBar(Action<Exception> defectObserver)
    {
        Title = _resources.GetString("CommanderWindowTitle");
        return new CaptionBarView(CaptionBar, _resources,
            new AppWindowCaptionAdapter(AppWindow, CaptionDragArea, CaptionButtons, defectObserver));
    }

    private CommandEntryView CreateCommandEntry()
    {
        CommandEntryView view = new(CommandEntryButtons, _resources, ForwardIntent);
        view.Render(CommandEntryPresenter.Present(_session.Current));
        return view;
    }

    /// <summary>Occurs when the host previews or displays an accepted scheme through the composition root.</summary>
    public event EventHandler<ColorSchemeChangedEventArgs>? ColorSchemeChanged;

    /// <inheritdoc />
    public void OperationProgressed(DualPaneSnapshot snapshot)
    {
        RenderPanes(snapshot, _session.Current.Settings.Settings.Display.KeyHintVisibility);
    }

    /// <inheritdoc />
    public void SettingsProgressed(SettingsSnapshot snapshot)
    {
        _ = snapshot;
        _ = DispatcherQueue.TryEnqueue(() => RenderSession(_session.Current));
    }

    private void OnLoaded(object _, RoutedEventArgs args)
    {
        if (_stopping)
        {
            return;
        }
        FocusFileList(_session.Current.Panes.ActiveSide);
        _ = _paneWork.TryStart(cancellationToken => RenderAfterAsync(LoadInitialLocationsAsync(cancellationToken)));
    }

    private void OnActivated(object _, WindowActivatedEventArgs args)
    {
        _pointerMapper.Reset();
        _captionBarView.RenderActivation(args.WindowActivationState);
        if (args.WindowActivationState != WindowActivationState.Deactivated)
        {
            _ = DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, FocusActiveFileListWhenIdle);
        }
    }

    private void OnKeyDown(object _, KeyRoutedEventArgs args)
    {
        _pointerMapper.Reset();
        if (_stopping)
        {
            args.Handled = true;
            return;
        }
        KeyboardContext context = GetKeyboardContext();
        // Blank-area clicks can leave the framework scroll host owning character delivery.
        if (FocusManager.GetFocusedElement(Content.XamlRoot) is ScrollViewer)
        {
            FocusActiveFileListWhenIdle();
        }
        KeyboardInput input = WinUiKeyboardInputTranslator.TranslateKey(args, context);
        bool consumesPendingFocus = _bookmarkView.ObserveInput(input);
        if (IsTextComposing || _bookmarkView.HasOpenSelector)
        {
            _keyboardIntentMapper.ObserveNativeInput(input);
            _bookmarkView.ObserveNativeInput(input);
            return;
        }
        KeyboardMappingOutcome outcome = _keyboardIntentMapper.Map(input);
        if (ConflictModal.Visibility == Visibility.Visible ||
            BookmarkOverlay.Visibility == Visibility.Visible)
        {
            outcome = KeyboardIntentMapper.DeferModalConfirmToNativeControl(outcome);
        }
        args.Handled = consumesPendingFocus || ForwardOutcome(outcome);
        if (!args.Handled &&
            (input.RepeatState == KeyRepeatState.Initial || input.Key != KeyboardKey.Other))
        {
            _bookmarkView.ObserveNativeInput(input);
        }
    }

    private void OnCharacterReceived(object _, CharacterReceivedRoutedEventArgs args)
    {
        _pointerMapper.Reset();
        if (_stopping)
        {
            args.Handled = true;
            return;
        }
        KeyboardInput input = WinUiKeyboardInputTranslator.TranslateCharacter(args, GetKeyboardContext());
        args.Handled = !IsTextComposing && ForwardOutcome(_keyboardIntentMapper.Map(input));
        if (!args.Handled)
        {
            _bookmarkView.ObserveNativeInput(input);
        }
    }

    private bool IsTextComposing => _paletteView.IsComposing || _bookmarkView.IsComposing || _addressView.IsComposing || _themePicker.IsComposing || _filterView.IsComposing;

    private async Task<CommanderSnapshot> LoadInitialLocationsAsync(CancellationToken cancellationToken)
    {
        _ = await _session.NavigateAsync(PaneSide.Left, _initialLeftLocation, cancellationToken);
        return await _session.NavigateAsync(PaneSide.Right, _initialRightLocation, cancellationToken);
    }

    /// <summary>
    /// Renders the snapshot the coordinator reports when its work completes. Expected failures
    /// arrive as closed activities, so the owned task faults only on a defect.
    /// </summary>
    private async Task RenderAfterAsync(Task<CommanderSnapshot> work)
    {
        RenderSession(_session.Current);
        CommanderSnapshot snapshot = await work;
        RenderSession(snapshot);
    }

    private async Task RenderIntentAsync(UserIntent intent, PaneFilterEditSubmission? leavingFilter, CancellationToken cancellationToken)
    {
        CommanderSnapshot before = _session.Current;
        if (leavingFilter is not null)
        {
            if (!ReferenceEquals(before.Scopes.NameFilter, leavingFilter.ExpectedState))
            {
                return;
            }
            before = await _session.HandleAsync(leavingFilter, this, cancellationToken);
            if (before.Scopes.NameFilter is not PaneFilterClosed ||
                !ReferenceEquals(before.Scopes.NameFilter, _session.Current.Scopes.NameFilter))
            {
                return;
            }
        }
        Task<CommanderSnapshot> work = _session.HandleAsync(intent, this, cancellationToken);
        RenderSession(_session.Current);
        CommanderSnapshot after = await work;
        PaneSide? focusSide = _pointerMapper.ObserveResult(intent, before, after);
        RenderSession(after);
        ReturnPointerFocus(focusSide);
    }

    private void ReturnFilterFocus(PaneFilterClosed expected)
    {
        FocusFileList(PaneFilterPresenter.FocusTarget(expected, _session.Current, GetKeyboardContext()));
    }

    private void ReturnPointerFocus(PaneSide? side)
    {
        if (_stopping || side is null)
        {
            return;
        }
        if (PanePointerMapper.CanReturnFocus(_session.Current, side, GetKeyboardContext()))
        {
            FocusFileList(side);
        }
    }

    private void RenderSession(CommanderSnapshot snapshot)
    {
        // The final settings write still drains, but its queued observer cannot rebuild closing content.
        if (_stopping)
        {
            return;
        }
        _pointerMapper.ObserveSession(snapshot);
        _resources.ApplyLanguage(snapshot.Settings.Settings.Display.InterfaceLanguage);
        WindowFrame.Language = _resources.LanguageTag;
        _captionBarView.RenderLanguage();
        _commandEntryView.Render(CommandEntryPresenter.Present(snapshot));
        bool returningFromPicker = _themePicker.IsOpen && snapshot.Settings.Editor == SettingsEditorState.Closed;
        RenderPanes(snapshot.Panes, snapshot.Settings.Settings.Display.KeyHintVisibility);
        RenderSettingsSurfaces(snapshot.Settings);
        _paletteView.Render(snapshot);
        _contextView.Render(snapshot);
        _addressView.Render(snapshot.Scopes.AddressEditor);
        _filterView.Render(snapshot);
        _locationsView.Render(snapshot.Scopes.Locations);
        _windowAdjustmentView.Render();
        _keyboardHelpView.Render();
        RenderColorScheme(_themePicker.PreviewScheme ?? snapshot.Settings.Settings.Display.ColorScheme);
        if (returningFromPicker)
        {
            FocusFileList(snapshot.Panes.ActiveSide);
        }
    }

    private void RenderColorScheme(ColorScheme scheme)
    {
        if (_renderedScheme != scheme)
        {
            _renderedScheme = scheme;
            ColorSchemeChanged?.Invoke(this, new ColorSchemeChangedEventArgs(scheme));
        }
    }

    private void RenderSettingsSurfaces(SettingsSnapshot snapshot)
    {
        _themePicker.Render(snapshot);
        _settingsView.Render(SettingsPresenter.Present(snapshot));
        _bookmarkView.Render(snapshot);
        if (_bookmarkView.IsOpen || _settingsView.IsOpen || _themePicker.IsOpen)
        {
            _operationContext = KeyboardContext.Modal;
        }
    }

    private void RenderPanes(DualPaneSnapshot snapshot, KeyHintVisibility keyHintVisibility)
    {
        if (_stopping)
        {
            return;
        }
        DualPanePresentation presentation = DualPanePresenter.Present(snapshot, _metadataFormat, _presentation, keyHintVisibility);
        _presentation = presentation;
        _leftPaneView.Render(presentation.Left, presentation.LeftFrame,
            PaneNavigationAction.Create(snapshot, PaneSide.Left));
        _rightPaneView.Render(presentation.Right, presentation.RightFrame,
            PaneNavigationAction.Create(snapshot, PaneSide.Right));
        _operationView.Render(presentation);
        _operationContext = presentation.InputContext;
        if (GetKeyboardContext() == KeyboardContext.FileList)
        {
            _ = DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, FocusActiveFileListWhenIdle);
        }
    }

    /// <summary>
    /// Returns keyboard focus to the active file list after the framework realized its rows, unless
    /// a text editor owns focus. Runs on the UI thread through the window's dispatcher queue.
    /// </summary>
    private void FocusActiveFileListWhenIdle()
    {
        if (!_stopping && GetKeyboardContext() == KeyboardContext.FileList)
        {
            FocusFileList(_session.Current.Panes.ActiveSide);
        }
    }

    private bool ForwardOutcome(KeyboardMappingOutcome outcome)
    {
        if (outcome is MappedKeyboardFocusAction focus)
        {
            ForwardFocus(focus.Action);
            return true;
        }
        if (outcome is MappedWindowAdjustmentAction windowAction)
        {
            _windowAdjustmentView.Forward(windowAction.Action);
            return true;
        }
        return outcome switch
        {
            MappedCommandPaletteAction paletteAction => _themePicker.IsOpen ? _themePicker.Forward(paletteAction.Action) : _paletteView.Forward(paletteAction.Action),
            MappedBookmarkManagerAction bookmarkAction => _bookmarkView.Forward(bookmarkAction.Action),
            MappedKeyboardIntent mapped => ForwardMappedIntent(mapped.Intent),
            _ => outcome is KeyboardAwaitingChord or KeyboardConsumed,
        };
    }

    private bool ForwardMappedIntent(UserIntent intent)
    {
        if (SettingsOverlay.Visibility == Visibility.Visible && _settingsView.Forward(intent))
        {
            return true;
        }
        if (_session.Current.Scopes.KeyboardHelp is KeyboardHelpOpen)
        {
            _keyboardHelpView.Forward(intent);
            return true;
        }
        if (GetKeyboardContext() == KeyboardContext.PaneFilter)
        {
            _filterView.Forward(intent);
            return true;
        }
        ForwardIntent(LocationsPresenter.Qualify(intent, _locationsView.Rendered));
        return true;
    }

    private void ForwardFocus(KeyboardFocusAction action)
    {
        if (action == KeyboardFocusAction.ActivePane)
        {
            FocusFileList(_session.Current.Panes.ActiveSide);
        }
        else if (GetKeyboardContext() == KeyboardContext.FileList)
        {
            _commandEntryView.Render(CommandEntryPresenter.Present(_session.Current));
            _commandEntryView.FocusFirst();
        }
    }

    private void ForwardIntent(UserIntent intent)
    {
        if (intent is not PaneInteraction)
        {
            _pointerMapper.Reset();
        }
        if (_stopping)
        {
            return;
        }
        if (intent == UserIntent.FocusAddress && _addressView.IsOpen)
        {
            _addressView.FocusAndSelectAll();
            return;
        }
        UserIntent forwarded = CreateForwardedIntent(intent);
        PaneFilterEditSubmission? leavingFilter = _filterView.CaptureExternalExit(forwarded);
        if (!_paneWork.TryStartIntent(forwarded, cancellationToken => RenderIntentAsync(forwarded, leavingFilter, cancellationToken)))
        {
            _pointerMapper.Reset();
            RenderSession(_session.Current);
        }
    }

    private UserIntent CreateForwardedIntent(UserIntent intent)
    {
        return _addressView.IsOpen
            ? _addressView.Qualify(intent)
            : intent == UserIntent.Confirm && NameEntryFrame.Visibility == Visibility.Visible
                ? UserIntent.SubmitName(NameEntry.Text)
                : intent;
    }

    private void OnBookmarkKeyHintClick(object sender, RoutedEventArgs args)
    {
        _bookmarkView.Invoke(sender);
    }

    private void OnCommandPaletteRowLoaded(object sender, RoutedEventArgs _)
    {
        CommandPaletteView.BindTitleToolTip(sender);
    }

    private void OnSettingsClose(object _, RoutedEventArgs args)
    {
        ForwardIntent(UserIntent.Escape);
    }

    private void OnSettingsShowHiddenChanged(object sender, RoutedEventArgs args)
    {
        _ = args;
        _settingsView.ShowHiddenChanged(sender);
    }

    private void OnSettingsShowKeyHintsChanged(object sender, RoutedEventArgs args)
    {
        _ = args;
        _settingsView.ShowKeyHintsChanged(sender);
    }

    private void OnSettingsLanguageChecked(object sender, RoutedEventArgs args)
    {
        _ = args;
        _settingsView.LanguageChecked(sender);
    }

    /// <summary>Cancels and awaits pane work before the application releases operation resources.</summary>
    public async Task StopAsync()
    {
        _stopping = true;
        _windowAdjustmentView.Stop();
        _contextView.Stop();
        _filterView.Stop();
        InputSurface.IsHitTestVisible = false;
        SavedWindowPlacement? placement = _placementAdapter.FinishTracking();
        try
        {
            await _paneWork.StopAsync().ConfigureAwait(true);
        }
        finally
        {
            await _session.StopAsync(placement, this).ConfigureAwait(true);
        }
    }

    private KeyboardContext GetKeyboardContext()
    {
        return _session.Current.Scopes switch
        {
            { WindowAdjustment: WindowAdjustmentOpen } => KeyboardContext.WindowAdjustment,
            { KeyboardHelp: KeyboardHelpOpen } => KeyboardContext.KeyboardHelp,
            { Locations: not LocationsClosed } => KeyboardContext.Locations,
            { CommandPalette: CommandPaletteOpen open } when open.Surface != CommandSurface.Palette => KeyboardContext.ContextMenu,
            _ when CommandPaletteOverlay.Visibility == Visibility.Visible => KeyboardContext.CommandPalette,
            _ when _themePicker.IsOpen => KeyboardContext.CommandPalette,
            _ when SettingsOverlay.Visibility == Visibility.Visible => KeyboardContext.Settings,
            _ when _operationContext == KeyboardContext.Modal => _bookmarkView.OwnedKeyboardContext ?? KeyboardContext.Modal,
            { AddressEditor: not AddressEditorClosed } => KeyboardContext.AddressEntry,
            { NameFilter: PaneFilterEditing } => KeyboardContext.PaneFilter,
            _ => CommanderFocusView.CurrentContext(InputSurface, _commandEntryView, _filterView),
        };
    }

    private void FocusFileList(PaneSide? side)
    {
        if (_stopping || side is null)
        {
            return;
        }
        _commandEntryView.Render(CommandEntryPresenter.Present(_session.Current));
        CommanderFocusView.FocusFileList(side == PaneSide.Left ? LeftFileList : RightFileList);
    }
}
