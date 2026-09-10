using System;

namespace NeNeCommander.Application.Bookmarks;

/// <summary>Retains browse inputs and the complete stale-safe selection across nested editor states.</summary>
public sealed record BookmarkBrowseContext
{
    /// <summary>Initializes one immutable manager browse context.</summary>
    public BookmarkBrowseContext(
        string searchText,
        BookmarkCategoryFilter filter,
        BookmarkSelection? selection)
    {
        ArgumentNullException.ThrowIfNull(searchText);
        ArgumentNullException.ThrowIfNull(filter);
        SearchText = searchText;
        Filter = filter;
        Selection = selection;
    }

    /// <summary>Gets the verbatim search text.</summary>
    public string SearchText { get; }

    /// <summary>Gets the closed category filter.</summary>
    public BookmarkCategoryFilter Filter { get; }

    /// <summary>Gets the complete selected entry, when one is selected.</summary>
    public BookmarkSelection? Selection { get; }
}
