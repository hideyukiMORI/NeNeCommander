using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>
/// Implements the sole file-operation port for Windows local paths. Every mutation revalidates
/// the preflighted identity first, never follows reparse points, reports only permanent deletion
/// because no recycle mechanism exists yet, and normalizes platform failures to the canonical vocabulary.
/// An absent snapshot is rejected by <see cref="WindowsLocalEntryIdentity.Revalidate"/> before any step runs.
/// </summary>
public sealed class WindowsLocalFileOperationAdapter : IFileOperationPort
{
    private readonly WindowsLocalIoExecutionBoundary _executionBoundary;

    /// <summary>Initializes an adapter with the default Windows local I/O execution boundary.</summary>
    public WindowsLocalFileOperationAdapter()
        : this(new WindowsLocalIoExecutionBoundary())
    {
    }

    /// <summary>Initializes an adapter with the composed Windows local I/O execution boundary.</summary>
    /// <param name="executionBoundary">Shared boundary for synchronous Windows filesystem work.</param>
    public WindowsLocalFileOperationAdapter(WindowsLocalIoExecutionBoundary executionBoundary)
    {
        ArgumentNullException.ThrowIfNull(executionBoundary);
        _executionBoundary = executionBoundary;
    }

    /// <inheritdoc />
    public Task<FileInspectionOutcome> InspectAsync(FileSystemPath path, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(path);
        return _executionBoundary.ExecuteAsync(() => Inspect(path));
    }

    /// <inheritdoc />
    public Task<TransferPreflightOutcome> PreflightTransferAsync(
        IReadOnlyList<FileEntrySnapshot> sources,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(destination);
        return _executionBoundary.ExecuteAsync(
            () => GuardedPreflight(() => Preflight(sources, destination)));
    }

