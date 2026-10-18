namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents the closed result of validating a complete immutable bookmark catalog.</summary>
public abstract record BookmarkCatalogCreationOutcome
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    private protected BookmarkCatalogCreationOutcome()
    {
    }
}
