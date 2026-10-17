using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>
/// Implements the sole file-operation port for Windows local paths. Every mutation revalidates
/// the preflighted identity first, never follows reparse points, queries guarded recycle capability,
/// and normalizes platform failures to the canonical vocabulary.
/// An absent snapshot is rejected by <see cref="WindowsLocalEntryIdentity.Revalidate"/> before any step runs.
/// </summary>
public sealed class WindowsLocalFileOperationAdapter : IFileOperationPort
{
    private readonly WindowsLocalIoExecutionBoundary _executionBoundary;
    private readonly WindowsRecycleBin _recycleBin;
    private readonly IWslFileSystem _wslFileSystem;
    private readonly WindowsDriveBoundary _driveBoundary;

    /// <summary>Initializes an adapter with the default Windows local I/O execution boundary.</summary>
    public WindowsLocalFileOperationAdapter()
        : this(new WindowsLocalIoExecutionBoundary())
    {
    }

    /// <summary>Initializes an adapter with the composed Windows local I/O execution boundary.</summary>
    /// <param name="executionBoundary">Shared boundary for synchronous Windows filesystem work.</param>
    public WindowsLocalFileOperationAdapter(WindowsLocalIoExecutionBoundary executionBoundary)
        : this(executionBoundary, new WindowsDriveBoundary())
    {
    }

    internal WindowsLocalFileOperationAdapter(
        WindowsLocalIoExecutionBoundary executionBoundary,
        WindowsDriveBoundary driveBoundary)
        : this(executionBoundary, new WindowsRecycleBin(new WindowsRecycleShell(), driveBoundary),
            new WindowsWslFileSystem(driveBoundary), driveBoundary)
    {
    }

    internal WindowsLocalFileOperationAdapter(
        WindowsLocalIoExecutionBoundary executionBoundary,
        WindowsRecycleBin recycleBin)
        : this(executionBoundary, recycleBin, new WindowsDriveBoundary())
    {
    }

    private WindowsLocalFileOperationAdapter(
        WindowsLocalIoExecutionBoundary executionBoundary,
        WindowsRecycleBin recycleBin,
        WindowsDriveBoundary driveBoundary)
        : this(executionBoundary, recycleBin,
            new WindowsWslFileSystem(driveBoundary), driveBoundary)
    {
    }

    internal WindowsLocalFileOperationAdapter(
        WindowsLocalIoExecutionBoundary executionBoundary,
        WindowsRecycleBin recycleBin,
        IWslFileSystem wslFileSystem,
        WindowsDriveBoundary driveBoundary)
    {
        ArgumentNullException.ThrowIfNull(executionBoundary);
        ArgumentNullException.ThrowIfNull(recycleBin);
        ArgumentNullException.ThrowIfNull(wslFileSystem);
        ArgumentNullException.ThrowIfNull(driveBoundary);
        _executionBoundary = executionBoundary;
        _recycleBin = recycleBin;
        _wslFileSystem = wslFileSystem;
        _driveBoundary = driveBoundary;
    }

    internal WindowsLocalFileOperationAdapter(WindowsLocalIoExecutionBoundary executionBoundary, IWslFileSystem wslFileSystem)
        : this(executionBoundary, new WindowsRecycleBin(new WindowsRecycleShell()), wslFileSystem, new WindowsDriveBoundary())
    {
    }

    internal WindowsLocalFileOperationAdapter(
        WindowsLocalIoExecutionBoundary executionBoundary,
        IWslFileSystem wslFileSystem,
        WindowsDriveBoundary driveBoundary)
        : this(executionBoundary, new WindowsRecycleBin(new WindowsRecycleShell(), driveBoundary),
            wslFileSystem, driveBoundary)
    {
    }

    /// <inheritdoc />
    public Task<FileInspectionOutcome> InspectAsync(FileSystemPath path, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(path);
        return _executionBoundary.ExecuteShellAsync(() => Inspect(path));
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
    public Task<CopyVerificationOutcome> VerifyCopyAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        return _executionBoundary.ExecuteAsync(
            () => WslFileOperationAdapter.GuardedVerification(() => Verify(source, destination)));
    }

