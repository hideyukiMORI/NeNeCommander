using System;
using NeNeCommander.Application.Bookmarks;

namespace NeNeCommander.Application.Settings;

/// <summary>
/// Represents the complete persisted user preferences the application reads at startup. The
/// record is constructor-complete: a partially applied document never becomes settings.
/// </summary>
public sealed record UserSettings
{
    private UserSettings(
        ColorScheme colorScheme,
        HiddenItemVisibility hiddenItemVisibility,
        BookmarkCatalog bookmarks)
    {
        ColorScheme = colorScheme;
        HiddenItemVisibility = hiddenItemVisibility;
        Bookmarks = bookmarks;
    }

    /// <summary>Gets the settings used when no valid persisted document exists.</summary>
    public static UserSettings Default { get; } =
        new(
            Settings.ColorScheme.NeNeDark,
            Settings.HiddenItemVisibility.Hidden,
            BookmarkCatalog.Empty);

    /// <summary>Gets the approved color scheme the host renders.</summary>
    public ColorScheme ColorScheme { get; }

    /// <summary>Gets the closed visibility of hidden and system entries.</summary>
    public HiddenItemVisibility HiddenItemVisibility { get; }

    /// <summary>Gets the complete immutable bookmark catalog.</summary>
    public BookmarkCatalog Bookmarks { get; }

    /// <summary>
    /// Creates settings from preferences and a catalog already validated at their boundaries.
    /// </summary>
    public static UserSettings Create(
        ColorScheme colorScheme,
        HiddenItemVisibility hiddenItemVisibility,
        BookmarkCatalog bookmarks)
    {
        ArgumentNullException.ThrowIfNull(colorScheme);
        ArgumentNullException.ThrowIfNull(hiddenItemVisibility);
        ArgumentNullException.ThrowIfNull(bookmarks);
        return new UserSettings(colorScheme, hiddenItemVisibility, bookmarks);
    }
}
