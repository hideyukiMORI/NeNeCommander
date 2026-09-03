using Windows.System;
using Windows.UI.Core;

namespace NeNeCommander.Presentation.WinUI.Input;

internal static class KeyboardInputTranslator
{
    internal static KeyboardInput TranslateKeyData(
        int virtualKey,
        KeyRepeatState repeatState,
        KeyboardContext context,
        KeyboardModifier modifier)
    {
        KeyboardKey key = virtualKey switch
        {
            (int)VirtualKey.Down => KeyboardKey.Down,
            (int)VirtualKey.Up => KeyboardKey.Up,
            (int)VirtualKey.Back => KeyboardKey.Backspace,
            (int)VirtualKey.Enter => KeyboardKey.Enter,
            (int)VirtualKey.PageDown => KeyboardKey.PageDown,
            (int)VirtualKey.PageUp => KeyboardKey.PageUp,
            (int)VirtualKey.Tab => KeyboardKey.Tab,
            (int)VirtualKey.Space => KeyboardKey.Space,
            (int)VirtualKey.Escape => KeyboardKey.Escape,
            (int)VirtualKey.F2 => KeyboardKey.F2,
            (int)VirtualKey.F5 => KeyboardKey.F5,
            (int)VirtualKey.F6 => KeyboardKey.F6,
            (int)VirtualKey.F7 => KeyboardKey.F7,
            (int)VirtualKey.F8 => KeyboardKey.F8,
            _ => KeyboardKey.Other,
        };
        return KeyboardInput.Create(key, modifier, repeatState, context);
    }

    internal static KeyboardInput TranslateCharacterData(
        char character,
        KeyRepeatState repeatState,
        KeyboardContext context,
        KeyboardModifier modifier)
    {
        KeyboardKey key = character switch
        {
            'd' or '\u0004' => KeyboardKey.D,
            'G' => KeyboardKey.UpperG,
            'g' => KeyboardKey.LowerG,
            'h' => KeyboardKey.H,
            'j' => KeyboardKey.J,
            'k' => KeyboardKey.K,
            'l' or '\u000c' => KeyboardKey.L,
            'r' or '\u0012' => KeyboardKey.R,
            'u' or '\u0015' => KeyboardKey.U,
            _ => KeyboardKey.Other,
        };
        return KeyboardInput.Create(key, modifier, repeatState, context);
    }

    internal static KeyboardModifier TranslateModifierState(
        CoreVirtualKeyStates controlState,
        CoreVirtualKeyStates altState)
    {
        bool controlPressed = IsPressed(controlState);
        bool altPressed = IsPressed(altState);
        return controlPressed && altPressed
            ? KeyboardModifier.Other
            : controlPressed
                ? KeyboardModifier.Control
                : altPressed ? KeyboardModifier.Alt : KeyboardModifier.None;
    }

    private static bool IsPressed(CoreVirtualKeyStates state)
    {
        return (state & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
    }
}
