using System;
using NeNeCommander.Application.Drives;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Directories;

/// <summary>Represents one complete, successful directory read.</summary>
public sealed record DirectoryReadSucceeded : DirectoryReadOutcome
{
    internal DirectoryReadSucceeded(DirectoryListing listing, DriveKind? driveKind)
    {
        ArgumentNullException.ThrowIfNull(listing);
        if (driveKind is not null && listing.Location is not WindowsLocalPath)
        {
            throw new ArgumentException("A drive kind requires a Windows local listing.", nameof(driveKind));
        }
        Listing = listing;
        DriveKind = driveKind;
    }

    /// <summary>Gets the validated, deterministically ordered listing.</summary>
    public DirectoryListing Listing { get; }

    /// <summary>Gets the drive kind observed with this read, or absence when none was reported.</summary>
    public DriveKind? DriveKind { get; }
}
