using System;
using System.Collections.Generic;

namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>
/// Declares one key the window-adjustment keyboard context owns: the layout-translated key, its
/// explicit modifier state, the single Presentation action it emits, and the hint group whose
/// displayed caps it joins. An entry that declares no hint group is an alias of another entry's
/// action and shows no cap, so the helper never advertises the same action twice (KBD-005).
/// </summary>
public sealed record WindowAdjustmentKeyBinding
{
    private static readonly Dictionary<KeyboardKey, string> ControlKeyLabels = new()
    {
        [KeyboardKey.W] = "KeyLabelCtrlW",
    };

    internal WindowAdjustmentKeyBinding(
        KeyboardKey key,
        KeyboardModifier modifier,
        WindowAdjustmentKeyAction action,
        WindowAdjustmentHintGroup? hintGroup)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(modifier);
        ArgumentNullException.ThrowIfNull(action);
        Key = key;
        Modifier = modifier;
        Action = action;
        HintGroup = hintGroup;
    }

    /// <summary>Gets the owned key.</summary>
    public KeyboardKey Key { get; }

    /// <summary>Gets the explicit modifier state the binding declares.</summary>
    public KeyboardModifier Modifier { get; }

    /// <summary>Gets the Presentation action emitted for the key.</summary>
    public WindowAdjustmentKeyAction Action { get; }

    /// <summary>Gets the hint group this key is a displayed cap of, or absence for an alias.</summary>
    public WindowAdjustmentHintGroup? HintGroup { get; }

    /// <summary>Gets the localized key-cap resource for this key and its modifier chord.</summary>
    public string KeyLabelResourceKey => SelectKeyLabelResource();

    private string SelectKeyLabelResource()
    {
        return Modifier == KeyboardModifier.None
            ? Key.LabelResourceKey
            : ControlKeyLabels.TryGetValue(Key, out string? label) ? label : "KeyLabelUnmapped";
    }
}
