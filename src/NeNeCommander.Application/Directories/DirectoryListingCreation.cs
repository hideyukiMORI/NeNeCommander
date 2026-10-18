namespace NeNeCommander.Application.Directories;

/// <summary>
/// Represents the closed result of validating an adapter-supplied directory listing.
/// </summary>
public abstract record DirectoryListingCreation
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    private protected DirectoryListingCreation()
    {
    }
}
