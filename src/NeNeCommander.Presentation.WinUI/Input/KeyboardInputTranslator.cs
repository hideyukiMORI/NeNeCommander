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
            (int)VirtualKey.B when modifier == KeyboardModifier.Control => KeyboardKey.B,
            (int)VirtualKey.Number1 when modifier == KeyboardModifier.Control => KeyboardKey.One,
            (int)VirtualKey.Number2 when modifier == KeyboardModifier.Control => KeyboardKey.Two,
            (int)VirtualKey.Number3 when modifier == KeyboardModifier.Control => KeyboardKey.Three,
            (int)VirtualKey.Number4 when modifier == KeyboardModifier.Control => KeyboardKey.Four,
            (int)VirtualKey.Number5 when modifier == KeyboardModifier.Control => KeyboardKey.Five,
            (int)VirtualKey.Number6 when modifier == KeyboardModifier.Control => KeyboardKey.Six,
            (int)VirtualKey.Number7 when modifier == KeyboardModifier.Control => KeyboardKey.Seven,
            (int)VirtualKey.Number8 when modifier == KeyboardModifier.Control => KeyboardKey.Eight,
            (int)VirtualKey.Number9 when modifier == KeyboardModifier.Control => KeyboardKey.Nine,
            188 when modifier == KeyboardModifier.Control => KeyboardKey.Comma,
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
            'b' or '\u0002' => KeyboardKey.B,
            'G' => KeyboardKey.UpperG,
            'g' => KeyboardKey.LowerG,
            'h' => KeyboardKey.H,
            '\u0008' when modifier == KeyboardModifier.Control => KeyboardKey.H,
            'j' => KeyboardKey.J,
            'k' => KeyboardKey.K,
            'l' or '\u000c' => KeyboardKey.L,
            'r' or '\u0012' => KeyboardKey.R,
            'u' or '\u0015' => KeyboardKey.U,
            ',' => KeyboardKey.Comma,
            '1' => KeyboardKey.One,
            '2' => KeyboardKey.Two,
            '3' => KeyboardKey.Three,
            '4' => KeyboardKey.Four,
            '5' => KeyboardKey.Five,
            '6' => KeyboardKey.Six,
            '7' => KeyboardKey.Seven,
            '8' => KeyboardKey.Eight,
            '9' => KeyboardKey.Nine,
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
