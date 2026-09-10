namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents collections accepted as one complete bookmark catalog.</summary>
public sealed record BookmarkCatalogAccepted : BookmarkCatalogCreationOutcome
{
    internal BookmarkCatalogAccepted(BookmarkCatalog catalog)
    {
        Catalog = catalog;
    }

    /// <summary>Gets the accepted immutable catalog.</summary>
    public BookmarkCatalog Catalog { get; }
}
