using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.ApplicationModel.Resources;
using NeNeCommander.App.Input;
using NeNeCommander.Application.Directories;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.App.Views;

/// <summary>Hosts the design-neutral dual-pane shell and forwards typed keyboard intents.</summary>
public sealed partial class CommanderWindow : Window
{
    private readonly IDirectoryReadPort _directoryReader;
    private readonly DirectoryReadRequest _initialLeftRequest;
    private readonly KeyboardIntentMapper _keyboardIntentMapper;
    private readonly ResourceLoader _resources;
    private Task? _initialLeftLoad;

    /// <summary>Initializes the shell with its sole keyboard mapping and directory read mechanisms.</summary>
    /// <param name="keyboardIntentMapper">Canonical context-aware keyboard mapper.</param>
    /// <param name="directoryReader">Provider-neutral directory read port.</param>
    /// <param name="initialLeftRequest">Validated request for the initial left-pane location.</param>
    public CommanderWindow(
        KeyboardIntentMapper keyboardIntentMapper,
        IDirectoryReadPort directoryReader,
        DirectoryReadRequest initialLeftRequest)
    {
        ArgumentNullException.ThrowIfNull(keyboardIntentMapper);
        ArgumentNullException.ThrowIfNull(directoryReader);
        ArgumentNullException.ThrowIfNull(initialLeftRequest);
        _keyboardIntentMapper = keyboardIntentMapper;
        _directoryReader = directoryReader;
        _initialLeftRequest = initialLeftRequest;
        _resources = new ResourceLoader();
        InitializeComponent();
        Title = _resources.GetString("CommanderWindowTitle");
    }

    /// <summary>Occurs when a framework key event maps to one application intent.</summary>
    public event EventHandler<UserIntentMappedEventArgs>? IntentMapped;

    private void OnLoaded(object _, RoutedEventArgs args)
    {
        _initialLeftLoad ??= LoadLeftPaneAsync();
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
    /// Reads the initial left location once. Expected failures arrive as closed outcomes, so the
    /// owned task faults only on a defect.
    /// </summary>
    private async Task LoadLeftPaneAsync()
    {
        DirectoryReadOutcome outcome = await _directoryReader.ReadAsync(_initialLeftRequest, CancellationToken.None);
        RenderLeftPane(PaneListingPresenter.Present(outcome));
    }

    private void RenderLeftPane(PanePresentation presentation)
    {
        LeftAddress.Text = _initialLeftRequest.Location.CanonicalText;
        LeftFileList.ItemsSource = presentation.Entries;
        LeftFileList.SelectedItem = presentation.FocusEntry;
        LeftStatus.Text = _resources.GetString(presentation.Status.ResourceKey);
    }

    private bool ForwardOutcome(KeyboardMappingOutcome outcome)
    {
        if (outcome is MappedKeyboardIntent mapped)
        {
            IntentMapped?.Invoke(this, new UserIntentMappedEventArgs(mapped.Intent));
            return true;
        }
        return outcome is KeyboardAwaitingChord;
    }

    private KeyboardContext GetKeyboardContext()
    {
        object? focused = FocusManager.GetFocusedElement(Content.XamlRoot);
        return focused is TextBox or RichEditBox or PasswordBox or AutoSuggestBox
            ? KeyboardContext.TextEntry
            : KeyboardContext.FileList;
    }
}
