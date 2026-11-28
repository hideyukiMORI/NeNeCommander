using System;
using System.Linq;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Associates one frozen source with its explicit collision decision.</summary>
public sealed record TransferConflictChoice
{
    private TransferConflictChoice(
        FileSystemPath source,
        TransferConflictDecision decision,
        FileSystemPath keepBothCandidate,
        TransferConflictTarget target)
    {
        Source = source;
        Decision = decision;
        KeepBothCandidate = keepBothCandidate;
        Target = target;
    }

    /// <summary>Gets the source identity addressed by the choice.</summary>
    public FileSystemPath Source { get; }
    /// <summary>Gets the explicit decision.</summary>
    public TransferConflictDecision Decision { get; }
    /// <summary>Gets the exact candidate accepted with KeepBoth.</summary>
    public FileSystemPath KeepBothCandidate { get; }

    /// <summary>Gets the exact original conflict target binding accepted with this choice.</summary>
    public TransferConflictTarget Target { get; }

    /// <summary>Creates a choice admitted by the supplied conflict.</summary>
    public static TransferConflictChoice Create(
        TransferConflict conflict,
        TransferConflictDecision decision)
    {
        ArgumentNullException.ThrowIfNull(conflict);
        ArgumentNullException.ThrowIfNull(decision);
        return !conflict.AllowedDecisions.Contains(decision)
            ? throw new ArgumentException("The conflict does not admit this decision.", nameof(decision))
            : new TransferConflictChoice(
                conflict.Source.Path,
                decision,
                conflict.KeepBothCandidate,
                conflict.Target);
    }
}
