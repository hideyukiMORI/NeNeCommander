using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using NeNeCommander.App.Input;
using NeNeCommander.App.Windowing;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;
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
    private readonly CommanderSession _session;
    private readonly ViewTextResources _resources;
    private readonly AsyncWorkOwner _paneWork;
    private readonly BookmarkManagerView _bookmarkView;
    private readonly WindowAdjustmentView _windowAdjustmentView;
    private readonly KeyboardHelpView _keyboardHelpView;
    private readonly SettingsEditorView _settingsView;
    private readonly SettingsThemePickerView _themePicker;
    private readonly CaptionBarView _captionBarView;
    private readonly LocationsView _locationsView;
    private readonly CommandPaletteView _paletteView;
    private readonly OperationBarView _operationView;
    private readonly CommanderPaneView _leftPaneView;
    private readonly CommanderPaneView _rightPaneView;
    private readonly EntryMetadataFormat _metadataFormat;
    private readonly AddressEditorView _addressView;
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
        _resources = (ViewTextResources)WindowFrame.Resources["ViewTextResources"];
        _resources.ApplyLanguage(_session.Current.Settings.Settings.Display.InterfaceLanguage);
        WindowFrame.Language = _resources.LanguageTag;
        _metadataFormat = EntryMetadataFormat.Create(dependencies.LocalZone, _resources.GetString);
        _bookmarkView = new BookmarkManagerView(BookmarkOverlay, _resources, ForwardIntent,
            () => FocusFileList(_session.Current.Panes.ActiveSide));
        _windowAdjustmentView = new WindowAdjustmentView(
            WindowAdjustmentOverlay,
            _resources,
            new WindowAdjustmentRoute(dependencies.Session, new AppWindowPlacementAdapter(AppWindow, InputSurface), dependencies.DefectObserver),
            FocusFileList);
        _locationsView = new LocationsView(LocationsOverlay, _resources, ForwardIntent, FocusFileList);
        _keyboardHelpView = new KeyboardHelpView(KeyboardHelpOverlay, _resources, dependencies.Session, FocusFileList);
        _settingsView = new SettingsEditorView(InputSurface, _resources, ForwardIntent,
            () => FocusFileList(_session.Current.Panes.ActiveSide));
        _themePicker = new SettingsThemePickerView(ThemePickerOverlay, _resources, ForwardIntent, RenderColorScheme);
        Title = _resources.GetString("CommanderWindowTitle");
        _captionBarView = new CaptionBarView(
            CaptionBar,
            _resources,
            new AppWindowCaptionAdapter(AppWindow, CaptionDragArea, CaptionButtons, dependencies.DefectObserver));
        _paletteView = new CommandPaletteView(CommandPaletteOverlay, _resources, ForwardIntent, FocusFileList);
        _addressView = new AddressEditorView(AddressEditorOverlay, _resources, ForwardIntent, FocusFileList);
        _operationView = new OperationBarView(InputSurface, _resources, ForwardIntent);
        _leftPaneView = new CommanderPaneView(InputSurface, _resources, PaneSide.Left);
        _rightPaneView = new CommanderPaneView(InputSurface, _resources, PaneSide.Right);
        _renderedScheme = dependencies.Session.Current.Settings.Settings.Display.ColorScheme;
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
        _captionBarView.RenderActivation(args.WindowActivationState);
        if (args.WindowActivationState != WindowActivationState.Deactivated)
        {
            _ = DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, FocusActiveFileListWhenIdle);
        }
    }

    private void OnKeyDown(object _, KeyRoutedEventArgs args)
    {
        if (_stopping)
        {
            args.Handled = true;
            return;
        }
        KeyboardContext context = GetKeyboardContext();
        KeyboardInput input = WinUiKeyboardInputTranslator.TranslateKey(args, context);
        bool consumesPendingFocus = _bookmarkView.ObserveInput(input);
        if (IsTextComposing || _bookmarkView.HasOpenSelector)
        {
            _keyboardIntentMapper.ObserveNativeInput(input);
            _bookmarkView.ObserveNativeInput();
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
            _bookmarkView.ObserveNativeInput();
        }
    }

    private void OnCharacterReceived(object _, CharacterReceivedRoutedEventArgs args)
    {
        if (_stopping)
        {
            args.Handled = true;
            return;
        }
        KeyboardInput input = WinUiKeyboardInputTranslator.TranslateCharacter(args, GetKeyboardContext());
        args.Handled = !IsTextComposing && ForwardOutcome(_keyboardIntentMapper.Map(input));
        if (!args.Handled)
        {
            _bookmarkView.ObserveNativeInput();
        }
    }

    private bool IsTextComposing => _paletteView.IsComposing || _bookmarkView.IsComposing || _addressView.IsComposing || _themePicker.IsComposing;

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

    private void RenderSession(CommanderSnapshot snapshot)
    {
        _resources.ApplyLanguage(snapshot.Settings.Settings.Display.InterfaceLanguage);
        WindowFrame.Language = _resources.LanguageTag;
        _captionBarView.RenderLanguage();
        bool returningFromPicker = _themePicker.IsOpen && snapshot.Settings.Editor == SettingsEditorState.Closed;
        RenderPanes(snapshot.Panes, snapshot.Settings.Settings.Display.KeyHintVisibility);
        RenderSettingsSurfaces(snapshot.Settings);
        _paletteView.Render(snapshot);
        _addressView.Render(snapshot.Scopes.AddressEditor);
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
        DualPanePresentation presentation = DualPanePresenter.Present(snapshot, _metadataFormat, _presentation, keyHintVisibility);
        _presentation = presentation;
        _leftPaneView.Render(presentation.Left, presentation.LeftFrame);
        _rightPaneView.Render(presentation.Right, presentation.RightFrame);
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
        if (GetKeyboardContext() == KeyboardContext.FileList)
        {
            FocusFileList(_session.Current.Panes.ActiveSide);
        }
    }

    private bool ForwardOutcome(KeyboardMappingOutcome outcome)
    {
        if (outcome is MappedWindowAdjustmentAction windowAction)
        {
            _windowAdjustmentView.Forward(windowAction.Action);
            return true;
        }
        if (outcome is MappedCommandPaletteAction paletteAction)
        {
            return ForwardPaletteAction(paletteAction.Action);
        }
        if (outcome is MappedBookmarkManagerAction bookmarkAction)
        {
            return _bookmarkView.Forward(bookmarkAction.Action);
        }
        if (outcome is MappedKeyboardIntent setting &&
            SettingsOverlay.Visibility == Visibility.Visible &&
            _settingsView.Forward(setting.Intent))
        {
            return true;
        }
        if (outcome is MappedKeyboardIntent help &&
            _session.Current.Scopes.KeyboardHelp is KeyboardHelpOpen)
        {
            _keyboardHelpView.Forward(help.Intent);
            return true;
        }
        if (outcome is MappedKeyboardIntent mapped)
        {
            ForwardIntent(LocationsPresenter.Qualify(mapped.Intent, _locationsView.Rendered));
            return true;
        }
        return outcome is KeyboardAwaitingChord or KeyboardConsumed;
    }

    private bool ForwardPaletteAction(CommandPaletteKeyAction action)
    {
        return _themePicker.IsOpen ? _themePicker.Forward(action) : _paletteView.Forward(action);
    }

    private void ForwardIntent(UserIntent intent)
    {
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
        _ = _paneWork.TryStartIntent(forwarded, cancellationToken =>
            RenderAfterAsync(_session.HandleAsync(forwarded, this, cancellationToken)));
    }

    private UserIntent CreateForwardedIntent(UserIntent intent)
    {
        return _addressView.IsOpen
            ? _addressView.Qualify(intent)
            : intent == UserIntent.Confirm && NameEntryFrame.Visibility == Visibility.Visible
                ? UserIntent.SubmitName(NameEntry.Text)
                : intent;
    }

    private void OnPathDisplayPointerPressed(object sender, PointerRoutedEventArgs args)
    {
        _ = sender;
        args.Handled = true;
        FocusActiveFileListWhenIdle();
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
        InputSurface.IsHitTestVisible = false;
        try
        {
            await _paneWork.StopAsync().ConfigureAwait(true);
        }
        finally
        {
            await _session.StopAsync().ConfigureAwait(true);
        }
    }

    private KeyboardContext GetKeyboardContext()
    {
        return _session.Current.Scopes switch
        {
            { WindowAdjustment: WindowAdjustmentOpen } => KeyboardContext.WindowAdjustment,
            { KeyboardHelp: KeyboardHelpOpen } => KeyboardContext.KeyboardHelp,
            { Locations: not LocationsClosed } => KeyboardContext.Locations,
            _ when CommandPaletteOverlay.Visibility == Visibility.Visible => KeyboardContext.CommandPalette,
            _ when _themePicker.IsOpen => KeyboardContext.CommandPalette,
            _ when SettingsOverlay.Visibility == Visibility.Visible => KeyboardContext.Settings,
            _ when _operationContext == KeyboardContext.Modal => _bookmarkView.OwnedKeyboardContext ?? KeyboardContext.Modal,
            { AddressEditor: not AddressEditorClosed } => KeyboardContext.AddressEntry,
            _ => GetFocusedControlContext(),
        };
    }

    private KeyboardContext GetFocusedControlContext()
    {
        object? focused = FocusManager.GetFocusedElement(Content.XamlRoot);
        return focused is TextBox or RichEditBox or PasswordBox or AutoSuggestBox
            ? KeyboardContext.TextEntry
            : KeyboardContext.FileList;
    }

    private void FocusFileList(PaneSide side)
    {
        ListView fileList = side == PaneSide.Left ? LeftFileList : RightFileList;
        Control target = fileList.SelectedItem is object selected && fileList.ContainerFromItem(selected) is ListViewItem row
            ? row
            : fileList;
        if (!target.Focus(FocusState.Programmatic))
        {
            _ = fileList.Focus(FocusState.Programmatic);
        }
    }
}
