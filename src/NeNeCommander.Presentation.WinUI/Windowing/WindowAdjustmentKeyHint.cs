using System;
using System.Collections.Generic;

namespace NeNeCommander.Presentation.WinUI.Windowing;

/// <summary>
/// Represents one displayed hint of the window-adjustment helper: the key-cap resources of every
/// key declared for one hint group, in declaration order, and the resource that names what those
/// keys do. Both come from the mode's own binding table, so no view holds a private binding
/// (KBD-005) and no hint text is assembled in code (CS-025).
/// </summary>
public sealed record WindowAdjustmentKeyHint
{
    internal WindowAdjustmentKeyHint(
        IReadOnlyList<string> keyLabelResourceKeys,
        string intentLabelResourceKey)
    {
        ArgumentNullException.ThrowIfNull(keyLabelResourceKeys);
        ArgumentException.ThrowIfNullOrWhiteSpace(intentLabelResourceKey);
        KeyLabelResourceKeys = keyLabelResourceKeys;
        IntentLabelResourceKey = intentLabelResourceKey;
    }

    /// <summary>Gets the key-cap resources of this hint, in the table's declaration order.</summary>
    public IReadOnlyList<string> KeyLabelResourceKeys { get; }

    /// <summary>Gets the localization resource that names what the hint's keys do.</summary>
    public string IntentLabelResourceKey { get; }
}