    /// <inheritdoc />
    public Task<ProviderStepOutcome> CopyAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        return _executionBoundary.ExecuteAsync(
            () => Guarded(() => Copy(source, destination), FileOperationFailureKind.Copy));
    }

    /// <inheritdoc />
    public Task<AtomicMoveCapabilityOutcome> GetAtomicMoveCapabilityAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        return _executionBoundary.ExecuteAsync(() => AtomicMoveCapability(source, destination));
    }

    /// <inheritdoc />
    public Task<ProviderStepOutcome> MoveAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        return _executionBoundary.ExecuteAsync(
            () => Guarded(() => Move(source, destination), FileOperationFailureKind.Move));
    }

    /// <inheritdoc />
    public Task<ProviderStepOutcome> VerifyCopyAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        return _executionBoundary.ExecuteAsync(
            () => Guarded(() => Verify(source, destination), FileOperationFailureKind.Verification));
    }

    /// <inheritdoc />
    public Task<ProviderStepOutcome> DeleteAsync(
        FileEntrySnapshot source,
        DeletionExecutionMode mode,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(mode);
        return _executionBoundary.ExecuteAsync(
            () => Guarded(() => Delete(source, mode), FileOperationFailureKind.Delete));
    }

    /// <inheritdoc />
    public Task<ProviderStepOutcome> CreateDirectoryAsync(
        FileEntrySnapshot location,
        FileSystemPath target,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(target);
        return _executionBoundary.ExecuteAsync(
            () => Guarded(
                () => CreateDirectory(location, target),
                FileOperationFailureKind.ProviderUnavailable));
    }

    /// <inheritdoc />
    public Task<ProviderStepOutcome> RenameAsync(
        FileEntrySnapshot source,
        FileSystemPath target,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        return _executionBoundary.ExecuteAsync(
            () => Guarded(() => Rename(source, target), FileOperationFailureKind.ProviderUnavailable));
    }

    private static ProviderStepOutcome Rename(FileEntrySnapshot source, FileSystemPath target)
    {
        return target is not WindowsLocalPath localTarget
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : WithRevalidatedEntry(source, entry => RenameEntry(entry, source.Path, localTarget));
    }

    private static AtomicMoveCapabilityOutcome AtomicMoveCapability(
        FileEntrySnapshot source,
        FileSystemPath destination)
    {
        if (destination is not WindowsLocalPath localDestination)
        {
            return AtomicMoveCapabilityOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        try
        {
            return WindowsLocalEntryIdentity.Revalidate(source) switch
            {
                EntryRejected rejected => AtomicMoveCapabilityOutcome.Failed(rejected.Failure),
                EntryMatched matched => SupportsAtomicMove(matched.Entry, localDestination),
                _ => throw new InvalidOperationException("The revalidation outcome variant is not executable."),
            };
        }
        catch (UnauthorizedAccessException exception)
        {
            return AtomicMoveCapabilityOutcome.Failed(Normalize(exception.HResult, FileOperationFailureKind.Inspection));
        }
        catch (IOException exception)
        {
            return AtomicMoveCapabilityOutcome.Failed(Normalize(exception.HResult, FileOperationFailureKind.Inspection));
        }
    }

    private static AtomicMoveCapabilityOutcome SupportsAtomicMove(
        FileSystemInfo entry,
        WindowsLocalPath destination)
    {
        DirectoryInfo destinationDirectory = new(destination.CanonicalText);
        return !destinationDirectory.Exists
            ? AtomicMoveCapabilityOutcome.Failed(FileOperationFailureKind.NotFound)
            : (entry.Attributes & FileAttributes.ReparsePoint) != 0 ||
            (destinationDirectory.Attributes & FileAttributes.ReparsePoint) != 0
            ? AtomicMoveCapabilityOutcome.Unsupported
            : WindowsLocalVolumeIdentity.SharesVolume(entry.FullName, destinationDirectory.FullName)
            ? AtomicMoveCapabilityOutcome.Supported
            : AtomicMoveCapabilityOutcome.Unsupported;
    }

    private static ProviderStepOutcome Move(FileEntrySnapshot source, FileSystemPath destination)
    {
        return destination is not WindowsLocalPath localDestination
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : WithRevalidatedEntry(source, entry => MoveEntry(entry, source, localDestination));
    }

    private static ProviderStepOutcome MoveEntry(
        FileSystemInfo entry,
        FileEntrySnapshot source,
        WindowsLocalPath destination)
    {
        DirectoryInfo destinationDirectory = new(destination.CanonicalText);
        if (!destinationDirectory.Exists)
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.NotFound);
        }
        if ((entry.Attributes & FileAttributes.ReparsePoint) != 0 ||
            (destinationDirectory.Attributes & FileAttributes.ReparsePoint) != 0 ||
            !WindowsLocalVolumeIdentity.SharesVolume(entry.FullName, destinationDirectory.FullName))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        if (ProviderPathContainment.Evaluate(source.Path, destination) is ContainedPath)
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict);
        }
        string targetText = BuildTargetText(source, destination, entry);
        if (TargetExists(targetText))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict);
        }
        if (entry is DirectoryInfo)
        {
            Directory.Move(entry.FullName, targetText);
        }
        else
        {
            File.Move(entry.FullName, targetText);
        }
        return ProviderStepOutcome.Succeeded();
    }

    private static ProviderStepOutcome RenameEntry(FileSystemInfo entry, FileSystemPath source, WindowsLocalPath target)
    {
        if (!SharesParent(source, target))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.Inspection);
        }
        if (TargetExists(target.CanonicalText) && !IsSameEntryText(source, target))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict);
        }
        if (entry is DirectoryInfo)
        {
            Directory.Move(entry.FullName, target.CanonicalText);
            return ProviderStepOutcome.Succeeded();
        }
        File.Move(entry.FullName, target.CanonicalText);
        return ProviderStepOutcome.Succeeded();
    }

    /// <summary>
    /// Confirms the target is a direct child of the source's own parent, so a rename can never
    /// leave that parent. Both parents are derived by the domain path model and compared with the
    /// provider-aware filesystem identity comparer; no string prefix comparison is used.
    /// </summary>
    private static bool SharesParent(FileSystemPath source, WindowsLocalPath target)
    {
        return source.Parent is FileSystemPath sourceParent &&
            target.Parent is FileSystemPath targetParent &&
            FileSystemPathIdentityComparer.Instance.Equals(sourceParent, targetParent);
    }

    /// <summary>
    /// Reports whether the existing target is the source itself, which is how a rename that only
    /// changes letter case reaches the provider. Windows local text is case-insensitive.
    /// </summary>
    private static bool IsSameEntryText(FileSystemPath source, WindowsLocalPath target)
    {
        return target.CanonicalText.Equals(source.CanonicalText, StringComparison.OrdinalIgnoreCase);
    }

    private static ProviderStepOutcome CreateDirectory(FileEntrySnapshot location, FileSystemPath target)
    {
        return target is not WindowsLocalPath localTarget
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : WithRevalidatedEntry(location, entry => CreateDirectoryBeneath(entry, location.Path, localTarget));
    }

    private static ProviderStepOutcome CreateDirectoryBeneath(FileSystemInfo entry, FileSystemPath location, WindowsLocalPath target)
    {
        if (entry is not DirectoryInfo)
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.NotFound);
        }
        if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        if (ProviderPathContainment.Evaluate(location, target) is not ContainedPath)
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.Inspection);
        }
        if (TargetExists(target.CanonicalText))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict);
        }
        _ = Directory.CreateDirectory(target.CanonicalText);
        return ProviderStepOutcome.Succeeded();
    }

    private static FileInspectionOutcome Inspect(FileSystemPath path)
    {
        if (path is not WindowsLocalPath local)
        {
            return FileInspectionOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        try
        {
            FileSystemInfo? entry = WindowsLocalEntryIdentity.Find(local);
            return entry is null
                ? FileInspectionOutcome.Failed(FileOperationFailureKind.NotFound)
                : FileInspectionOutcome.Succeeded(FileEntrySnapshot.Create(
                    path,
                    WindowsLocalEntryIdentity.Describe(entry),
                    DeletionCapability.PermanentOnly));
        }
        catch (UnauthorizedAccessException exception)
        {
            return FileInspectionOutcome.Failed(Normalize(exception.HResult, FileOperationFailureKind.Inspection));
        }
        catch (IOException exception)
        {
            return FileInspectionOutcome.Failed(Normalize(exception.HResult, FileOperationFailureKind.Inspection));
        }
    }

    private static TransferPreflightOutcome Preflight(
        IReadOnlyList<FileEntrySnapshot> sources,
        FileSystemPath destination)
    {
        if (destination is not WindowsLocalPath localDestination)
        {
            return TransferPreflightOutcome.Rejected(FileOperationFailureKind.ProviderUnavailable);
        }
        DirectoryInfo destinationDirectory = new(localDestination.CanonicalText);
        if (!destinationDirectory.Exists)
        {
            return TransferPreflightOutcome.Rejected(FileOperationFailureKind.NotFound);
        }
        if ((destinationDirectory.Attributes & FileAttributes.ReparsePoint) != 0)
        {
            return TransferPreflightOutcome.Rejected(FileOperationFailureKind.ProviderUnavailable);
        }

        HashSet<string> reservations = new(StringComparer.OrdinalIgnoreCase);
        List<TransferPlanEntry> plan = [];
        List<TransferConflict> conflicts = [];
        foreach (FileEntrySnapshot source in sources)
        {
            RevalidationOutcome revalidation = WindowsLocalEntryIdentity.Revalidate(source);
            if (revalidation is EntryRejected rejected)
            {
                return TransferPreflightOutcome.Rejected(rejected.Failure);
            }
            FileSystemInfo entry = ((EntryMatched)revalidation).Entry;
            if ((entry.Attributes & FileAttributes.ReparsePoint) != 0 ||
                WindowsLocalTreeCopy.ContainsReparsePoint(entry))
            {
                return TransferPreflightOutcome.Rejected(FileOperationFailureKind.ProviderUnavailable);
            }
            if (ProviderPathContainment.Evaluate(source.Path, destination) is ContainedPath)
            {
                return TransferPreflightOutcome.Rejected(FileOperationFailureKind.Conflict);
            }

            FileSystemPath? ordinaryTarget = DirectChild(localDestination, entry.Name);
            if (ordinaryTarget is null)
            {
                return TransferPreflightOutcome.Rejected(FileOperationFailureKind.ProviderUnavailable);
            }
            TransferConflictChoice? choice = source.ConflictChoice;
            bool ordinaryUnavailable = TargetExists(ordinaryTarget.CanonicalText) ||
                reservations.Contains(ordinaryTarget.CanonicalText);
            if (!ordinaryUnavailable && choice is null)
            {
                _ = reservations.Add(ordinaryTarget.CanonicalText);
                plan.Add(TransferPlanEntry.Transfer(source, ordinaryTarget));
                continue;
            }
            if (choice?.Decision == TransferConflictDecision.Skip)
            {
                plan.Add(TransferPlanEntry.Skip(source, ordinaryTarget));
                continue;
            }

            TransferConflictChoice? keepBothChoice = choice is not null &&
                choice.Decision == TransferConflictDecision.KeepBoth
                ? choice
                : null;
            FileSystemPath? candidate = keepBothChoice is not null
                ? keepBothChoice.KeepBothCandidate
                : AllocateKeepBoth(localDestination, entry, reservations);
            if (candidate is null ||
                candidate.Parent is not FileSystemPath candidateParent ||
                !FileSystemPathIdentityComparer.Instance.Equals(candidateParent, destination) ||
                TargetExists(candidate.CanonicalText) ||
                reservations.Contains(candidate.CanonicalText))
            {
                FileSystemPath? replacement = AllocateKeepBoth(localDestination, entry, reservations);
                if (replacement is null)
                {
                    return TransferPreflightOutcome.Rejected(FileOperationFailureKind.ProviderUnavailable);
                }
                _ = reservations.Add(replacement.CanonicalText);
                conflicts.Add(TransferConflict.Create(source, ordinaryTarget, replacement));
                continue;
            }
            if (keepBothChoice is not null)
            {
                _ = reservations.Add(candidate.CanonicalText);
                plan.Add(TransferPlanEntry.Transfer(source, candidate));
                continue;
            }
            _ = reservations.Add(candidate.CanonicalText);
            conflicts.Add(TransferConflict.Create(source, ordinaryTarget, candidate));
        }
        return conflicts.Count > 0
            ? TransferPreflightOutcome.Conflicted(conflicts)
            : TransferPreflightOutcome.Succeeded(plan);
    }

    private static FileSystemPath? AllocateKeepBoth(
        WindowsLocalPath destination,
        FileSystemInfo entry,
        HashSet<string> reservations)
    {
        string extension = entry is FileInfo ? Path.GetExtension(entry.Name) : string.Empty;
        string stem = extension.Length == 0 ? entry.Name : entry.Name[..^extension.Length];
        for (BigInteger suffix = 2; ; suffix++)
        {
            string candidateName = stem + " (" + suffix.ToString(CultureInfo.InvariantCulture) + ")" + extension;
            FileSystemPath? candidate = DirectChild(destination, candidateName);
            if (candidate is null)
            {
                return null;
            }
            if (!TargetExists(candidate.CanonicalText) && !reservations.Contains(candidate.CanonicalText))
            {
                return candidate;
            }
        }
    }

    private static FileSystemPath? DirectChild(WindowsLocalPath destination, string name)
    {
        return name.Length > 255 ? null : (destination.Child(name) as PathParseSuccess)?.Path;
    }

    private static ProviderStepOutcome Copy(FileEntrySnapshot source, FileSystemPath destination)
    {
        return destination is not WindowsLocalPath localDestination
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : WithRevalidatedEntry(source, entry => CopyEntry(entry, BuildTargetText(source, localDestination, entry)));
    }

    private static ProviderStepOutcome CopyEntry(FileSystemInfo entry, string targetText)
    {
        if (TargetExists(targetText))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict);
        }
        if (WindowsLocalTreeCopy.ContainsReparsePoint(entry))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        try
        {
            WindowsLocalTreeCopy.Copy(entry, targetText);
            return ProviderStepOutcome.Succeeded();
        }
        catch (UnauthorizedAccessException exception)
        {
            return FailedCopy(targetText, Normalize(exception.HResult, FileOperationFailureKind.Copy));
        }
        catch (IOException exception)
        {
            return FailedCopy(targetText, Normalize(exception.HResult, FileOperationFailureKind.Copy));
        }
    }

    private static ProviderStepOutcome FailedCopy(string targetText, FileOperationFailureKind failure)
    {
        return TargetExists(targetText)
            ? ProviderStepOutcome.FailedAfterEffect(failure, ProviderStepEffectKind.CopyTargetCreated)
            : ProviderStepOutcome.Failed(failure);
    }

    private static ProviderStepOutcome Verify(FileEntrySnapshot source, FileSystemPath destination)
    {
        return destination is not WindowsLocalPath localDestination
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : WithRevalidatedEntry(source, entry =>
                WindowsLocalTreeCopy.Matches(entry, BuildTargetText(source, localDestination, entry))
                    ? ProviderStepOutcome.Succeeded()
                    : ProviderStepOutcome.Failed(FileOperationFailureKind.Verification));
    }

    private static ProviderStepOutcome Delete(FileEntrySnapshot source, DeletionExecutionMode mode)
    {
        return mode != DeletionExecutionMode.Permanent
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : WithRevalidatedEntry(source, DeleteEntry);
    }

    private static ProviderStepOutcome DeleteEntry(FileSystemInfo entry)
    {
        if (entry is DirectoryInfo directory)
        {
            directory.Delete(recursive: true);
        }
        else
        {
            entry.Delete();
        }
        return ProviderStepOutcome.Succeeded();
    }

    private static ProviderStepOutcome WithRevalidatedEntry(
        FileEntrySnapshot source,
        Func<FileSystemInfo, ProviderStepOutcome> step)
    {
        return WindowsLocalEntryIdentity.Revalidate(source) switch
        {
            EntryRejected rejected => ProviderStepOutcome.Failed(rejected.Failure),
            EntryMatched matched => step(matched.Entry),
            _ => throw new InvalidOperationException("The revalidation outcome variant is not executable."),
        };
    }

    private static ProviderStepOutcome Guarded(Func<ProviderStepOutcome> step, FileOperationFailureKind fallback)
    {
        try
        {
            return step();
        }
        catch (UnauthorizedAccessException exception)
        {
            return ProviderStepOutcome.Failed(Normalize(exception.HResult, fallback));
        }
        catch (IOException exception)
        {
            return ProviderStepOutcome.Failed(Normalize(exception.HResult, fallback));
        }
    }

    internal static FileOperationFailureKind Normalize(int hResult, FileOperationFailureKind fallback)
    {
        FileOperationFailureKind normalized = WindowsFileFailureNormalizer.Normalize(hResult);
        return normalized == FileOperationFailureKind.ProviderUnavailable ? fallback : normalized;
    }

    private static string BuildTargetText(
        FileEntrySnapshot source,
        WindowsLocalPath destination,
        FileSystemInfo entry)
    {
        return source.TransferTarget?.CanonicalText ??
            WindowsLocalTreeCopy.ResolveDirectChild(destination.CanonicalText, entry.Name);
    }

    private static TransferPreflightOutcome GuardedPreflight(Func<TransferPreflightOutcome> step)
    {
        try
        {
            return step();
        }
        catch (UnauthorizedAccessException exception)
        {
            return TransferPreflightOutcome.Rejected(
                Normalize(exception.HResult, FileOperationFailureKind.Inspection));
        }
        catch (IOException exception)
        {
            return TransferPreflightOutcome.Rejected(
                Normalize(exception.HResult, FileOperationFailureKind.Inspection));
        }
    }

    private static bool TargetExists(string targetText)
    {
        return File.Exists(targetText) || Directory.Exists(targetText);
    }
}
