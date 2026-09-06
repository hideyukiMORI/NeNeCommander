using System;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Provides the exact paths and safe initial focus for the current transfer conflict.</summary>
public sealed record ActiveConflictModal : ConflictModalPresentation
{
    internal ActiveConflictModal(
        ConflictSet conflicts,
        TransferConflictDecision initialFocus,
        TransferContinuation continuation)
    {
        ArgumentNullException.ThrowIfNull(conflicts);
        ArgumentNullException.ThrowIfNull(initialFocus);
        ArgumentNullException.ThrowIfNull(continuation);
        TransferConflict first = conflicts.Conflicts[0];
        SourceText = first.Source.Path.CanonicalText;
        ExistingTargetText = first.ExistingTarget.CanonicalText;
        KeepBothCandidateText = first.KeepBothCandidate.CanonicalText;
        ConflictCount = conflicts.Conflicts.Count;
        InitialFocus = initialFocus;
        Continuation = continuation;
    }

    /// <summary>Gets the frozen source path shown in the modal.</summary>
    public string SourceText { get; }
    /// <summary>Gets the colliding target path shown in the modal.</summary>
    public string ExistingTargetText { get; }
    /// <summary>Gets the provider-validated KeepBoth candidate shown in the modal.</summary>
    public string KeepBothCandidateText { get; }
    /// <summary>Gets the number of conflicts found by complete-batch preflight.</summary>
    public int ConflictCount { get; }
    /// <summary>Gets the decision whose control receives focus when the modal opens.</summary>
    public TransferConflictDecision InitialFocus { get; }
    internal TransferContinuation Continuation { get; }
}
