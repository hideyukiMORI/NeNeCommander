using System;

namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents a manager navigation whose existing pane read has not settled.</summary>
public sealed record BookmarkNavigationPending : BookmarksEditorState
{
    internal BookmarkNavigationPending(
        BookmarkBrowseContext returnContext,
        BookmarkSelection selection)
    {
        ArgumentNullException.ThrowIfNull(returnContext);
        ArgumentNullException.ThrowIfNull(selection);
        ReturnContext = returnContext;
        Selection = selection;
    }

    /// <summary>Gets the browse state retained during navigation.</summary>
    public BookmarkBrowseContext ReturnContext { get; }

    /// <summary>Gets the complete entry admitted to canonical navigation.</summary>
    public BookmarkSelection Selection { get; }
}
