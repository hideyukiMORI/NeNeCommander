namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents a complete accepted replacement catalog.</summary>
public sealed record BookmarkCatalogChanged : BookmarkCatalogMutationOutcome
{
    internal BookmarkCatalogChanged(BookmarkCatalog catalog)
    {
        Catalog = catalog;
    }

    /// <summary>Gets the complete immutable replacement catalog.</summary>
    public BookmarkCatalog Catalog { get; }
}
