namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents category text rejected before it became application state.</summary>
public sealed record BookmarkCategoryNameRejected : BookmarkCategoryNameParseOutcome
{
    internal BookmarkCategoryNameRejected(BookmarkTextFailureKind kind)
    {
        Kind = kind;
    }

    /// <summary>Gets the closed rejection reason.</summary>
    public BookmarkTextFailureKind Kind { get; }
}
