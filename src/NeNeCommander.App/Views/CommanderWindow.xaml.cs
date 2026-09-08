using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.ApplicationModel.Resources;
using NeNeCommander.App.Input;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Lifecycle;
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
    private AddressEditorPresentation? _addressPresentation;
    private AddressEditorState? _defaultFileListFocusSuppressedState;
    private AddressEditorState? _leftAddressOwner;
    private AddressEditorState? _rightAddressOwner;
    private ActiveConflictModal? _renderedConflictModal;
    private KeyboardContext _operationContext = KeyboardContext.FileList;
    private DualPanePresentation? _presentation;
    private ColorScheme? _renderedScheme;
    private bool _renderingSettings;
    private bool _renderingAddressTransition;

    /// <summary>Initializes the shell with the sole keyboard mapping and pane coordination mechanisms.</summary>
    /// <param name="keyboardIntentMapper">Canonical context-aware keyboard mapper.</param>
    /// <param name="session">Coordinator over the pane and settings state owners.</param>
    /// <param name="initialLeftLocation">Validated location read into the left pane when the shell loads.</param>
    /// <param name="initialRightLocation">Validated location read into the right pane when the shell loads.</param>
    /// <param name="defectObserver">Application callback that publishes unexpected task defects.</param>
    public CommanderWindow(
        KeyboardIntentMapper keyboardIntentMapper,
        CommanderSession session,
        FileSystemPath initialLeftLocation,
        FileSystemPath initialRightLocation,
        Action<Exception> defectObserver)
    {
        ArgumentNullException.ThrowIfNull(keyboardIntentMapper);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(initialLeftLocation);
        ArgumentNullException.ThrowIfNull(initialRightLocation);
        ArgumentNullException.ThrowIfNull(defectObserver);
        _keyboardIntentMapper = keyboardIntentMapper;
        _session = session;
        _initialLeftLocation = initialLeftLocation;
        _initialRightLocation = initialRightLocation;
        _paneWork = new AsyncWorkOwner(defectObserver);
        _resources = new ResourceLoader();
        InitializeComponent();
        Title = _resources.GetString("CommanderWindowTitle");
        _renderedScheme = session.Current.Settings.Settings.ColorScheme;
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
        KeyboardInput input = WinUiKeyboardInputTranslator.TranslateKey(args, GetKeyboardContext());
        KeyboardMappingOutcome outcome = _keyboardIntentMapper.Map(input);
        if (ConflictModal.Visibility == Visibility.Visible ||
            SettingsOverlay.Visibility == Visibility.Visible)
        {
            outcome = KeyboardIntentMapper.DeferModalConfirmToNativeControl(outcome);
        }
        args.Handled = ForwardOutcome(outcome);
    }

    private void OnCharacterReceived(object _, CharacterReceivedRoutedEventArgs args)
    {
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
            snapshot.AddressEditor,
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
        if (addressChanged)
        {
            RenderAddressTransition(address);
        }
        ColorScheme scheme = snapshot.Settings.Settings.ColorScheme;
        if (_renderedScheme != scheme)
        {
            _renderedScheme = scheme;
            ColorSchemeChanged?.Invoke(this, new ColorSchemeChangedEventArgs(scheme));
        }
    }

    private void RenderPanes(DualPaneSnapshot snapshot)
    {
        DualPanePresentation presentation = DualPanePresenter.Present(snapshot, _presentation);
        _presentation = presentation;
        RenderPane(PaneSide.Left, presentation.Left, LeftAddress, LeftStatus, LeftFileList);
        RenderPane(PaneSide.Right, presentation.Right, RightAddress, RightStatus, RightFileList);
        RenderFrame(presentation.LeftFrame, LeftPaneBorder, LeftPaneHeader);
        RenderNumber(presentation.LeftFrame, LeftPaneNumberSurface, LeftPaneNumber);
        RenderFrame(presentation.RightFrame, RightPaneBorder, RightPaneHeader);
        RenderNumber(presentation.RightFrame, RightPaneNumberSurface, RightPaneNumber);
        OperationStatus.Text = _resources.GetString(presentation.OperationStatus.ResourceKey);
        RenderTone(presentation.Tone);
        RenderDetail(presentation.Detail);
        OperationKeyHints.ItemsSource = presentation.KeyHints;
        RenderNameEntry(presentation.NameEntry);
        RenderConflict(presentation.ConflictModal);
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

    private void RenderTone(OperationBarTone tone)
    {
        OperationBar.Background = ResolveBrush(tone.SurfaceBrushResourceKey);
        OperationBar.BorderBrush = ResolveBrush(tone.BorderBrushResourceKey);
        Brush foreground = ResolveBrush(tone.ForegroundBrushResourceKey);
        OperationStatus.Foreground = foreground;
        OperationDetailCount.Foreground = foreground;
        OperationProgressSeparator.Foreground = foreground;
        OperationTotal.Foreground = foreground;
        OperationWarningIcon.Stroke = foreground;
        OperationNameEntryIcon.Stroke = foreground;
        RenderToneIcon(tone.Icon);
    }

    private void RenderToneIcon(OperationBarIcon icon)
    {
        OperationWarningIcon.Visibility = icon == OperationBarIcon.Warning
            ? Visibility.Visible
            : Visibility.Collapsed;
        OperationNameEntryIcon.Visibility = icon == OperationBarIcon.NameEntry
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void RenderDetail(OperationDetail detail)
    {
        TransferResultSegments.Visibility = detail is TransferResultDetail
            ? Visibility.Visible
            : Visibility.Collapsed;
        switch (detail)
        {
            case OperationItemCountDetail count:
                OperationProgressSegments.ItemsSource = null;
                OperationDetailCount.Text = count.Count.ToString(CultureInfo.CurrentCulture);
                OperationProgressSeparator.Text = string.Empty;
                OperationTotal.Text = string.Empty;
                break;
            case OperationProgressDetail progress:
                OperationProgressSegments.ItemsSource = progress.Segments;
                OperationDetailCount.Text = progress.Completed.ToString(CultureInfo.CurrentCulture);
                OperationProgressSeparator.Text = _resources.GetString("OperationProgressSeparator");
                OperationTotal.Text = progress.Total.ToString(CultureInfo.CurrentCulture);
                break;
            case TransferResultDetail result:
                OperationProgressSegments.ItemsSource = null;
                OperationDetailCount.Text = string.Empty;
                OperationProgressSeparator.Text = string.Empty;
                OperationTotal.Text = string.Empty;
                TransferResultNotTransferredCount.Text = result.NotTransferred.ToString(CultureInfo.CurrentCulture);
                TransferResultCopiedCount.Text = result.Copied.ToString(CultureInfo.CurrentCulture);
                TransferResultVerifiedCount.Text = result.Verified.ToString(CultureInfo.CurrentCulture);
                TransferResultSourceDeletedCount.Text = result.SourceDeleted.ToString(CultureInfo.CurrentCulture);
                break;
            default:
                OperationProgressSegments.ItemsSource = null;
                OperationDetailCount.Text = string.Empty;
                OperationProgressSeparator.Text = string.Empty;
                OperationTotal.Text = string.Empty;
                break;
        }
    }

    private void RenderNameEntry(NameEntryPresentation nameEntry)
    {
        if (nameEntry is not ActiveNameEntry active)
        {
            NameEntryFrame.Visibility = Visibility.Collapsed;
            return;
        }
        if (NameEntryFrame.Visibility == Visibility.Collapsed)
        {
            NameEntry.Text = active.InitialText;
            NameEntryFrame.Visibility = Visibility.Visible;
        }
        _ = NameEntry.Focus(FocusState.Programmatic);
        NameEntry.SelectAll();
    }

    private void RenderConflict(ConflictModalPresentation conflictModal)
    {
        if (conflictModal is not ActiveConflictModal active)
        {
            ConflictModal.Visibility = Visibility.Collapsed;
            _renderedConflictModal = null;
            return;
        }
        bool isNewConflict = !ReferenceEquals(_renderedConflictModal, active);
        ConflictSource.Text = active.SourceText;
        ConflictExistingTarget.Text = active.ExistingTargetText;
        ConflictKeepBothCandidate.Text = active.KeepBothCandidateText;
        ConflictModal.Visibility = Visibility.Visible;
        if (isNewConflict)
        {
            ConflictApplyToAll.IsChecked = false;
            _ = ConflictCancel.Focus(FocusState.Programmatic);
        }
        _renderedConflictModal = active;
    }

    private void OnConflictSkip(object _, RoutedEventArgs args)
    {
        ForwardConflictDecision(TransferConflictDecision.Skip);
    }

    private void OnConflictKeepBoth(object _, RoutedEventArgs args)
    {
        ForwardConflictDecision(TransferConflictDecision.KeepBoth);
    }

    private void OnConflictCancel(object _, RoutedEventArgs args)
    {
        ForwardConflictDecision(TransferConflictDecision.Cancel);
    }

    private void ForwardConflictDecision(TransferConflictDecision decision)
    {
        TransferConflictScope scope = ConflictApplyToAll.IsChecked is true
            ? TransferConflictScope.All
            : TransferConflictScope.Current;
        ForwardIntent(UserIntent.ResolveConflict(decision, scope));
    }

    private void RenderPane(
        PaneSide side,
        PanePresentation presentation,
        TextBox address,
        TextBlock status,
        ListView fileList)
    {
        if (_addressPresentation?.EditingSide != side)
        {
            address.Text = presentation.AddressText;
        }
        if (!ReferenceEquals(fileList.ItemsSource, presentation.Rows))
        {
            fileList.ItemsSource = presentation.Rows;
        }
        fileList.SelectedItem = presentation.FocusRow;
        if (presentation.FocusRow is not null)
        {
            fileList.ScrollIntoView(presentation.FocusRow);
        }
        PaneStatus paneStatus = _addressPresentation?.EditingSide == side &&
            _addressPresentation.Status is PaneStatus addressStatus
                ? addressStatus
                : presentation.Status;
        status.Text = _resources.GetString(paneStatus.ResourceKey);
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

    private static void RenderFrame(PaneFrame frame, Border border, Border header)
    {
        Brush brush = ResolveBrush(frame.BrushResourceKey);
        border.BorderBrush = brush;
        border.BorderThickness = (Thickness)Microsoft.UI.Xaml.Application.Current.Resources[frame.ThicknessResourceKey];
        header.BorderBrush = brush;
    }

    private static void RenderNumber(PaneFrame frame, Border surface, TextBlock number)
    {
        surface.Background = ResolveBrush(frame.NumberSurfaceBrushResourceKey);
        number.Foreground = ResolveBrush(frame.NumberForegroundBrushResourceKey);
    }

    private static Brush ResolveBrush(string resourceKey)
    {
        return (Brush)Microsoft.UI.Xaml.Application.Current.Resources[resourceKey];
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
        if (outcome is MappedKeyboardIntent mapped)
        {
            ForwardIntent(mapped.Intent);
            return true;
        }
        return outcome is KeyboardAwaitingChord;
    }

    private void ForwardIntent(UserIntent intent)
    {
        if (intent == UserIntent.FocusAddress && FocusedAddress() is TextBox focusedAddress)
        {
            focusedAddress.SelectAll();
            return;
        }
        UserIntent forwarded = CreateForwardedIntent(intent);
        _ = _paneWork.TryStart(cancellationToken =>
            RenderAfterAsync(_session.HandleAsync(forwarded, this, cancellationToken)));
    }

    private UserIntent CreateForwardedIntent(UserIntent intent)
    {
        return intent == UserIntent.Confirm && FocusedAddressSide() is PaneSide side &&
            AddressOwnerOf(side) is AddressEditorState owner &&
            ReferenceEquals(owner, _session.Current.AddressEditor)
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
        AddressEditorState current = _session.Current.AddressEditor;
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
        if (owner is not null && ReferenceEquals(owner, _session.Current.AddressEditor))
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
        if (!EditorOwnsSide(snapshot.AddressEditor, side))
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
        await _paneWork.StopAsync().ConfigureAwait(true);
        await _session.StopAsync().ConfigureAwait(true);
    }

    private KeyboardContext GetKeyboardContext()
    {
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
