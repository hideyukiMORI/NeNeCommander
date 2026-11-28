using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Provides the exact operation, paths and safe initial focus for one transfer conflict.</summary>
public sealed record ActiveConflictModal : ConflictModalPresentation
{
    private readonly ImmutableList<TransferConflict> _otherOverwriteConflicts;
    private protected override void RestrictDerivation()
    {
    }

    internal ActiveConflictModal(OperationAwaitingConflict sourceState, EntryMetadataFormat metadataFormat)
    {
        ArgumentNullException.ThrowIfNull(sourceState);
        ArgumentNullException.ThrowIfNull(metadataFormat);
        TransferConflict first = sourceState.Conflicts.Conflicts[0];
        SourceText = first.Source.Path.CanonicalText;
        ExistingTargetText = first.ExistingTarget.CanonicalText;
        KeepBothCandidateText = first.KeepBothCandidate.CanonicalText;
        SourceState = sourceState;
        MetadataFormat = metadataFormat;
        Overwrite = first.Target.Overwrite;
        _otherOverwriteConflicts = Overwrite is TransferOverwriteCapability.QualifiedOverwrite
            ? [.. sourceState.Conflicts.Conflicts.Skip(1).Where(conflict => conflict.Target.OverwriteEvidence is not null)]
            : [];
    }

    internal EntryMetadataFormat MetadataFormat { get; }
    /// <summary>Gets the first conflict's sole admitted capability or explicit refusal.</summary>
    public TransferOverwriteCapability Overwrite { get; }
    /// <summary>Gets only the other already-disclosed eligible conflicts; later discoveries cannot inherit this set.</summary>
    public IReadOnlyList<TransferConflict> OtherOverwriteConflicts => _otherOverwriteConflicts;
    /// <summary>Gets whether the displayed current file admits the explicit overwrite action.</summary>
    public bool CanOverwrite => Overwrite is TransferOverwriteCapability.QualifiedOverwrite;
    /// <summary>Gets whether there is a complete disclosed set beyond the current target.</summary>
    public bool CanOverwriteAll => CanOverwrite && OtherOverwriteConflicts.Count > 0;

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
