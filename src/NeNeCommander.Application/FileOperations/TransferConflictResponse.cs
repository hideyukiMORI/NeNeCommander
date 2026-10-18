using System;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Pairs one explicit conflict decision with the operation scope selected by the user.</summary>
public sealed record TransferConflictResponse
{
    private TransferConflictResponse(TransferConflictDecision decision, TransferConflictScope scope)
    {
        Decision = decision;
        Scope = scope;
    }

    /// <summary>Gets the explicit decision; no response supplies a default choice.</summary>
    public TransferConflictDecision Decision { get; }

    /// <summary>Gets whether the decision applies to the current or all pending conflicts.</summary>
    public TransferConflictScope Scope { get; }

    /// <summary>Creates a complete response without changing or consuming any continuation.</summary>
    public static TransferConflictResponse Create(TransferConflictDecision decision, TransferConflictScope scope)
    {
        ArgumentNullException.ThrowIfNull(decision);
        ArgumentNullException.ThrowIfNull(scope);
        return new TransferConflictResponse(decision, scope);
    }
}
