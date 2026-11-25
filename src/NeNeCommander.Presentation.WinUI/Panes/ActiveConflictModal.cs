using System;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Provides the exact operation, paths and safe initial focus for one transfer conflict.</summary>
public sealed record ActiveConflictModal : ConflictModalPresentation
{
    private protected override void RestrictDerivation()
    {
    }

    internal ActiveConflictModal(OperationAwaitingConflict sourceState)
    {
        ArgumentNullException.ThrowIfNull(sourceState);
        TransferConflict first = sourceState.Conflicts.Conflicts[0];
        SourceText = first.Source.Path.CanonicalText;
        ExistingTargetText = first.ExistingTarget.CanonicalText;
        KeepBothCandidateText = first.KeepBothCandidate.CanonicalText;
        SourceState = sourceState;
    }

    /// <summary>Gets the exact rendered operation that qualifies every modal callback.</summary>
    public OperationAwaitingConflict SourceState { get; }
    /// <summary>Gets the frozen source path shown in the modal.</summary>
    public string SourceText { get; }
    /// <summary>Gets the colliding target path shown in the modal.</summary>
    public string ExistingTargetText { get; }
    /// <summary>Gets the provider-validated KeepBoth candidate shown in the modal.</summary>
    public string KeepBothCandidateText { get; }
    /// <summary>Gets the number of conflicts found by complete-batch preflight.</summary>
    public int ConflictCount => SourceState.Conflicts.Conflicts.Count;
    /// <summary>Gets the decision whose control receives focus when the modal opens.</summary>
    public TransferConflictDecision InitialFocus => SourceState.InitialFocus;
}
