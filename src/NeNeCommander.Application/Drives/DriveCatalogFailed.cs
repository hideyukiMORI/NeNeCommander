using System;

namespace NeNeCommander.Application.Drives;

/// <summary>Represents one expected drive listing failure.</summary>
public sealed record DriveCatalogFailed : DriveCatalogOutcome
{
    internal DriveCatalogFailed(DriveCatalogFailureKind failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        Failure = failure;
    }

    /// <summary>Gets the normalized failure.</summary>
    public DriveCatalogFailureKind Failure { get; }
}
