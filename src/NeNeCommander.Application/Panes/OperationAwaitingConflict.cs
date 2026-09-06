using System;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Application.Panes;

/// <summary>Owns one frozen transfer continuation while the conflict modal awaits input.</summary>
public sealed record OperationAwaitingConflict : OperationActivity
{
    internal OperationAwaitingConflict(
        OperationKind kind,
        ConflictSet conflicts,
        TransferContinuation continuation)
    {
        ArgumentNullException.ThrowIfNull(kind);
        ArgumentNullException.ThrowIfNull(conflicts);
        ArgumentNullException.ThrowIfNull(continuation);
        Kind = kind;
        Conflicts = conflicts;
        Continuation = continuation;
    }

    /// <summary>Gets the transfer kind being resolved.</summary>
    public OperationKind Kind { get; }
    /// <summary>Gets every conflict found by the latest complete-batch preflight.</summary>
    public ConflictSet Conflicts { get; }
    /// <summary>Gets the original frozen transfer continuation.</summary>
    public TransferContinuation Continuation { get; }
    /// <summary>Gets the deliberately safe initial focus decision.</summary>
    public TransferConflictDecision InitialFocus { get; } = TransferConflictDecision.Cancel;
}
