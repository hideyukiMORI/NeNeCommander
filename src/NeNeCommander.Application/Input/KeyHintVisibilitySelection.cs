using System;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Input;

/// <summary>Represents a footer hint visibility selected in the settings editor.</summary>
public sealed record KeyHintVisibilitySelection : UserIntent
{
    private protected override void RestrictDerivation()
    {
    }

    internal KeyHintVisibilitySelection(KeyHintVisibility visibility)
    {
        ArgumentNullException.ThrowIfNull(visibility);
        Visibility = visibility;
    }

    /// <summary>Gets the selected closed visibility choice.</summary>
    public KeyHintVisibility Visibility { get; }
}
