using System.Collections.Generic;

namespace NeNeCommander.Application.Drives;

/// <summary>Represents the closed success, cancellation, or expected failure of drive listing.</summary>
public abstract record DriveCatalogOutcome
{
    internal DriveCatalogOutcome()
    {
    }

    /// <summary>Creates a successful listing outcome.</summary>
    /// <param name="drives">Validated volumes in provider order.</param>
    /// <param name="unrepresentableRootCount">Number of reported roots that are not drive roots.</param>
    /// <returns>The successful outcome.</returns>
    public static DriveCatalogOutcome Succeeded(IReadOnlyList<DriveLocation> drives, int unrepresentableRootCount)
    {
        return new DriveCatalogSucceeded(drives, unrepresentableRootCount);
    }

    /// <summary>Creates the cancelled listing outcome.</summary>
    /// <returns>The cancelled outcome.</returns>
    public static DriveCatalogOutcome Cancelled()
    {
        return new DriveCatalogCancelled();
    }

    /// <summary>Creates an expected failed listing outcome.</summary>
    /// <param name="failure">Normalized failure.</param>
    /// <returns>The failed outcome.</returns>
    public static DriveCatalogOutcome Failed(DriveCatalogFailureKind failure)
    {
        return new DriveCatalogFailed(failure);
    }
}
