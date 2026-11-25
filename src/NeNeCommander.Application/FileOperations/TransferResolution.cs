using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Holds the immutable, operation-scoped conflict choices already made.</summary>
public sealed record TransferResolution
{
    private readonly ImmutableList<TransferConflictChoice> _choices;

    private TransferResolution(ImmutableList<TransferConflictChoice> choices)
    {
        _choices = choices;
    }

    /// <summary>Gets the initial resolution with no user choices.</summary>
    public static TransferResolution None { get; } = new([]);
    /// <summary>Gets the ordered operation-scoped choices.</summary>
    public IReadOnlyList<TransferConflictChoice> Choices => _choices;

    /// <summary>Adds the explicit decision for the selected conflict scope.</summary>
    public TransferResolution Add(
        ConflictSet conflicts,
        TransferConflictDecision decision,
        TransferConflictScope scope)
    {
        ArgumentNullException.ThrowIfNull(conflicts);
        ArgumentNullException.ThrowIfNull(decision);
        ArgumentNullException.ThrowIfNull(scope);
        IEnumerable<TransferConflict> selected = scope == TransferConflictScope.All
            ? conflicts.Conflicts
            : conflicts.Conflicts.Take(1);
        return new TransferResolution(_choices.AddRange(
            selected.Select(conflict => TransferConflictChoice.Create(conflict, decision))));
    }

    /// <summary>Finds the latest choice for a source, or absence when it is unresolved.</summary>
    public TransferConflictChoice? Find(FileSystemPath source)
    {
        return _choices.LastOrDefault(choice =>
            FileSystemPathIdentityComparer.Instance.Equals(choice.Source, source));
    }
}
