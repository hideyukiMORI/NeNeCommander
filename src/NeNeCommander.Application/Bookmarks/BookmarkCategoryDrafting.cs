using System;

namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents category add or rename form ownership.</summary>
public sealed record BookmarkCategoryDrafting : BookmarksEditorState
{
    internal BookmarkCategoryDrafting(
        BookmarkBrowseContext returnContext,
        BookmarkCategorySelection? original,
        string name,
        BookmarkEditorProblem? problem)
    {
        ArgumentNullException.ThrowIfNull(returnContext);
        ArgumentNullException.ThrowIfNull(name);
        ReturnContext = returnContext;
        Original = original;
        Name = name;
        Problem = problem;
    }

    /// <summary>Gets the browse state restored by Cancel or successful Save.</summary>
    public BookmarkBrowseContext ReturnContext { get; }

    /// <summary>Gets the complete category being renamed, or absence for creation.</summary>
    public BookmarkCategorySelection? Original { get; }

    /// <summary>Gets the current untrusted category name.</summary>
    public string Name { get; }

    /// <summary>Gets the last user-correctable rejection, when present.</summary>
    public BookmarkEditorProblem? Problem { get; }
}
