namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Represents a closed preflight inspection success or expected failure.
/// </summary>
public abstract record FileInspectionOutcome
{
    private protected FileInspectionOutcome()
    {
    }

    /// <summary>Creates a successful inspection outcome.</summary>
    /// <param name="snapshot">Complete provider snapshot.</param>
    /// <returns>A successful inspection.</returns>
    public static FileInspectionOutcome Succeeded(FileEntrySnapshot snapshot)
    {
        return new FileInspectionSucceeded(snapshot);
    }

    /// <summary>Creates a failed inspection outcome.</summary>
    /// <param name="failure">Normalized expected failure.</param>
    /// <returns>A failed inspection.</returns>
    public static FileInspectionOutcome Failed(FileOperationFailureKind failure)
    {
        return new FileInspectionFailed(failure);
    }
}
