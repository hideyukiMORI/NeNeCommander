using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Application.Directories;

/// <summary>
/// Represents the closed success, cancellation, or expected failure of one directory read.
/// </summary>
public abstract record DirectoryReadOutcome
{
    private protected DirectoryReadOutcome()
    {
    }

    /// <summary>Creates a successful read outcome.</summary>
    /// <param name="listing">Validated listing.</param>
    /// <returns>A successful outcome.</returns>
    public static DirectoryReadOutcome Succeeded(DirectoryListing listing)
    {
        return new DirectoryReadSucceeded(listing);
    }

    /// <summary>Creates the outcome for cancellation observed before the listing was complete.</summary>
    /// <returns>The cancelled outcome.</returns>
    public static DirectoryReadOutcome Cancelled()
    {
        return new DirectoryReadCancelled();
    }

    /// <summary>Creates a failed read outcome.</summary>
    /// <param name="failure">Normalized expected failure.</param>
    /// <returns>A failed outcome.</returns>
    public static DirectoryReadOutcome Failed(FileOperationFailureKind failure)
    {
        return new DirectoryReadFailed(failure);
    }
}
