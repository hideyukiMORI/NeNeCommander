using System;
using NeNeCommander.Application.Drives;

namespace NeNeCommander.Application.Locations;

/// <summary>Represents a drives section whose listing failed; it lists nothing and is never success.</summary>
public sealed record DriveSectionFailed : DriveSection
{
    internal DriveSectionFailed(DriveCatalogFailureKind failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        Failure = failure;
    }

    /// <summary>Gets the normalized listing failure.</summary>
    public DriveCatalogFailureKind Failure { get; }
}
