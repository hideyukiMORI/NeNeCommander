namespace NeNeCommander.Application.FileOperations;

/// <summary>Contains a normalized preflight failure.</summary>
public sealed record TransferPreflightRejected : TransferPreflightOutcome
{
    internal TransferPreflightRejected(FileOperationFailureKind failure)
    {
        Failure = failure;
    }

    /// <summary>Gets the normalized rejection reason.</summary>
    public override FileOperationFailureKind Failure { get; }
}
