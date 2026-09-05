using System;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Reports the normalized failure that prevented a provider capability decision.</summary>
public sealed record AtomicMoveCapabilityFailed : AtomicMoveCapabilityOutcome
{
    internal AtomicMoveCapabilityFailed(FileOperationFailureKind failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        Failure = failure;
    }

    /// <summary>Gets the failure that stops the transfer batch before any mutation starts.</summary>
    public FileOperationFailureKind Failure { get; }
}
