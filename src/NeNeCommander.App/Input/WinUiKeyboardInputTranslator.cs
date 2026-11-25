using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using NeNeCommander.Presentation.WinUI.Input;
using Windows.System;
using Windows.UI.Core;

namespace NeNeCommander.App.Input;

internal static class WinUiKeyboardInputTranslator
{
    internal static KeyboardInput TranslateKey(KeyRoutedEventArgs args, KeyboardContext context)
    {
        KeyRepeatState repeatState = args.KeyStatus.WasKeyDown
            ? KeyRepeatState.Repeated
            : KeyRepeatState.Initial;
        return KeyboardInputTranslator.TranslateKeyData(
            (int)args.Key,
            repeatState,
            context,
            args.Key == VirtualKey.F10 ? GetF10Modifier() : GetModifier());
    }

    internal static KeyboardInput TranslateCharacter(
        CharacterReceivedRoutedEventArgs args,
        KeyboardContext context)
    {
        KeyRepeatState repeatState = args.KeyStatus.WasKeyDown
            ? KeyRepeatState.Repeated
            : KeyRepeatState.Initial;
        return KeyboardInputTranslator.TranslateCharacterData(
            args.Character,
            repeatState,
            context,
            GetModifier());
    }

    private static KeyboardModifier GetModifier()
    {
        CoreVirtualKeyStates control = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
        CoreVirtualKeyStates alt = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu);
        return KeyboardInputTranslator.TranslateModifierState(control, alt);
    }

    private static KeyboardModifier GetF10Modifier()
    {
        return KeyboardInputTranslator.TranslateF10ModifierState(
            InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control),
            InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu),
            InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift));
    }
}
