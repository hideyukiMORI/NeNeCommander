using System;

namespace NeNeCommander.Application.Directories;

/// <summary>Represents one complete, successful directory read.</summary>
public sealed record DirectoryReadSucceeded : DirectoryReadOutcome
{
    internal DirectoryReadSucceeded(DirectoryListing listing)
    {
        ArgumentNullException.ThrowIfNull(listing);
        Listing = listing;
    }

    /// <summary>Gets the validated, deterministically ordered listing.</summary>
    public DirectoryListing Listing { get; }
}
