using System;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Application.Input;

/// <summary>Carries one explicit decision and operation-scoped application scope from the conflict modal.</summary>
public sealed record ConflictDecisionSubmission : UserIntent
{
    internal ConflictDecisionSubmission(TransferConflictDecision decision, TransferConflictScope scope)
    {
        ArgumentNullException.ThrowIfNull(decision);
        ArgumentNullException.ThrowIfNull(scope);
        Decision = decision;
        Scope = scope;
    }

    /// <summary>Gets the selected collision decision.</summary>
    public TransferConflictDecision Decision { get; }
    /// <summary>Gets whether the decision addresses one or all currently shown conflicts.</summary>
    public TransferConflictScope Scope { get; }
}
