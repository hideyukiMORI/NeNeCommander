using System;
using System.Collections.Generic;

namespace NeNeCommander.Presentation.WinUI.Help;

/// <summary>One help group: the keys one input context owns, under a localized heading.</summary>
public sealed record KeyboardHelpGroup
{
    internal KeyboardHelpGroup(string titleResourceKey, IReadOnlyList<KeyboardHelpRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(titleResourceKey);
        ArgumentNullException.ThrowIfNull(rows);
        TitleResourceKey = titleResourceKey;
        Rows = rows;
    }

    /// <summary>Gets the localized group heading resource.</summary>
    public string TitleResourceKey { get; }

    /// <summary>Gets the rows in the context's declaration order.</summary>
    public IReadOnlyList<KeyboardHelpRow> Rows { get; }
}
