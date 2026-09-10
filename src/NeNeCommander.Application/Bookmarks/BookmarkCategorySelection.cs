using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NeNeCommander.Application.Bookmarks;

/// <summary>Captures a category and all entries that referenced it for stale-action rejection.</summary>
public sealed record BookmarkCategorySelection
{
    private readonly ReadOnlyCollection<BookmarkEntry> _entries;

    internal BookmarkCategorySelection(
        BookmarkCategoryName category,
        IReadOnlyList<BookmarkEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(entries);
        Category = category;
        _entries = new List<BookmarkEntry>(entries).AsReadOnly();
    }

    /// <summary>Gets the category spelling captured for the action.</summary>
    public BookmarkCategoryName Category { get; }

    /// <summary>Gets the complete ordered entries captured from that category.</summary>
    public IReadOnlyList<BookmarkEntry> Entries => _entries;
}