    /// <inheritdoc />
    public Task<ProviderStepOutcome> DeleteAsync(
        FileEntrySnapshot source,
        DeletionExecutionMode mode,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(mode);
        return mode == DeletionExecutionMode.Recycle
            ? _executionBoundary.ExecuteShellAsync(() => source.Path is WindowsLocalPath local && _driveBoundary.AdmitsLocal(local)
                ? _recycleBin.Recycle(source, cancellationToken)
                : ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable))
            : _executionBoundary.ExecuteAsync(
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

    private ProviderStepOutcome Rename(FileEntrySnapshot source, FileSystemPath target)
    {
        return target is not WindowsLocalPath localTarget || !_driveBoundary.AdmitsLocal(localTarget)
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : WithRevalidatedEntry(source, _driveBoundary, entry => RenameEntry(entry, source.Path, localTarget));
    }

    private AtomicMoveCapabilityOutcome AtomicMoveCapability(
        FileEntrySnapshot source,
        FileSystemPath destination)
    {
        if (destination is not WindowsLocalPath localDestination || !_driveBoundary.AdmitsLocal(localDestination))
        {
            return AtomicMoveCapabilityOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        try
        {
            return RevalidateLocal(source, _driveBoundary) switch
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

    private ProviderStepOutcome Move(FileEntrySnapshot source, FileSystemPath destination)
    {
        return destination is not WindowsLocalPath localDestination || !_driveBoundary.AdmitsLocal(localDestination)
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : WithRevalidatedEntry(source, _driveBoundary, entry => MoveEntry(entry, source, localDestination));
    }

    private ProviderStepOutcome MoveEntry(
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
        if (BuildTarget(source, destination, entry) is not WindowsLocalPath target)
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        string targetText = target.CanonicalText;
        if (TargetExists(targetText))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict);
        }
        if (source.Path is not WindowsLocalPath localSource || !_driveBoundary.AdmitsLocal(localSource) ||
            !_driveBoundary.AdmitsLocal(target))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
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

    private ProviderStepOutcome RenameEntry(FileSystemInfo entry, FileSystemPath source, WindowsLocalPath target)
    {
        if (!SharesParent(source, target))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.Inspection);
        }
        if (TargetExists(target.CanonicalText) && !IsSameEntryText(source, target))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict);
        }
        if (source is not WindowsLocalPath localSource || !_driveBoundary.AdmitsLocal(localSource) ||
            !_driveBoundary.AdmitsLocal(target))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
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

    private ProviderStepOutcome CreateDirectory(FileEntrySnapshot location, FileSystemPath target)
    {
        return target is not WindowsLocalPath localTarget || !_driveBoundary.AdmitsLocal(localTarget)
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : WithRevalidatedEntry(location, _driveBoundary, entry => CreateDirectoryBeneath(entry, location.Path, localTarget));
    }

