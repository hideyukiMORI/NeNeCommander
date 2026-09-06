using System;

namespace NeNeCommander.Application.Bookmarks;

/// <summary>Captures a bookmark key and complete immutable entry for stale-action rejection.</summary>
public sealed record BookmarkSelection
{
    /// <summary>Initializes a selection from the entry displayed to the user.</summary>
    public BookmarkSelection(BookmarkEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        Key = new BookmarkKey(entry.Category, entry.Name);
        Entry = entry;
    }

    /// <summary>Gets the case-insensitive lookup key captured with the selection.</summary>
    public BookmarkKey Key { get; }

    /// <summary>Gets the complete expected entry captured with the selection.</summary>
    public BookmarkEntry Entry { get; }
}
