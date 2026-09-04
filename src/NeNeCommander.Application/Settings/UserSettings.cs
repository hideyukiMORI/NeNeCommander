using System;

namespace NeNeCommander.Application.Settings;

/// <summary>
/// Represents the complete persisted user preferences the application reads at startup. The
/// record is constructor-complete: a partially applied document never becomes settings.
/// </summary>
public sealed record UserSettings
{
    private UserSettings(ColorScheme colorScheme, HiddenItemVisibility hiddenItemVisibility)
    {
        ColorScheme = colorScheme;
        HiddenItemVisibility = hiddenItemVisibility;
    }

    /// <summary>Gets the settings used when no valid persisted document exists.</summary>
    public static UserSettings Default { get; } =
        new(Settings.ColorScheme.NeNeDark, Settings.HiddenItemVisibility.Hidden);

    /// <summary>Gets the approved color scheme the host renders.</summary>
    public ColorScheme ColorScheme { get; }

    /// <summary>Gets the closed visibility of hidden and system entries.</summary>
    public HiddenItemVisibility HiddenItemVisibility { get; }

    /// <summary>
    /// Creates settings from values an adapter has already validated at the settings boundary.
    /// </summary>
    /// <param name="colorScheme">Approved color scheme.</param>
    /// <param name="hiddenItemVisibility">Closed hidden-entry visibility.</param>
    /// <returns>Complete immutable settings.</returns>
    public static UserSettings Create(ColorScheme colorScheme, HiddenItemVisibility hiddenItemVisibility)
    {
        ArgumentNullException.ThrowIfNull(colorScheme);
        ArgumentNullException.ThrowIfNull(hiddenItemVisibility);
        return new UserSettings(colorScheme, hiddenItemVisibility);
    }
}
