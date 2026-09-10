using System;

namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents the dedicated category deletion confirmation.</summary>
public sealed record BookmarkCategoryDeleteConfirmation : BookmarksEditorState
{
    internal BookmarkCategoryDeleteConfirmation(
        BookmarkBrowseContext returnContext,
        BookmarkCategorySelection selection)
    {
        ArgumentNullException.ThrowIfNull(returnContext);
        ArgumentNullException.ThrowIfNull(selection);
        ReturnContext = returnContext;
        Selection = selection;
    }

    /// <summary>Gets the browse state restored by Cancel or successful deletion.</summary>
    public BookmarkBrowseContext ReturnContext { get; }

    /// <summary>Gets the unchanged category and affected entries awaiting confirmation.</summary>
    public BookmarkCategorySelection Selection { get; }
}
