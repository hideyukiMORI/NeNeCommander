using NeNeCommander.Application.Input;

namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>
/// Represents one declared entry of the canonical key map: the context that owns the keystroke,
/// the layout-translated key, its explicit modifier state, and the single intent it emits. The
/// same declarations both drive <see cref="KeyboardIntentMapper.Map"/> and generate every
/// displayed shortcut hint, so no view can hold a private binding (KBD-005).
/// </summary>
public sealed record KeyBinding
{
    internal KeyBinding(
        KeyboardContext context,
        KeyboardKey key,
        KeyboardModifier modifier,
        UserIntent intent)
    {
        Context = context;
        Key = key;
        Modifier = modifier;
        Intent = intent;
    }

    /// <summary>Gets the focus context in which the binding is declared.</summary>
    public KeyboardContext Context { get; }

    /// <summary>Gets the layout-translated key the binding declares.</summary>
    public KeyboardKey Key { get; }

    /// <summary>Gets the explicit modifier state the binding declares.</summary>
    public KeyboardModifier Modifier { get; }

    /// <summary>Gets the localized key-cap resource for this key and its modifier chord.</summary>
    public string KeyLabelResourceKey => Modifier == KeyboardModifier.None
        ? Key.LabelResourceKey
        : Modifier == KeyboardModifier.Control && Key == KeyboardKey.D
            ? "KeyLabelCtrlD"
            : Modifier == KeyboardModifier.Control && Key == KeyboardKey.H
                ? "KeyLabelCtrlH"
                : Modifier == KeyboardModifier.Control && Key == KeyboardKey.L
                    ? "KeyLabelCtrlL"
                    : Modifier == KeyboardModifier.Control && Key == KeyboardKey.R
                        ? "KeyLabelCtrlR"
                        : Modifier == KeyboardModifier.Control && Key == KeyboardKey.U
                            ? "KeyLabelCtrlU"
                            : Modifier == KeyboardModifier.Alt && Key == KeyboardKey.Up
                                ? "KeyLabelAltUp"
                                : "KeyLabelUnmapped";

    /// <summary>Gets the sole intent the binding emits.</summary>
    public UserIntent Intent { get; }
}
