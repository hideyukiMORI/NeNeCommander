namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents collections rejected before they became a bookmark catalog.</summary>
public sealed record BookmarkCatalogRejected : BookmarkCatalogCreationOutcome
{
    private protected override void RestrictDerivation()
    {
    }

    internal BookmarkCatalogRejected(BookmarkCatalogFailureKind kind)
    {
        Kind = kind;
    }

    /// <summary>Gets the closed rejection reason.</summary>
    public BookmarkCatalogFailureKind Kind { get; }
}
