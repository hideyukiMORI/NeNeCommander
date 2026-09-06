using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Holds the immutable, operation-scoped conflict choices already made.</summary>
public sealed record TransferResolution
{
    private readonly ReadOnlyCollection<TransferConflictChoice> _choices;

    private TransferResolution(ReadOnlyCollection<TransferConflictChoice> choices)
    {
        _choices = choices;
    }

    /// <summary>Gets the initial resolution with no user choices.</summary>
    public static TransferResolution None { get; } = new(Array.Empty<TransferConflictChoice>().ToList().AsReadOnly());
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
        List<TransferConflictChoice> combined = [.. _choices];
        combined.AddRange(selected.Select(conflict => TransferConflictChoice.Create(conflict, decision)));
        return new TransferResolution(combined.AsReadOnly());
    }

    /// <summary>Finds the latest choice for a source, or absence when it is unresolved.</summary>
    public TransferConflictChoice? Find(FileSystemPath source)
    {
        return _choices.LastOrDefault(choice =>
            FileSystemPathIdentityComparer.Instance.Equals(choice.Source, source));
    }
}
