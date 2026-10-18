namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents a catalog mutation rejected without changing the current catalog.</summary>
public sealed record BookmarkCatalogChangeRejected : BookmarkCatalogMutationOutcome
{
    private protected override void RestrictDerivation()
    {
    }

    internal BookmarkCatalogChangeRejected(BookmarkCatalogFailureKind kind)
    {
        Kind = kind;
    }

    /// <summary>Gets the closed rejection reason.</summary>
    public BookmarkCatalogFailureKind Kind { get; }
}
