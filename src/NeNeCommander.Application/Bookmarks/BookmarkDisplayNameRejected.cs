namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents bookmark-name text rejected before it became application state.</summary>
public sealed record BookmarkDisplayNameRejected : BookmarkDisplayNameParseOutcome
{
    internal BookmarkDisplayNameRejected(BookmarkTextFailureKind kind)
    {
        Kind = kind;
    }

    /// <summary>Gets the closed rejection reason.</summary>
    public BookmarkTextFailureKind Kind { get; }
}
