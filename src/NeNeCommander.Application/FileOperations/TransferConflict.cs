using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Describes one source whose ordinary target already exists.</summary>
public sealed record TransferConflict
{
    private readonly ReadOnlyCollection<TransferConflictDecision> _allowedDecisions;

    private TransferConflict(
        FileEntrySnapshot source,
        FileSystemPath existingTarget,
        FileSystemPath keepBothCandidate,
        ReadOnlyCollection<TransferConflictDecision> allowedDecisions)
    {
        Source = source;
        ExistingTarget = existingTarget;
        KeepBothCandidate = keepBothCandidate;
        _allowedDecisions = allowedDecisions;
    }

    /// <summary>Gets the original frozen source.</summary>
    public FileEntrySnapshot Source { get; }
    /// <summary>Gets the ordinary target that already exists or is reserved.</summary>
    public FileSystemPath ExistingTarget { get; }
    /// <summary>Gets the provider-validated alternate target shown to the user.</summary>
    public FileSystemPath KeepBothCandidate { get; }
    /// <summary>Gets the decisions the provider permits for this conflict.</summary>
    public IReadOnlyList<TransferConflictDecision> AllowedDecisions => _allowedDecisions;

    /// <summary>Creates one complete Windows-local transfer conflict.</summary>
    public static TransferConflict Create(
        FileEntrySnapshot source,
        FileSystemPath existingTarget,
        FileSystemPath keepBothCandidate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(existingTarget);
        ArgumentNullException.ThrowIfNull(keepBothCandidate);
        return new TransferConflict(
            source,
            existingTarget,
            keepBothCandidate,
            Array.AsReadOnly([
                TransferConflictDecision.Skip,
                TransferConflictDecision.KeepBoth,
                TransferConflictDecision.Cancel]));
    }
}
