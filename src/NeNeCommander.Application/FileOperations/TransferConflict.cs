using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Describes one source whose ordinary target already exists.</summary>
public sealed record TransferConflict
{
    private readonly ImmutableList<TransferConflictDecision> _allowedDecisions;

    private TransferConflict(
        FileEntrySnapshot source,
        TransferConflictTarget target,
        FileSystemPath keepBothCandidate)
    {
        Source = source;
        Target = target;
        KeepBothCandidate = keepBothCandidate;
        _allowedDecisions = target.OverwriteEvidence is null
            ? [TransferConflictDecision.Skip, TransferConflictDecision.KeepBoth, TransferConflictDecision.Cancel]
            : [TransferConflictDecision.Skip, TransferConflictDecision.KeepBoth, TransferConflictDecision.OverwriteContents, TransferConflictDecision.Cancel];
    }

    /// <summary>Gets the original frozen source.</summary>
    public FileEntrySnapshot Source { get; }
    /// <summary>Gets the ordinary target that already exists or is reserved.</summary>
    public FileSystemPath ExistingTarget => Target.Path;
    /// <summary>Gets the original target binding that every resulting choice must retain.</summary>
    public TransferConflictTarget Target { get; }
    /// <summary>Gets the provider-validated alternate target shown to the user.</summary>
    public FileSystemPath KeepBothCandidate { get; }
    /// <summary>Gets the decisions the provider permits for this conflict.</summary>
    public IReadOnlyList<TransferConflictDecision> AllowedDecisions => _allowedDecisions;

    /// <summary>Creates one complete Windows-local transfer conflict.</summary>
    public static TransferConflict Create(
        FileEntrySnapshot source,
        FileSystemPath existingTarget,
        FileSystemPath keepBothCandidate,
        TransferOverwriteCapability overwrite)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(existingTarget);
        ArgumentNullException.ThrowIfNull(keepBothCandidate);
        ArgumentNullException.ThrowIfNull(overwrite);
        TransferOverwriteEvidence? overwriteEvidence = (overwrite as TransferOverwriteCapability.QualifiedOverwrite)?.Evidence;
        return overwriteEvidence is not null &&
            (source.Identity != overwriteEvidence.Source.Observation.Entry.Identity ||
                !FileSystemPathIdentityComparer.Instance.Equals(source.Path, overwriteEvidence.Source.Observation.Entry.Path))
            ? throw new ArgumentException("The overwrite evidence does not describe the frozen source.", nameof(overwrite))
            : new TransferConflict(source, TransferConflictTarget.Create(existingTarget, overwrite), keepBothCandidate);
    }
}
