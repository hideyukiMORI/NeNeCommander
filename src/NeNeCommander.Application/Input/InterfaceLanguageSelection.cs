using System;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Input;

/// <summary>Represents a footer hint language selected in the settings editor.</summary>
public sealed record InterfaceLanguageSelection : UserIntent
{
    private protected override void RestrictDerivation()
    {
    }

    internal InterfaceLanguageSelection(InterfaceLanguage language)
    {
        ArgumentNullException.ThrowIfNull(language);
        Language = language;
    }

    /// <summary>Gets the selected closed language choice.</summary>
    public InterfaceLanguage Language { get; }
}
