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
            (int)VirtualKey.Left => KeyboardKey.Left,
            (int)VirtualKey.Right => KeyboardKey.Right,
            (int)VirtualKey.Back => KeyboardKey.Backspace,
            (int)VirtualKey.Enter => KeyboardKey.Enter,
            (int)VirtualKey.PageDown => KeyboardKey.PageDown,
            (int)VirtualKey.PageUp => KeyboardKey.PageUp,
            (int)VirtualKey.Tab => KeyboardKey.Tab,
            (int)VirtualKey.Space => KeyboardKey.Space,
            (int)VirtualKey.Escape => KeyboardKey.Escape,
            (int)VirtualKey.F1 => KeyboardKey.F1,
            (int)VirtualKey.F2 => KeyboardKey.F2,
            (int)VirtualKey.F3 => KeyboardKey.F3,
            (int)VirtualKey.F4 => KeyboardKey.F4,
            (int)VirtualKey.F5 => KeyboardKey.F5,
            (int)VirtualKey.F6 => KeyboardKey.F6,
            (int)VirtualKey.F7 => KeyboardKey.F7,
            (int)VirtualKey.F8 => KeyboardKey.F8,
            (int)VirtualKey.F10 => KeyboardKey.F10,
            (int)VirtualKey.Application => KeyboardKey.Apps,
            _ => modifier == KeyboardModifier.Control ? TranslateControlKey(virtualKey) : KeyboardKey.Other,
        };
        return KeyboardInput.Create(key, modifier, repeatState, context);
    }

    private static KeyboardKey TranslateControlKey(int virtualKey)
    {
        return virtualKey switch
        {
            (int)VirtualKey.B => KeyboardKey.B,
            (int)VirtualKey.W => KeyboardKey.W,
            (int)VirtualKey.G => KeyboardKey.LowerG,
            (int)VirtualKey.Number1 => KeyboardKey.One,
            (int)VirtualKey.Number2 => KeyboardKey.Two,
            (int)VirtualKey.Number3 => KeyboardKey.Three,
            (int)VirtualKey.Number4 => KeyboardKey.Four,
            (int)VirtualKey.Number5 => KeyboardKey.Five,
            (int)VirtualKey.Number6 => KeyboardKey.Six,
            (int)VirtualKey.Number7 => KeyboardKey.Seven,
            (int)VirtualKey.Number8 => KeyboardKey.Eight,
            (int)VirtualKey.Number9 => KeyboardKey.Nine,
            188 => KeyboardKey.Comma,
            _ => KeyboardKey.Other,
        };
    }

    internal static KeyboardInput TranslateCharacterData(
        char character,
        KeyRepeatState repeatState,
        KeyboardContext context,
        KeyboardModifier modifier)
    {
        KeyboardKey key = character switch
        {
            'a' => KeyboardKey.A,
            'd' or '\u0004' => KeyboardKey.D,
            'e' => KeyboardKey.E,
            '/' => KeyboardKey.Slash,
            'b' or '\u0002' => KeyboardKey.B,
            'G' => KeyboardKey.UpperG,
            'g' or '\u0007' => KeyboardKey.LowerG,
            'h' => KeyboardKey.H,
            '\u0008' when modifier == KeyboardModifier.Control => KeyboardKey.H,
            'j' => KeyboardKey.J,
            'k' => KeyboardKey.K,
            'm' => KeyboardKey.M,
            'n' => KeyboardKey.N,
            'w' or '' => KeyboardKey.W,
            '+' => KeyboardKey.Plus,
            '-' => KeyboardKey.Minus,
            'l' or '\u000c' => KeyboardKey.L,
            'p' or '\u0010' => KeyboardKey.P,
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

    internal static KeyboardModifier TranslateF10ModifierState(
        CoreVirtualKeyStates controlState,
        CoreVirtualKeyStates altState,
        CoreVirtualKeyStates shiftState)
    {
        KeyboardModifier commandModifier = TranslateModifierState(controlState, altState);
        return !IsPressed(shiftState)
            ? commandModifier
            : commandModifier == KeyboardModifier.None ? KeyboardModifier.Shift : KeyboardModifier.Other;
    }
}
