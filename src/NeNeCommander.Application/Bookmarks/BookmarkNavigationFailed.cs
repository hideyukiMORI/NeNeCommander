using System;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Bookmarks;

/// <summary>Retains the manager state and selection after the canonical pane read fails.</summary>
public sealed record BookmarkNavigationFailed : BookmarksEditorState
{
    internal BookmarkNavigationFailed(
        BookmarkBrowseContext returnContext,
        BookmarkSelection selection,
        PaneActivity reason)
    {
        ArgumentNullException.ThrowIfNull(returnContext);
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(reason);
        if (reason is not PaneReadFailed and not PaneReadCancelled)
        {
            throw new ArgumentException(
                "A bookmark navigation failure requires a completed read rejection.",
                nameof(reason));
        }
        ReturnContext = returnContext;
        Selection = selection;
        Reason = reason;
    }

    /// <summary>Gets the browse state retained for retry.</summary>
    public BookmarkBrowseContext ReturnContext { get; }

    /// <summary>Gets the complete entry whose navigation failed.</summary>
    public BookmarkSelection Selection { get; }

    /// <summary>Gets the closed pane read outcome that explains why navigation did not complete.</summary>
    public PaneActivity Reason { get; }
}
