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
using NeNeCommander.Application.Panes;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.App.Views;

/// <summary>Hosts the design-neutral dual-pane shell, forwards typed keyboard intents, and renders progress as it is reported.</summary>
public sealed partial class CommanderWindow : Window, IDualPaneProgressObserver
{
    private readonly FileSystemPath _initialLeftLocation;
    private readonly FileSystemPath _initialRightLocation;
    private readonly KeyboardIntentMapper _keyboardIntentMapper;
    private readonly DualPaneSession _panes;
    private readonly ResourceLoader _resources;
    private Task? _paneWork;
    private KeyboardContext _operationContext = KeyboardContext.FileList;

    /// <summary>Initializes the shell with the sole keyboard mapping and pane coordination mechanisms.</summary>
    /// <param name="keyboardIntentMapper">Canonical context-aware keyboard mapper.</param>
    /// <param name="panes">Coordinator that owns both pane sessions and the active side.</param>
    /// <param name="initialLeftLocation">Validated location read into the left pane when the shell loads.</param>
    /// <param name="initialRightLocation">Validated location read into the right pane when the shell loads.</param>
    public CommanderWindow(
        KeyboardIntentMapper keyboardIntentMapper,
        DualPaneSession panes,
        FileSystemPath initialLeftLocation,
        FileSystemPath initialRightLocation)
    {
        ArgumentNullException.ThrowIfNull(keyboardIntentMapper);
        ArgumentNullException.ThrowIfNull(panes);
        ArgumentNullException.ThrowIfNull(initialLeftLocation);
        ArgumentNullException.ThrowIfNull(initialRightLocation);
        _keyboardIntentMapper = keyboardIntentMapper;
        _panes = panes;
        _initialLeftLocation = initialLeftLocation;
        _initialRightLocation = initialRightLocation;
        _resources = new ResourceLoader();
        InitializeComponent();
        Title = _resources.GetString("CommanderWindowTitle");
    }

    /// <inheritdoc />
    public void OperationProgressed(DualPaneSnapshot snapshot)
    {
        RenderPanes(snapshot);
    }

    private void OnLoaded(object _, RoutedEventArgs args)
    {
        _paneWork ??= RenderAfterAsync(LoadInitialLocationsAsync());
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
        args.Handled = ForwardOutcome(_keyboardIntentMapper.Map(input));
    }

    private void OnCharacterReceived(object _, CharacterReceivedRoutedEventArgs args)
    {
        KeyboardInput input = WinUiKeyboardInputTranslator.TranslateCharacter(args, GetKeyboardContext());
        args.Handled = ForwardOutcome(_keyboardIntentMapper.Map(input));
    }

    private async Task<DualPaneSnapshot> LoadInitialLocationsAsync()
    {
        _ = await _panes.NavigateAsync(PaneSide.Left, _initialLeftLocation, CancellationToken.None);
        return await _panes.NavigateAsync(PaneSide.Right, _initialRightLocation, CancellationToken.None);
    }

    /// <summary>
    /// Renders the snapshot the coordinator reports when its work completes. Expected failures
    /// arrive as closed activities, so the owned task faults only on a defect.
    /// </summary>
    private async Task RenderAfterAsync(Task<DualPaneSnapshot> work)
    {
        RenderPanes(_panes.Current);
        DualPaneSnapshot snapshot = await work;
        RenderPanes(snapshot);
    }

    private void RenderPanes(DualPaneSnapshot snapshot)
    {
        DualPanePresentation presentation = DualPanePresenter.Present(snapshot);
        RenderPane(presentation.Left, LeftAddress, LeftStatus, LeftFileList);
        RenderPane(presentation.Right, RightAddress, RightStatus, RightFileList);
        RenderFrame(presentation.LeftFrame, LeftPaneBorder);
        RenderFrame(presentation.RightFrame, RightPaneBorder);
        OperationStatus.Text = _resources.GetString(presentation.OperationStatus.ResourceKey);
        RenderDetail(presentation.Detail);
        RenderNameEntry(presentation.NameEntry);
        _operationContext = presentation.InputContext;
        _ = DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, FocusActiveFileListWhenIdle);
    }

    private void RenderDetail(OperationDetail detail)
    {
        switch (detail)
        {
            case OperationItemCountDetail count:
                OperationDetailCount.Text = count.Count.ToString(CultureInfo.CurrentCulture);
                OperationProgressSeparator.Text = string.Empty;
                OperationTotal.Text = string.Empty;
                break;
            case OperationProgressDetail progress:
                OperationDetailCount.Text = progress.Completed.ToString(CultureInfo.CurrentCulture);
                OperationProgressSeparator.Text = _resources.GetString("OperationProgressSeparator");
                OperationTotal.Text = progress.Total.ToString(CultureInfo.CurrentCulture);
                break;
            default:
                OperationDetailCount.Text = string.Empty;
                OperationProgressSeparator.Text = string.Empty;
                OperationTotal.Text = string.Empty;
                break;
        }
    }

    private void RenderNameEntry(NameEntryPresentation nameEntry)
    {
        if (nameEntry != NameEntryPresentation.Active)
        {
            NameEntry.Visibility = Visibility.Collapsed;
            return;
        }
        if (NameEntry.Visibility == Visibility.Collapsed)
        {
            NameEntry.Text = string.Empty;
            NameEntry.Visibility = Visibility.Visible;
        }
        _ = NameEntry.Focus(FocusState.Programmatic);
    }

    private void RenderPane(PanePresentation presentation, TextBox address, TextBlock status, ListView fileList)
    {
        address.Text = presentation.AddressText;
        fileList.ItemsSource = presentation.Rows;
        fileList.SelectedItem = presentation.FocusRow;
        if (presentation.FocusRow is not null)
        {
            fileList.ScrollIntoView(presentation.FocusRow);
        }
        status.Text = _resources.GetString(presentation.Status.ResourceKey);
    }

    private static void RenderFrame(PaneFrame frame, Border border)
    {
        border.BorderBrush = (Brush)Microsoft.UI.Xaml.Application.Current.Resources[frame.BrushResourceKey];
        border.BorderThickness = (Thickness)Microsoft.UI.Xaml.Application.Current.Resources[frame.ThicknessResourceKey];
    }

    /// <summary>
    /// Returns keyboard focus to the active file list after the framework realized its rows, unless
    /// a text editor owns focus. Runs on the UI thread through the window's dispatcher queue.
    /// </summary>
    private void FocusActiveFileListWhenIdle()
    {
        if (GetKeyboardContext() == KeyboardContext.FileList)
        {
            ListView activeList = _panes.Current.ActiveSide == PaneSide.Left ? LeftFileList : RightFileList;
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
        UserIntent forwarded = intent == UserIntent.Confirm && NameEntry.Visibility == Visibility.Visible
            ? UserIntent.SubmitName(NameEntry.Text)
            : intent;
        _paneWork = RenderAfterAsync(_panes.HandleAsync(forwarded, this, CancellationToken.None));
    }

    private KeyboardContext GetKeyboardContext()
    {
        if (_operationContext == KeyboardContext.Modal)
        {
            return KeyboardContext.Modal;
        }
        object? focused = FocusManager.GetFocusedElement(Content.XamlRoot);
        return focused is TextBox or RichEditBox or PasswordBox or AutoSuggestBox
            ? KeyboardContext.TextEntry
            : KeyboardContext.FileList;
    }
}
