using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.ApplicationModel.Resources;
using NeNeCommander.App.Input;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.App.Views;

/// <summary>Hosts the design-neutral dual-pane shell and forwards typed keyboard intents.</summary>
public sealed partial class CommanderWindow : Window
{
    private readonly FileSystemPath _initialLeftLocation;
    private readonly KeyboardIntentMapper _keyboardIntentMapper;
    private readonly PaneSession _leftPane;
    private readonly ResourceLoader _resources;
    private Task? _leftPaneWork;

    /// <summary>Initializes the shell with the sole keyboard mapping and pane navigation mechanisms.</summary>
    /// <param name="keyboardIntentMapper">Canonical context-aware keyboard mapper.</param>
    /// <param name="leftPane">Session that owns the left pane snapshot.</param>
    /// <param name="initialLeftLocation">Validated location read when the shell loads.</param>
    public CommanderWindow(
        KeyboardIntentMapper keyboardIntentMapper,
        PaneSession leftPane,
        FileSystemPath initialLeftLocation)
    {
        ArgumentNullException.ThrowIfNull(keyboardIntentMapper);
        ArgumentNullException.ThrowIfNull(leftPane);
        ArgumentNullException.ThrowIfNull(initialLeftLocation);
        _keyboardIntentMapper = keyboardIntentMapper;
        _leftPane = leftPane;
        _initialLeftLocation = initialLeftLocation;
        _resources = new ResourceLoader();
        InitializeComponent();
        Title = _resources.GetString("CommanderWindowTitle");
    }

    private void OnLoaded(object _, RoutedEventArgs args)
    {
        _leftPaneWork ??= RenderAfterAsync(_leftPane.NavigateAsync(_initialLeftLocation, CancellationToken.None));
    }

    private void OnActivated(object _, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState != WindowActivationState.Deactivated)
        {
            _ = DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, FocusFileListWhenIdle);
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

    /// <summary>
    /// Renders the snapshot the session reports when its work completes. Expected failures arrive
    /// as closed activities, so the owned task faults only on a defect.
    /// </summary>
    private async Task RenderAfterAsync(Task<PaneSnapshot> work)
    {
        RenderLeftPane(_leftPane.Current);
        PaneSnapshot snapshot = await work;
        RenderLeftPane(snapshot);
    }

    private void RenderLeftPane(PaneSnapshot snapshot)
    {
        PanePresentation presentation = PaneListingPresenter.Present(snapshot);
        LeftAddress.Text = presentation.AddressText;
        LeftFileList.ItemsSource = presentation.Entries;
        LeftFileList.SelectedItem = presentation.FocusEntry;
        if (presentation.FocusEntry is not null)
        {
            LeftFileList.ScrollIntoView(presentation.FocusEntry);
        }
        LeftStatus.Text = _resources.GetString(presentation.Status.ResourceKey);
        _ = DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, FocusFileListWhenIdle);
    }

    /// <summary>
    /// Returns keyboard focus to the file list after the framework realized its rows, unless a text
    /// editor owns focus. Runs on the UI thread through the window's dispatcher queue.
    /// </summary>
    private void FocusFileListWhenIdle()
    {
        if (GetKeyboardContext() == KeyboardContext.FileList)
        {
            _ = LeftFileList.Focus(FocusState.Programmatic);
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
        _leftPaneWork = RenderAfterAsync(_leftPane.HandleAsync(intent, CancellationToken.None));
    }

    private KeyboardContext GetKeyboardContext()
    {
        object? focused = FocusManager.GetFocusedElement(Content.XamlRoot);
        return focused is TextBox or RichEditBox or PasswordBox or AutoSuggestBox
            ? KeyboardContext.TextEntry
            : KeyboardContext.FileList;
    }
}
