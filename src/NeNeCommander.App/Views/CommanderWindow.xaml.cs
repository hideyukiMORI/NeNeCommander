using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.ApplicationModel.Resources;
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
    private readonly ResourceLoader _resources;
    private readonly AsyncWorkOwner _paneWork;
    private readonly BookmarkManagerView _bookmarkView;
    private readonly WindowAdjustmentView _windowAdjustmentView;
    private readonly LocationsView _locationsView;
    private readonly CommandPaletteView _paletteView;
    private readonly OperationBarView _operationView;
    private readonly CommanderPaneView _leftPaneView;
    private readonly CommanderPaneView _rightPaneView;
    private readonly EntryMetadataFormat _metadataFormat;
    private AddressEditorPresentation? _addressPresentation;
    private AddressEditorState? _defaultFileListFocusSuppressedState;
    private AddressEditorState? _leftAddressOwner;
    private AddressEditorState? _rightAddressOwner;
    private KeyboardContext _operationContext = KeyboardContext.FileList;
    private DualPanePresentation? _presentation;
    private ColorScheme? _renderedScheme;
    private bool _renderingSettings;
    private bool _renderingAddressTransition;
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
        _resources = new ResourceLoader();
        _metadataFormat = EntryMetadataFormat.Create(dependencies.LocalZone, _resources.GetString);
        InitializeComponent();
        _bookmarkView = new BookmarkManagerView(BookmarkOverlay, _resources, ForwardIntent);
        _windowAdjustmentView = new WindowAdjustmentView(
            WindowAdjustmentOverlay,
            _resources,
            new WindowAdjustmentRoute(dependencies.Session, new AppWindowPlacementAdapter(AppWindow, InputSurface), dependencies.DefectObserver),
            FocusFileList);
        _locationsView = new LocationsView(LocationsOverlay, _resources, ForwardIntent, FocusFileList);
        Title = _resources.GetString("CommanderWindowTitle");
        _paletteView = new CommandPaletteView(CommandPaletteOverlay, _resources, ForwardIntent, FocusFileList);
        _operationView = new OperationBarView(InputSurface, _resources, ForwardIntent);
        _leftPaneView = new CommanderPaneView(InputSurface, _resources, PaneSide.Left);
        _rightPaneView = new CommanderPaneView(InputSurface, _resources, PaneSide.Right);
        _renderedScheme = dependencies.Session.Current.Settings.Settings.ColorScheme;
    }

    /// <summary>Occurs when the session selects a scheme for the composition root to apply.</summary>
    public event EventHandler<ColorSchemeChangedEventArgs>? ColorSchemeChanged;

    /// <inheritdoc />
    public void OperationProgressed(DualPaneSnapshot snapshot)
    {
        RenderPanes(snapshot);
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
        _ = _paneWork.TryStart(cancellationToken => RenderAfterAsync(LoadInitialLocationsAsync(cancellationToken)));
    }

    private void OnActivated(object _, WindowActivatedEventArgs args)
    {
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
        if (context == KeyboardContext.CommandPalette && _paletteView.IsComposing)
        {
            return;
        }
        KeyboardMappingOutcome outcome = _keyboardIntentMapper.Map(input);
        if (ConflictModal.Visibility == Visibility.Visible ||
            SettingsOverlay.Visibility == Visibility.Visible ||
            BookmarkOverlay.Visibility == Visibility.Visible)
        {
            outcome = KeyboardIntentMapper.DeferModalConfirmToNativeControl(outcome);
        }
        args.Handled = ForwardOutcome(outcome);
    }

    private void OnCharacterReceived(object _, CharacterReceivedRoutedEventArgs args)
    {
        if (_stopping)
        {
            args.Handled = true;
            return;
        }
        KeyboardInput input = WinUiKeyboardInputTranslator.TranslateCharacter(args, GetKeyboardContext());
        args.Handled = ForwardOutcome(_keyboardIntentMapper.Map(input));
    }

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
        AddressEditorPresentation? previousAddress = _addressPresentation;
        AddressEditorPresentation address = AddressEditorPresenter.Present(
            snapshot.Scopes.AddressEditor,
            previousAddress);
        bool addressChanged = !ReferenceEquals(address, previousAddress);
        _addressPresentation = address;
        if (addressChanged)
        {
            _defaultFileListFocusSuppressedState = previousAddress is not null &&
                address.EditingSide is null
                    ? address.SourceState
                    : null;
        }
        if (snapshot.Settings.Editor == SettingsEditorState.Open ||
            snapshot.Panes.Operation is OperationAwaitingConfirmation or OperationAwaitingName or
                OperationAwaitingConflict)
        {
            _defaultFileListFocusSuppressedState = null;
        }
        _renderingAddressTransition = addressChanged;
        RenderPanes(snapshot.Panes);
        _renderingAddressTransition = false;
        RenderSettings(SettingsPresenter.Present(snapshot.Settings));
        _bookmarkView.Render(snapshot.Settings);
        if (_bookmarkView.IsOpen)
        {
            _operationContext = KeyboardContext.Modal;
        }
        if (addressChanged)
        {
            RenderAddressTransition(address);
        }
        _paletteView.Render(snapshot);
        _locationsView.Render(snapshot.Scopes.Locations);
        _windowAdjustmentView.Render();
        ColorScheme scheme = snapshot.Settings.Settings.ColorScheme;
        if (_renderedScheme != scheme)
        {
            _renderedScheme = scheme;
            ColorSchemeChanged?.Invoke(this, new ColorSchemeChangedEventArgs(scheme));
        }
    }

    private void RenderPanes(DualPaneSnapshot snapshot)
    {
        DualPanePresentation presentation = DualPanePresenter.Present(snapshot, _metadataFormat, _presentation);
        _presentation = presentation;
        _leftPaneView.Render(presentation.Left, presentation.LeftFrame, _addressPresentation);
        _rightPaneView.Render(presentation.Right, presentation.RightFrame, _addressPresentation);
        _operationView.Render(presentation);
        _operationContext = presentation.InputContext;
        if (!_renderingAddressTransition && ShouldScheduleFileListFocus())
        {
            _ = DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, FocusActiveFileListWhenIdle);
        }
    }

    private void RenderSettings(SettingsPresentation presentation)
    {
        _renderingSettings = true;
        bool opening = presentation.IsOpen && SettingsOverlay.Visibility != Visibility.Visible;
        SettingsOverlay.Visibility = presentation.IsOpen ? Visibility.Visible : Visibility.Collapsed;
        SettingsShowHiddenAtLaunch.IsChecked = presentation.ShowHiddenItemsAtLaunch;
        if (opening || SettingsSchemeOptions.ItemsSource is null)
        {
            SettingsSchemeOptions.ItemsSource = presentation.Schemes;
        }
        SettingsSaveStatus.Text = _resources.GetString(presentation.SaveStatus.ResourceKey);
        SettingsWarning.Visibility = presentation.Warning.IsVisible
            ? Visibility.Visible
            : Visibility.Collapsed;
        SettingsWarningText.Text = _resources.GetString(presentation.Warning.ResourceKey);
        if (presentation.IsOpen)
        {
            _operationContext = KeyboardContext.Modal;
            if (opening)
            {
                _ = SettingsClose.Focus(FocusState.Programmatic);
            }
        }
        _renderingSettings = false;
    }

    private void RenderAddressTransition(AddressEditorPresentation presentation)
    {
        if (presentation.EditingSide is PaneSide side)
        {
            TextBox address = AddressOf(side);
            SetAddressOwner(side, presentation.SourceState);
            if (presentation.ReplacementText is string replacement)
            {
                address.Text = replacement;
            }
            _ = address.Focus(FocusState.Programmatic);
            if (presentation.SelectAll)
            {
                address.SelectAll();
            }
            return;
        }
        if (presentation.FileListFocusSide is PaneSide fileListSide)
        {
            FocusFileList(fileListSide);
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
            ListView activeList = _session.Current.Panes.ActiveSide == PaneSide.Left ? LeftFileList : RightFileList;
            _ = activeList.Focus(FocusState.Programmatic);
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
            return _paletteView.Forward(paletteAction.Action);
        }
        if (outcome is MappedKeyboardIntent mapped)
        {
            ForwardIntent(LocationsPresenter.Qualify(mapped.Intent, _locationsView.Rendered));
            return true;
        }
        return outcome is KeyboardAwaitingChord or KeyboardConsumed;
    }

    private void ForwardIntent(UserIntent intent)
    {
        if (_stopping)
        {
            return;
        }
        if (intent == UserIntent.FocusAddress && FocusedAddress() is TextBox focusedAddress)
        {
            focusedAddress.SelectAll();
            return;
        }
        UserIntent forwarded = CreateForwardedIntent(intent);
        _ = _paneWork.TryStartIntent(forwarded, cancellationToken =>
            RenderAfterAsync(_session.HandleAsync(forwarded, this, cancellationToken)));
    }

    private UserIntent CreateForwardedIntent(UserIntent intent)
    {
        return intent == UserIntent.Confirm && FocusedAddressSide() is PaneSide side &&
            AddressOwnerOf(side) is AddressEditorState owner &&
            ReferenceEquals(owner, _session.Current.Scopes.AddressEditor)
                ? UserIntent.SubmitAddress(owner, AddressOf(side).Text)
                : intent == UserIntent.Confirm && NameEntryFrame.Visibility == Visibility.Visible
                    ? UserIntent.SubmitName(NameEntry.Text)
                    : intent;
    }

    private void OnAddressGotFocus(object sender, RoutedEventArgs args)
    {
        _ = args;
        if (sender is not TextBox address || SideOf(address) is not PaneSide side)
        {
            return;
        }
        AddressEditorState current = _session.Current.Scopes.AddressEditor;
        if (EditorOwnsSide(current, side))
        {
            SetAddressOwner(side, current);
            return;
        }
        bool started = _paneWork.TryStart(cancellationToken =>
            RenderAfterAddressFocusAsync(side, cancellationToken));
        if (!started)
        {
            FocusFileList(_session.Current.Panes.ActiveSide);
        }
    }

    private void OnAddressLostFocus(object sender, RoutedEventArgs args)
    {
        _ = args;
        if (sender is not TextBox address || SideOf(address) is not PaneSide side)
        {
            return;
        }
        AddressEditorState? owner = AddressOwnerOf(side);
        SetAddressOwner(side, null);
        if (owner is not null && ReferenceEquals(owner, _session.Current.Scopes.AddressEditor))
        {
            ForwardIntent(UserIntent.LeaveAddress(owner));
        }
    }

    private async Task RenderAfterAddressFocusAsync(PaneSide side, CancellationToken cancellationToken)
    {
        Task<CommanderSnapshot> work = _session.HandleAsync(
            UserIntent.BeginAddressEdit(side),
            this,
            cancellationToken);
        RenderSession(_session.Current);
        CommanderSnapshot snapshot = await work;
        RenderSession(snapshot);
        if (!EditorOwnsSide(snapshot.Scopes.AddressEditor, side))
        {
            FocusFileList(snapshot.Panes.ActiveSide);
        }
    }

    private void OnSettingsClose(object _, RoutedEventArgs args)
    {
        ForwardIntent(UserIntent.Escape);
    }

    private void OnSettingsShowHiddenChanged(object sender, RoutedEventArgs args)
    {
        _ = args;
        if (_renderingSettings || sender is not CheckBox checkbox || checkbox.IsChecked is null)
        {
            return;
        }
        HiddenItemVisibility visibility = checkbox.IsChecked is true
            ? HiddenItemVisibility.Shown
            : HiddenItemVisibility.Hidden;
        ForwardIntent(UserIntent.SelectLaunchHiddenItemVisibility(visibility));
    }

    private void OnSettingsSchemeChecked(object sender, RoutedEventArgs args)
    {
        _ = args;
        if (_renderingSettings ||
            sender is not RadioButton { IsChecked: true, DataContext: SettingsColorSchemeOption option })
        {
            return;
        }
        ForwardIntent(UserIntent.SelectColorScheme(option.Scheme));
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
        if (_session.Current.Scopes.WindowAdjustment is WindowAdjustmentOpen)
        {
            return KeyboardContext.WindowAdjustment;
        }
        if (_session.Current.Scopes.Locations is not LocationsClosed)
        {
            return KeyboardContext.Locations;
        }
        if (CommandPaletteOverlay.Visibility == Visibility.Visible)
        {
            return KeyboardContext.CommandPalette;
        }
        if (_operationContext == KeyboardContext.Modal)
        {
            return KeyboardContext.Modal;
        }
        object? focused = FocusManager.GetFocusedElement(Content.XamlRoot);
        return ReferenceEquals(focused, LeftAddress) || ReferenceEquals(focused, RightAddress)
            ? KeyboardContext.AddressEntry
            : focused is TextBox or RichEditBox or PasswordBox or AutoSuggestBox
                ? KeyboardContext.TextEntry
                : KeyboardContext.FileList;
    }

    private bool ShouldScheduleFileListFocus()
    {
        return _addressPresentation?.EditingSide is null &&
            !ReferenceEquals(
                _addressPresentation?.SourceState,
                _defaultFileListFocusSuppressedState);
    }

    private TextBox? FocusedAddress()
    {
        object? focused = FocusManager.GetFocusedElement(Content.XamlRoot);
        return ReferenceEquals(focused, LeftAddress)
            ? LeftAddress
            : ReferenceEquals(focused, RightAddress) ? RightAddress : null;
    }

    private PaneSide? FocusedAddressSide()
    {
        TextBox? address = FocusedAddress();
        return address is null ? null : SideOf(address);
    }

    private PaneSide? SideOf(TextBox address)
    {
        return ReferenceEquals(address, LeftAddress)
            ? PaneSide.Left
            : ReferenceEquals(address, RightAddress) ? PaneSide.Right : null;
    }

    private TextBox AddressOf(PaneSide side)
    {
        return side == PaneSide.Left ? LeftAddress : RightAddress;
    }

    private AddressEditorState? AddressOwnerOf(PaneSide side)
    {
        return side == PaneSide.Left ? _leftAddressOwner : _rightAddressOwner;
    }

    private void SetAddressOwner(PaneSide side, AddressEditorState? owner)
    {
        if (side == PaneSide.Left)
        {
            _leftAddressOwner = owner;
        }
        else
        {
            _rightAddressOwner = owner;
        }
    }

    private static bool EditorOwnsSide(AddressEditorState state, PaneSide side)
    {
        return (state is AddressEditing editing && editing.Side == side) ||
            (state is AddressInputRejected rejected && rejected.Side == side);
    }

    private void FocusFileList(PaneSide side)
    {
        ListView fileList = side == PaneSide.Left ? LeftFileList : RightFileList;
        _ = fileList.Focus(FocusState.Programmatic);
    }
}
