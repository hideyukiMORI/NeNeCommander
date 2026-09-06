using System;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Associates one frozen source with its explicit collision decision.</summary>
public sealed record TransferConflictChoice
{
    private TransferConflictChoice(
        FileSystemPath source,
        TransferConflictDecision decision,
        FileSystemPath keepBothCandidate)
    {
        Source = source;
        Decision = decision;
        KeepBothCandidate = keepBothCandidate;
    }

    /// <summary>Gets the source identity addressed by the choice.</summary>
    public FileSystemPath Source { get; }
    /// <summary>Gets the explicit decision.</summary>
    public TransferConflictDecision Decision { get; }
    /// <summary>Gets the exact candidate accepted with KeepBoth.</summary>
    public FileSystemPath KeepBothCandidate { get; }

    /// <summary>Creates a choice admitted by the supplied conflict.</summary>
    public static TransferConflictChoice Create(
        TransferConflict conflict,
        TransferConflictDecision decision)
    {
        ArgumentNullException.ThrowIfNull(conflict);
        ArgumentNullException.ThrowIfNull(decision);
        return new TransferConflictChoice(
            conflict.Source.Path,
            decision,
            conflict.KeepBothCandidate);
    }
}
