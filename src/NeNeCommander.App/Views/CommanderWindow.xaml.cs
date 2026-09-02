using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using NeNeCommander.App.Input;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.App.Views;

/// <summary>Hosts the design-neutral dual-pane shell and forwards typed keyboard intents.</summary>
public sealed partial class CommanderWindow : Window
{
    private readonly KeyboardIntentMapper _keyboardIntentMapper;

    /// <summary>Initializes the shell with the sole keyboard mapping mechanism.</summary>
    /// <param name="keyboardIntentMapper">Canonical context-aware keyboard mapper.</param>
    public CommanderWindow(KeyboardIntentMapper keyboardIntentMapper)
    {
        ArgumentNullException.ThrowIfNull(keyboardIntentMapper);
        _keyboardIntentMapper = keyboardIntentMapper;
        InitializeComponent();
    }

    /// <summary>Occurs when a framework key event maps to one application intent.</summary>
    public event EventHandler<UserIntentMappedEventArgs>? IntentMapped;

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
