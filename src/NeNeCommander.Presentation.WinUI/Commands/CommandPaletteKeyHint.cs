using System;

namespace NeNeCommander.Presentation.WinUI.Commands;

/// <summary>Represents one localized palette key hint projected from the canonical key map.</summary>
public sealed record CommandPaletteKeyHint
{
    internal CommandPaletteKeyHint(string keyLabelResourceKey, string intentLabelResourceKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyLabelResourceKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(intentLabelResourceKey);
        KeyLabelResourceKey = keyLabelResourceKey;
        IntentLabelResourceKey = intentLabelResourceKey;
    }

    /// <summary>Gets the key-cap resource declared by the canonical key map.</summary>
    public string KeyLabelResourceKey { get; }

    /// <summary>Gets the localized Presentation action label resource.</summary>
    public string IntentLabelResourceKey { get; }
}
