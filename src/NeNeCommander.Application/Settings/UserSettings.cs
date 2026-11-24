using System;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Windowing;

namespace NeNeCommander.Application.Settings;

/// <summary>
/// Represents the complete persisted user preferences the application reads at startup. The
/// record is constructor-complete: a partially applied document never becomes settings.
/// </summary>
public sealed record UserSettings
{
    private UserSettings(
        DisplayPreferences display,
        BookmarkCatalog bookmarks,
        PaneSortPreferences paneSortOrders,
        SavedWindowPlacement? windowPlacement)
    {
        Display = display;
        Bookmarks = bookmarks;
        PaneSortOrders = paneSortOrders;
        WindowPlacement = windowPlacement;
    }

    /// <summary>Gets the settings used when no valid persisted document exists.</summary>
    public static UserSettings Default { get; } =
        new(
            DisplayPreferences.Default,
            BookmarkCatalog.Empty,
            PaneSortPreferences.Default,
            null);

    /// <summary>Gets the complete persisted display choices.</summary>
    public DisplayPreferences Display { get; }

    /// <summary>Gets the complete immutable bookmark catalog.</summary>
    public BookmarkCatalog Bookmarks { get; }

    /// <summary>Gets both panes' independently persisted next-launch sort orders.</summary>
    public PaneSortPreferences PaneSortOrders { get; }

    /// <summary>Gets the last normal window placement, or absence for a first launch.</summary>
    public SavedWindowPlacement? WindowPlacement { get; }

    /// <summary>
    /// Creates settings from preferences and a catalog already validated at their boundaries.
    /// </summary>
    public static UserSettings Create(
        DisplayPreferences display,
        BookmarkCatalog bookmarks,
        PaneSortPreferences paneSortOrders,
        SavedWindowPlacement? windowPlacement)
    {
        ArgumentNullException.ThrowIfNull(display);
        ArgumentNullException.ThrowIfNull(bookmarks);
        ArgumentNullException.ThrowIfNull(paneSortOrders);
        return new UserSettings(display, bookmarks, paneSortOrders, windowPlacement);
    }
}
