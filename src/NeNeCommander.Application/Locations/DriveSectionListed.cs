using System;
using System.Collections.Generic;
using NeNeCommander.Application.Drives;

namespace NeNeCommander.Application.Locations;

/// <summary>Represents a drives section whose listing succeeded, in provider order.</summary>
public sealed record DriveSectionListed : DriveSection
{
    internal DriveSectionListed(DriveCatalogSucceeded listing)
    {
        ArgumentNullException.ThrowIfNull(listing);
        List<DriveLocationItem> items = new(listing.Drives.Count);
        foreach (DriveLocation drive in listing.Drives)
        {
            items.Add(new DriveLocationItem(drive));
        }
        Items = items.AsReadOnly();
        UnrepresentableRootCount = listing.UnrepresentableRootCount;
    }

    /// <summary>Gets the listed drive entries in provider order.</summary>
    public IReadOnlyList<DriveLocationItem> Items { get; }

    /// <summary>Gets the number of reported roots that are not drive roots and are not listed.</summary>
    public int UnrepresentableRootCount { get; }
}
