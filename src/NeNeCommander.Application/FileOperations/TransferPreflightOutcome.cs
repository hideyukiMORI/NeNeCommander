using System;
using System.Collections.Generic;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Represents the closed result of complete-batch transfer preflight.</summary>
public abstract record TransferPreflightOutcome
{
    private protected TransferPreflightOutcome()
    {
    }

    /// <summary>Gets the normalized rejection, or absence for a successful plan.</summary>
    public abstract FileOperationFailureKind? Failure { get; }

    /// <summary>Creates a successful exact transfer plan.</summary>
    public static TransferPreflightOutcome Succeeded(IReadOnlyList<TransferPlanEntry> plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return new TransferPreflightSucceeded(new List<TransferPlanEntry>(plan).AsReadOnly());
    }

    /// <summary>Creates a fail-closed preflight rejection.</summary>
    public static TransferPreflightOutcome Rejected(FileOperationFailureKind failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return new TransferPreflightRejected(failure);
    }

    /// <summary>Creates a non-mutating conflict set.</summary>
    public static TransferPreflightOutcome Conflicted(IReadOnlyList<TransferConflict> conflicts)
    {
        ArgumentNullException.ThrowIfNull(conflicts);
        _ = conflicts.Count > 0
            ? conflicts
            : throw new ArgumentException("A conflict set requires at least one conflict.", nameof(conflicts));
        return new ConflictSet(new List<TransferConflict>(conflicts).AsReadOnly());
    }
}
