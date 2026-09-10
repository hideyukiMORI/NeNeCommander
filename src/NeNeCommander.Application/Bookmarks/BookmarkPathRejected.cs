namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents path text rejected before it became bookmark metadata.</summary>
public sealed record BookmarkPathRejected : BookmarkPathParseOutcome
{
    internal BookmarkPathRejected(BookmarkPathFailureKind kind)
    {
        Kind = kind;
    }

    /// <summary>Gets the closed rejection reason.</summary>
    public BookmarkPathFailureKind Kind { get; }
}
