using NeNeCommander.Application.Drives;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Application.Directories;

/// <summary>
/// Represents the closed success, cancellation, or expected failure of one directory read.
/// </summary>
public abstract record DirectoryReadOutcome
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    internal DirectoryReadOutcome()
    {
    }

    /// <summary>Creates a successful read outcome.</summary>
    /// <param name="listing">Validated listing.</param>
    /// <param name="driveKind">Drive kind observed with a Windows local read, or absence when none was reported.</param>
    /// <returns>A successful outcome.</returns>
    public static DirectoryReadOutcome Succeeded(DirectoryListing listing, DriveKind? driveKind)
    {
        return new DirectoryReadSucceeded(listing, driveKind);
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
