using System.Collections.Generic;
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
    private static readonly Dictionary<KeyboardModifier, Dictionary<KeyboardKey, string>> ModifiedKeyLabels =
        new()
        {
            [KeyboardModifier.Control] = new Dictionary<KeyboardKey, string>
            {
                [KeyboardKey.D] = "KeyLabelCtrlD",
                [KeyboardKey.H] = "KeyLabelCtrlH",
                [KeyboardKey.L] = "KeyLabelCtrlL",
                [KeyboardKey.R] = "KeyLabelCtrlR",
                [KeyboardKey.U] = "KeyLabelCtrlU",
            },
            [KeyboardModifier.Alt] = new Dictionary<KeyboardKey, string>
            {
                [KeyboardKey.Up] = "KeyLabelAltUp",
            },
        };

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
    public string KeyLabelResourceKey => SelectKeyLabelResource();

    /// <summary>Gets the sole intent the binding emits.</summary>
    public UserIntent Intent { get; }

    private string SelectKeyLabelResource()
    {
        return Modifier == KeyboardModifier.None
            ? Key.LabelResourceKey
            : SelectModifiedKeyLabelResource();
    }

    private string SelectModifiedKeyLabelResource()
    {
        return ModifiedKeyLabels.TryGetValue(Modifier, out Dictionary<KeyboardKey, string>? labels) &&
            labels.TryGetValue(Key, out string? label)
            ? label
            : "KeyLabelUnmapped";
    }
}
