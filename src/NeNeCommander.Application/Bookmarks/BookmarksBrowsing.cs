using System;

namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents normal manager browsing with an optional correctable rejection.</summary>
public sealed record BookmarksBrowsing : BookmarksEditorState
{
    internal BookmarksBrowsing(BookmarkBrowseContext context, BookmarkEditorProblem? problem)
    {
        ArgumentNullException.ThrowIfNull(context);
        Context = context;
        Problem = problem;
    }

    /// <summary>Gets the current browse inputs and selection.</summary>
    public BookmarkBrowseContext Context { get; }

    /// <summary>Gets the last user-correctable rejection, when present.</summary>
    public BookmarkEditorProblem? Problem { get; }
}
