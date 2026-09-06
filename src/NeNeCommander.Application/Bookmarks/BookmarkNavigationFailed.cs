using System;

namespace NeNeCommander.Application.Bookmarks;

/// <summary>Retains the manager state and selection after the canonical pane read fails.</summary>
public sealed record BookmarkNavigationFailed : BookmarksEditorState
{
    internal BookmarkNavigationFailed(
        BookmarkBrowseContext returnContext,
        BookmarkSelection selection)
    {
        ArgumentNullException.ThrowIfNull(returnContext);
        ArgumentNullException.ThrowIfNull(selection);
        ReturnContext = returnContext;
        Selection = selection;
    }

    /// <summary>Gets the browse state retained for retry.</summary>
    public BookmarkBrowseContext ReturnContext { get; }

    /// <summary>Gets the complete entry whose navigation failed.</summary>
    public BookmarkSelection Selection { get; }
}
