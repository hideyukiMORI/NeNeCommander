using System;

namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents one complete immutable bookmark registration.</summary>
public sealed record BookmarkEntry
{
    private BookmarkEntry(
        BookmarkDisplayName name,
        BookmarkPath path,
        BookmarkCategoryName? category,
        BookmarkShortcutSlot? shortcutSlot)
    {
        Name = name;
        Path = path;
        Category = category;
        ShortcutSlot = shortcutSlot;
    }

    /// <summary>Gets the display name unique within this entry's category.</summary>
    public BookmarkDisplayName Name { get; }

    /// <summary>Gets the canonical provider path used only when navigation is requested.</summary>
    public BookmarkPath Path { get; }

    /// <summary>Gets the user category, or null for the reserved Uncategorized category.</summary>
    public BookmarkCategoryName? Category { get; }

    /// <summary>Gets the optional globally unique direct-navigation slot.</summary>
    public BookmarkShortcutSlot? ShortcutSlot { get; }

    /// <summary>Creates an entry from values already accepted by their individual boundaries.</summary>
    public static BookmarkEntry Create(
        BookmarkDisplayName name,
        BookmarkPath path,
        BookmarkCategoryName? category,
        BookmarkShortcutSlot? shortcutSlot)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(path);
        return new BookmarkEntry(name, path, category, shortcutSlot);
    }
}
