using System.Collections.Generic;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Rejects provider plans and conflicts that do not preserve the gateway's frozen request.</summary>
internal static class TransferPreflightValidation
{
    internal static bool IsValidConflictSet(
        ConflictSet conflicts,
        IReadOnlyList<FileEntrySnapshot> sources,
        FileSystemPath destination)
    {
        Dictionary<FileSystemPath, int> sourceIndexes = new(FileSystemPathIdentityComparer.Instance);
        for (int sourceIndex = 0; sourceIndex < sources.Count; sourceIndex++)
        {
            sourceIndexes.Add(sources[sourceIndex].Path, sourceIndex);
        }
        int previousSourceIndex = -1;
        foreach (TransferConflict conflict in conflicts.Conflicts)
        {
            if (!sourceIndexes.TryGetValue(conflict.Source.Path, out int sourceIndex) ||
                sourceIndex <= previousSourceIndex ||
                !MatchesSnapshot(sources[sourceIndex], conflict.Source) ||
                !IsDirectChild(conflict.ExistingTarget, destination) ||
                !IsDirectChild(conflict.KeepBothCandidate, destination) ||
                FileSystemPathIdentityComparer.Instance.Equals(
                    conflict.ExistingTarget,
                    conflict.KeepBothCandidate))
            {
                return false;
            }
            previousSourceIndex = sourceIndex;
        }
        return true;
    }

    internal static bool IsValidPlan(
        IReadOnlyList<TransferPlanEntry> plan,
        IReadOnlyList<FileEntrySnapshot> sources,
        FileSystemPath destination)
    {
        if (plan.Count != sources.Count)
        {
            return false;
        }
        for (int index = 0; index < plan.Count; index++)
        {
            TransferPlanEntry entry = plan[index];
            FileEntrySnapshot expected = sources[index];
            if (!MatchesSnapshot(expected, entry.Source) ||
                !IsDirectChild(entry.Target, destination) ||
                !MatchesDisposition(expected.ConflictChoice, entry))
            {
                return false;
            }
        }
        return true;
    }

    private static bool MatchesSnapshot(FileEntrySnapshot expected, FileEntrySnapshot actual)
    {
        return FileSystemPathIdentityComparer.Instance.Equals(expected.Path, actual.Path) &&
            expected.Identity == actual.Identity &&
            expected.DeletionCapability == actual.DeletionCapability &&
            actual.TransferTarget is null &&
            MatchesChoice(expected.ConflictChoice, actual.ConflictChoice);
    }

    private static bool MatchesChoice(
        TransferConflictChoice? expected,
        TransferConflictChoice? actual)
    {
        return expected is null
            ? actual is null
            : actual is not null &&
                FileSystemPathIdentityComparer.Instance.Equals(expected.Source, actual.Source) &&
                expected.Decision == actual.Decision &&
                FileSystemPathIdentityComparer.Instance.Equals(
                    expected.KeepBothCandidate,
                    actual.KeepBothCandidate);
    }

    private static bool MatchesDisposition(
        TransferConflictChoice? choice,
        TransferPlanEntry entry)
    {
        return choice is null
            ? entry.Disposition == TransferDisposition.Transfer
            : choice.Decision == TransferConflictDecision.Skip
                ? entry.Disposition == TransferDisposition.Skip
                : choice.Decision == TransferConflictDecision.KeepBoth &&
                    entry.Disposition == TransferDisposition.Transfer &&
                    FileSystemPathIdentityComparer.Instance.Equals(
                        choice.KeepBothCandidate,
                        entry.Target);
    }

    private static bool IsDirectChild(FileSystemPath target, FileSystemPath destination)
    {
        return target.Parent is FileSystemPath parent &&
            FileSystemPathIdentityComparer.Instance.Equals(parent, destination);
    }
}
