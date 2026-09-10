using System;

namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents bookmark add or edit form ownership.</summary>
public sealed record BookmarkDrafting : BookmarksEditorState
{
    internal BookmarkDrafting(
        BookmarkBrowseContext returnContext,
        BookmarkSelection? original,
        BookmarkDraft draft,
        BookmarkEditorProblem? problem)
    {
        ArgumentNullException.ThrowIfNull(returnContext);
        ArgumentNullException.ThrowIfNull(draft);
        ReturnContext = returnContext;
        Original = original;
        Draft = draft;
        Problem = problem;
    }

    /// <summary>Gets the browse state restored by Cancel or successful Save.</summary>
    public BookmarkBrowseContext ReturnContext { get; }

    /// <summary>Gets the complete entry being edited, or absence for registration.</summary>
    public BookmarkSelection? Original { get; }

    /// <summary>Gets the complete current untrusted form draft.</summary>
    public BookmarkDraft Draft { get; }

    /// <summary>Gets the last user-correctable rejection, when present.</summary>
    public BookmarkEditorProblem? Problem { get; }
}
