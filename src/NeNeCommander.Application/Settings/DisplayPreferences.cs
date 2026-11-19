using System;

namespace NeNeCommander.Application.Settings;

/// <summary>Contains every persisted display choice as one complete immutable value.</summary>
public sealed record DisplayPreferences
{
    private DisplayPreferences(
        ColorScheme colorScheme,
        HiddenItemVisibility hiddenItemVisibility,
        KeyHintVisibility keyHintVisibility)
    {
        ColorScheme = colorScheme;
        HiddenItemVisibility = hiddenItemVisibility;
        KeyHintVisibility = keyHintVisibility;
    }

    /// <summary>Gets the launch defaults, including visible footer hints.</summary>
    public static DisplayPreferences Default { get; } = new(
        Settings.ColorScheme.NeNeDark,
        Settings.HiddenItemVisibility.Hidden,
        Settings.KeyHintVisibility.Shown);

    /// <summary>Gets the approved scheme rendered by the host.</summary>
    public ColorScheme ColorScheme { get; }

    /// <summary>Gets the hidden-entry choice applied at next launch.</summary>
    public HiddenItemVisibility HiddenItemVisibility { get; }

    /// <summary>Gets the normal footer hint visibility, applied immediately.</summary>
    public KeyHintVisibility KeyHintVisibility { get; }

    /// <summary>Creates one complete value from three validated choices.</summary>
    public static DisplayPreferences Create(
        ColorScheme colorScheme,
        HiddenItemVisibility hiddenItemVisibility,
        KeyHintVisibility keyHintVisibility)
    {
        ArgumentNullException.ThrowIfNull(colorScheme);
        ArgumentNullException.ThrowIfNull(hiddenItemVisibility);
        ArgumentNullException.ThrowIfNull(keyHintVisibility);
        return new DisplayPreferences(colorScheme, hiddenItemVisibility, keyHintVisibility);
    }
}