    private ProviderStepOutcome CreateDirectoryBeneath(FileSystemInfo entry, FileSystemPath location, WindowsLocalPath target)
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
        if (location is not WindowsLocalPath localLocation || !_driveBoundary.AdmitsLocal(localLocation) ||
            !_driveBoundary.AdmitsLocal(target))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        _ = Directory.CreateDirectory(target.CanonicalText);
        return ProviderStepOutcome.Succeeded();
    }

    private FileInspectionOutcome Inspect(FileSystemPath path)
    {
        if (path is not WindowsLocalPath local || !_driveBoundary.AdmitsLocal(local))
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
                    _recycleBin.GetCapability(local)));
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

    private TransferPreflightOutcome Preflight(
        IReadOnlyList<FileEntrySnapshot> sources,
        FileSystemPath destination)
    {
        if (destination is not WindowsLocalPath localDestination || !_driveBoundary.AdmitsLocal(localDestination))
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
            RevalidationOutcome revalidation = RevalidateTransferSource(source, _driveBoundary);
            if (revalidation is EntryRejected rejected)
            {
                return TransferPreflightOutcome.Rejected(rejected.Failure);
            }
            FileSystemInfo entry = ((EntryMatched)revalidation).Entry;
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

    /// <summary>
    /// Revalidates one transfer source and rejects it when the entry or any entry beneath it is a
    /// reparse point. Every transfer preflight that reads a Windows local source uses this check,
    /// whatever the destination provider (ADR-0059).
    /// </summary>
    internal static RevalidationOutcome RevalidateTransferSource(FileEntrySnapshot source, WindowsDriveBoundary driveBoundary)
    {
        RevalidationOutcome revalidation = RevalidateLocal(source, driveBoundary);
        return revalidation is EntryMatched matched && WindowsLocalTreeCopy.ContainsReparsePoint(
            matched.Entry, entry => WindowsLocalTreeCopy.InspectLocalSource(entry, driveBoundary))
            ? new EntryRejected(FileOperationFailureKind.ProviderUnavailable)
            : revalidation;
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

    private ProviderStepOutcome Copy(FileEntrySnapshot source, FileSystemPath destination)
    {
        return destination is not WindowsLocalPath localDestination || !_driveBoundary.AdmitsLocal(localDestination)
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : WithRevalidatedEntry(source, _driveBoundary, entry =>
                BuildTarget(source, localDestination, entry) is WindowsLocalPath target
                    ? CopyEntry(entry, target)
                    : ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable));
    }

    private ProviderStepOutcome CopyEntry(FileSystemInfo entry, WindowsLocalPath target)
    {
        if (!_driveBoundary.AdmitsLocal(target))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        string targetText = target.CanonicalText;
        if (TargetExists(targetText))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict);
        }
        if (WindowsLocalTreeCopy.ContainsReparsePoint(entry,
            source => WindowsLocalTreeCopy.InspectLocalSource(source, _driveBoundary)))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        try
        {
            return WindowsLocalTreeCopy.Copy(
                entry, target, new WindowsTreeCopyAccess(
                    path => path.CanonicalText,
                    source => WindowsLocalTreeCopy.InspectLocalSource(source, _driveBoundary),
                    _driveBoundary));
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

    private CopyVerificationOutcome Verify(FileEntrySnapshot source, FileSystemPath destination)
    {
        if (destination is not WindowsLocalPath local || !_driveBoundary.AdmitsLocal(local) ||
            WindowsLocalTransferDestination.Failure(local, _driveBoundary) is not null)
        {
            return CopyVerificationOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        RevalidationOutcome revalidation = RevalidateTransferSource(source, _driveBoundary);
        if (revalidation is EntryRejected rejected)
        {
            return CopyVerificationOutcome.Failed(rejected.Failure);
        }
        FileSystemInfo entry = ((EntryMatched)revalidation).Entry;
        return BuildTarget(source, local, entry) is WindowsLocalPath target && TargetExists(target.CanonicalText)
            ? _wslFileSystem.VerifyCopy(source, target)
            : CopyVerificationOutcome.Failed(FileOperationFailureKind.Verification);
    }

    private ProviderStepOutcome Delete(FileEntrySnapshot source, DeletionExecutionMode mode)
    {
        return mode is DeletionExecutionMode.VerifiedCopyMode verified
            ? source.Path is WindowsLocalPath
                ? _wslFileSystem.DeleteVerifiedSource(source, verified.Receipt)
                : ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : mode != DeletionExecutionMode.Permanent
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : WithRevalidatedEntry(source, _driveBoundary, entry => DeleteEntry(entry, (WindowsLocalPath)source.Path));
    }

    private ProviderStepOutcome DeleteEntry(FileSystemInfo entry, WindowsLocalPath source)
    {
        if (!_driveBoundary.AdmitsLocal(source))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
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

    internal static ProviderStepOutcome WithRevalidatedEntry(
        FileEntrySnapshot source,
        WindowsDriveBoundary driveBoundary,
        Func<FileSystemInfo, ProviderStepOutcome> step)
    {
        return RevalidateLocal(source, driveBoundary) switch
        {
            EntryRejected rejected => ProviderStepOutcome.Failed(rejected.Failure),
            EntryMatched matched => step(matched.Entry),
            _ => throw new InvalidOperationException("The revalidation outcome variant is not executable."),
        };
    }

    internal static RevalidationOutcome RevalidateLocal(FileEntrySnapshot source, WindowsDriveBoundary driveBoundary)
    {
        return source.Path is not WindowsLocalPath local || !driveBoundary.AdmitsLocal(local)
            ? new EntryRejected(FileOperationFailureKind.ProviderUnavailable)
            : WindowsLocalEntryIdentity.Revalidate(source);
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

    private static WindowsLocalPath? BuildTarget(
        FileEntrySnapshot source,
        WindowsLocalPath destination,
        FileSystemInfo entry)
    {
        return source.TransferTarget as WindowsLocalPath ??
            WindowsLocalTreeCopy.DeriveChild(destination, entry.Name) as WindowsLocalPath;
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
