using System;

namespace NeNeCommander.Application.Settings;

/// <summary>Contains every persisted display choice as one complete immutable value.</summary>
public sealed record DisplayPreferences
{
    private DisplayPreferences(
        ColorScheme colorScheme,
        HiddenItemVisibility hiddenItemVisibility,
        KeyHintVisibility keyHintVisibility,
        InterfaceLanguage interfaceLanguage)
    {
        ColorScheme = colorScheme;
        HiddenItemVisibility = hiddenItemVisibility;
        KeyHintVisibility = keyHintVisibility;
        InterfaceLanguage = interfaceLanguage;
    }

    /// <summary>Gets the launch defaults, including visible footer hints.</summary>
    public static DisplayPreferences Default { get; } = new(
        Settings.ColorScheme.NeNeDark,
        Settings.HiddenItemVisibility.Hidden,
        Settings.KeyHintVisibility.Shown,
        Settings.InterfaceLanguage.System);

    /// <summary>Gets the approved scheme rendered by the host.</summary>
    public ColorScheme ColorScheme { get; }

    /// <summary>Gets the hidden-entry choice applied at next launch.</summary>
    public HiddenItemVisibility HiddenItemVisibility { get; }

    /// <summary>Gets the normal footer hint visibility, applied immediately.</summary>
    public KeyHintVisibility KeyHintVisibility { get; }

    /// <summary>Gets the interface language applied only at the next launch.</summary>
    public InterfaceLanguage InterfaceLanguage { get; }

    /// <summary>Creates one complete value from four validated choices.</summary>
    public static DisplayPreferences Create(
        ColorScheme colorScheme,
        HiddenItemVisibility hiddenItemVisibility,
        KeyHintVisibility keyHintVisibility,
        InterfaceLanguage interfaceLanguage)
    {
        ArgumentNullException.ThrowIfNull(colorScheme);
        ArgumentNullException.ThrowIfNull(hiddenItemVisibility);
        ArgumentNullException.ThrowIfNull(keyHintVisibility);
        ArgumentNullException.ThrowIfNull(interfaceLanguage);
        return new DisplayPreferences(colorScheme, hiddenItemVisibility, keyHintVisibility, interfaceLanguage);
    }
}
