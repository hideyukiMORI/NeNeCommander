namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents the closed result of one complete bookmark-catalog mutation.</summary>
public abstract record BookmarkCatalogMutationOutcome
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    private protected BookmarkCatalogMutationOutcome()
    {
    }
}
