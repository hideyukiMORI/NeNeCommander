using System;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Input;

/// <summary>Carries one explicit decision and operation-scoped application scope from the conflict modal.</summary>
public sealed record ConflictDecisionSubmission : UserIntent
{
    private protected override void RestrictDerivation()
    {
    }

    internal ConflictDecisionSubmission(
        OperationAwaitingConflict expectedState, TransferConflictDecision decision, TransferConflictScope scope)
    {
        ArgumentNullException.ThrowIfNull(expectedState);
        ArgumentNullException.ThrowIfNull(decision);
        ArgumentNullException.ThrowIfNull(scope);
        ExpectedState = expectedState;
        Decision = decision;
        Scope = scope;
    }

    /// <summary>Gets the exact rendered operation that owns this decision.</summary>
    public OperationAwaitingConflict ExpectedState { get; }
    /// <summary>Gets the selected collision decision.</summary>
    public TransferConflictDecision Decision { get; }
    /// <summary>Gets whether the decision addresses one or all currently shown conflicts.</summary>
    public TransferConflictScope Scope { get; }
}
