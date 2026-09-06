using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Contains every unresolved collision found by complete-batch preflight.</summary>
public sealed record ConflictSet : TransferPreflightOutcome
{
    internal ConflictSet(ReadOnlyCollection<TransferConflict> conflicts)
    {
        Conflicts = conflicts;
    }

    /// <summary>Gets the ordered unresolved conflicts.</summary>
    public IReadOnlyList<TransferConflict> Conflicts { get; }
    /// <inheritdoc />
    public override FileOperationFailureKind Failure => FileOperationFailureKind.Conflict;
}
