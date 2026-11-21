using System;

namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents normal manager browsing with an optional correctable rejection.</summary>
public sealed record BookmarksBrowsing : BookmarksEditorState
{
    private protected override void RestrictDerivation()
    {
    }

    internal BookmarksBrowsing(
        BookmarkBrowseContext context,
        BookmarkEditorProblem? problem,
        BookmarkSelection? savedSelection = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        Context = context;
        Problem = problem;
        SavedSelection = savedSelection;
    }

    /// <summary>Gets the current browse inputs and selection.</summary>
    public BookmarkBrowseContext Context { get; }

    /// <summary>Gets the last user-correctable rejection, when present.</summary>
    public BookmarkEditorProblem? Problem { get; }

    /// <summary>Gets the most recently saved bookmark while its return-to-list notice is current.</summary>
    public BookmarkSelection? SavedSelection { get; }
}
