using System;

namespace NeNeCommander.Application.Bookmarks;

/// <summary>
/// Represents the dedicated bookmark deletion confirmation (ADR-0080). The catalog is unchanged
/// and nothing is saved until the deletion is confirmed.
/// </summary>
public sealed record BookmarkDeleteConfirmation : BookmarksEditorState
{
    private protected override void RestrictDerivation()
    {
    }

    internal BookmarkDeleteConfirmation(
        BookmarkBrowseContext returnContext,
        BookmarkSelection selection)
    {
        ArgumentNullException.ThrowIfNull(returnContext);
        ArgumentNullException.ThrowIfNull(selection);
        ReturnContext = returnContext;
        Selection = selection;
    }

    /// <summary>Gets the browse state restored by Cancel or successful deletion.</summary>
    public BookmarkBrowseContext ReturnContext { get; }

    /// <summary>Gets the unchanged bookmark awaiting confirmation.</summary>
    public BookmarkSelection Selection { get; }
}
