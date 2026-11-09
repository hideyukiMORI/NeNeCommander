using System;
using System.Collections.Generic;

namespace NeNeCommander.Presentation.WinUI.Help;

/// <summary>One help row: every key cap that performs one action, and that action's localized label.</summary>
public sealed record KeyboardHelpRow
{
    internal KeyboardHelpRow(IReadOnlyList<string> keyLabelResourceKeys, string actionLabelResourceKey)
    {
        ArgumentNullException.ThrowIfNull(keyLabelResourceKeys);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionLabelResourceKey);
        KeyLabelResourceKeys = keyLabelResourceKeys;
        ActionLabelResourceKey = actionLabelResourceKey;
    }

    /// <summary>Gets the key-cap resources of every alias of the action, in declaration order.</summary>
    public IReadOnlyList<string> KeyLabelResourceKeys { get; }

    /// <summary>Gets the localized action label resource.</summary>
    public string ActionLabelResourceKey { get; }
}
