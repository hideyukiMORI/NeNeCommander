using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Owns one local transfer batch's ordered name reservations, choices and conflicts before effects.</summary>
internal sealed class WindowsLocalTransferPreflight
{
    private readonly WindowsLocalPath _destination;
    private readonly WindowsDriveBoundary _driveBoundary;
    private readonly HashSet<string> _reservations = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<TransferPlanEntry> _plan = [];
    private readonly List<TransferConflict> _conflicts = [];

    private WindowsLocalTransferPreflight(WindowsLocalPath destination, WindowsDriveBoundary driveBoundary)
    {
        _destination = destination;
        _driveBoundary = driveBoundary;
    }

    internal static TransferPreflightOutcome Inspect(
        IReadOnlyList<FileEntrySnapshot> sources,
        FileSystemPath destination,
        WindowsDriveBoundary driveBoundary)
    {
        if (destination is not WindowsLocalPath localDestination || !driveBoundary.AdmitsLocal(localDestination))
        {
            return TransferPreflightOutcome.Rejected(FileOperationFailureKind.ProviderUnavailable);
        }
        DirectoryInfo destinationDirectory = new(localDestination.CanonicalText);
        return destinationDirectory switch
        {
            _ when !destinationDirectory.Exists => TransferPreflightOutcome.Rejected(FileOperationFailureKind.NotFound),
            _ when (destinationDirectory.Attributes & FileAttributes.ReparsePoint) != 0 =>
                TransferPreflightOutcome.Rejected(FileOperationFailureKind.ProviderUnavailable),
            _ => new WindowsLocalTransferPreflight(localDestination, driveBoundary).Plan(sources),
        };
    }

    private TransferPreflightOutcome Plan(IReadOnlyList<FileEntrySnapshot> sources)
    {
        foreach (FileEntrySnapshot source in sources)
        {
            if (AdmitSource(source) is FileOperationFailureKind failure)
            {
                return TransferPreflightOutcome.Rejected(failure);
            }
        }
        return _conflicts.Count > 0
            ? TransferPreflightOutcome.Conflicted(_conflicts)
            : TransferPreflightOutcome.Succeeded(_plan);
    }

    private FileOperationFailureKind? AdmitSource(FileEntrySnapshot source)
    {
        RevalidationOutcome revalidation = WindowsLocalFileOperationAdapter.RevalidateTransferSource(source, _driveBoundary);
        if (revalidation is EntryRejected rejected)
        {
            return rejected.Failure;
        }
        FileSystemInfo entry = ((EntryMatched)revalidation).Entry;
        if (ProviderPathContainment.Evaluate(source.Path, _destination) is ContainedPath)
        {
            return FileOperationFailureKind.Conflict;
        }
        FileSystemPath? ordinaryTarget = DirectChild(_destination, entry.Name);
        return ordinaryTarget is null
            ? FileOperationFailureKind.ProviderUnavailable
            : PlanTarget(source, entry, ordinaryTarget);
    }

    private FileOperationFailureKind? PlanTarget(FileEntrySnapshot source, FileSystemInfo entry, FileSystemPath ordinaryTarget)
    {
        TransferConflictChoice? choice = source.ConflictChoice;
        bool ordinaryUnavailable = WindowsLocalTransferDestination.Exists(ordinaryTarget.CanonicalText) ||
            _reservations.Contains(ordinaryTarget.CanonicalText);
        if (!ordinaryUnavailable && choice is null)
        {
            _ = _reservations.Add(ordinaryTarget.CanonicalText);
            _plan.Add(TransferPlanEntry.Transfer(source, ordinaryTarget));
            return null;
        }
        if (choice?.Decision == TransferConflictDecision.Skip)
        {
            _plan.Add(TransferPlanEntry.Skip(source, ordinaryTarget));
            return null;
        }
        TransferConflictChoice? keepBothChoice = choice is not null &&
            choice.Decision == TransferConflictDecision.KeepBoth ? choice : null;
        return ReserveCandidate(source, entry, ordinaryTarget, keepBothChoice);
    }

    private FileOperationFailureKind? ReserveCandidate(
        FileEntrySnapshot source,
        FileSystemInfo entry,
        FileSystemPath ordinaryTarget,
        TransferConflictChoice? keepBothChoice)
    {
        FileSystemPath? candidate = keepBothChoice is not null
            ? keepBothChoice.KeepBothCandidate : AllocateKeepBoth(entry);
        if (candidate is null ||
            candidate.Parent is not FileSystemPath candidateParent ||
            !FileSystemPathIdentityComparer.Instance.Equals(candidateParent, _destination) ||
            WindowsLocalTransferDestination.Exists(candidate.CanonicalText) ||
            _reservations.Contains(candidate.CanonicalText))
        {
            FileSystemPath? replacement = AllocateKeepBoth(entry);
            if (replacement is null)
            {
                return FileOperationFailureKind.ProviderUnavailable;
            }
            _ = _reservations.Add(replacement.CanonicalText);
            _conflicts.Add(TransferConflict.Create(source, ordinaryTarget, replacement));
            return null;
        }
        if (keepBothChoice is not null)
        {
            _ = _reservations.Add(candidate.CanonicalText);
            _plan.Add(TransferPlanEntry.Transfer(source, candidate));
            return null;
        }
        _ = _reservations.Add(candidate.CanonicalText);
        _conflicts.Add(TransferConflict.Create(source, ordinaryTarget, candidate));
        return null;
    }

    private FileSystemPath? AllocateKeepBoth(FileSystemInfo entry)
    {
        string extension = entry is FileInfo ? Path.GetExtension(entry.Name) : string.Empty;
        string stem = extension.Length == 0 ? entry.Name : entry.Name[..^extension.Length];
        for (BigInteger suffix = 2; ; suffix++)
        {
            string candidateName = stem + " (" + suffix.ToString(CultureInfo.InvariantCulture) + ")" + extension;
            FileSystemPath? candidate = DirectChild(_destination, candidateName);
            if (candidate is null)
            {
                return null;
            }
            if (!WindowsLocalTransferDestination.Exists(candidate.CanonicalText) && !_reservations.Contains(candidate.CanonicalText))
            {
                return candidate;
            }
        }
    }

    private static FileSystemPath? DirectChild(WindowsLocalPath destination, string name)
    {
        return name.Length > 255 ? null : (destination.Child(name) as PathParseSuccess)?.Path;
    }
}
