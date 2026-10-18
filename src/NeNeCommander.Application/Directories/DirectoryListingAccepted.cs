namespace NeNeCommander.Application.Directories;

/// <summary>
/// Represents an accepted, deterministically ordered directory listing.
/// </summary>
public sealed record DirectoryListingAccepted : DirectoryListingCreation
{
    private protected override void RestrictDerivation()
    {
    }

    internal DirectoryListingAccepted(DirectoryListing listing)
    {
        Listing = listing;
    }

    /// <summary>Gets the validated listing.</summary>
    public DirectoryListing Listing { get; }
}
