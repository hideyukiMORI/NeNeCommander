using System;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Pairs the returned conflict set with the gateway-issued continuation that resumes it.</summary>
internal sealed record TransferConflictPause
{
    private TransferConflictPause(ConflictSet conflicts, TransferContinuation continuation)
    {
        Conflicts = conflicts;
        Continuation = continuation;
    }

    internal ConflictSet Conflicts { get; }
    internal TransferContinuation Continuation { get; }

    internal static TransferConflictPause Create(ConflictSet conflicts, TransferContinuation continuation)
    {
        ArgumentNullException.ThrowIfNull(conflicts);
        ArgumentNullException.ThrowIfNull(continuation);
        return new TransferConflictPause(conflicts, continuation);
    }
}
