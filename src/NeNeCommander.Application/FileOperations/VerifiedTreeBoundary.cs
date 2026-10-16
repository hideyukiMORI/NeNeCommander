using System;
using System.Collections.Generic;
using System.Linq;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Retains the original provider-owned ancestor observations around a verified transfer.</summary>
public sealed record VerifiedTreeBoundary
{
    private VerifiedTreeBoundary(IReadOnlyList<VerifiedEntrySnapshot> source, IReadOnlyList<VerifiedEntrySnapshot> target)
    {
        SourceAncestors = new List<VerifiedEntrySnapshot>(source).AsReadOnly();
        TargetAncestors = new List<VerifiedEntrySnapshot>(target).AsReadOnly();
    }

    /// <summary>Gets the original source ancestors, nearest parent first.</summary>
    public IReadOnlyList<VerifiedEntrySnapshot> SourceAncestors { get; }
    /// <summary>Gets the original target ancestors, nearest parent first.</summary>
    public IReadOnlyList<VerifiedEntrySnapshot> TargetAncestors { get; }

    /// <summary>Creates an owned immutable boundary observation.</summary>
    public static VerifiedTreeBoundary Create(IReadOnlyList<VerifiedEntrySnapshot> source, IReadOnlyList<VerifiedEntrySnapshot> target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        return source.Any(entry => entry is null) || target.Any(entry => entry is null)
            ? throw new ArgumentException("A verified boundary cannot contain an absent entry.")
            : new VerifiedTreeBoundary(source, target);
    }
}
