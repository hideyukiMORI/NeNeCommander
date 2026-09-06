using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Executes same-distribution WSL mutations through the canonical Windows namespace.</summary>
internal sealed class WslFileOperationAdapter : IFileOperationPort
{
    private readonly WindowsLocalIoExecutionBoundary _executionBoundary;
    private readonly IWslFileSystem _fileSystem;

    internal WslFileOperationAdapter(WindowsLocalIoExecutionBoundary executionBoundary)
        : this(executionBoundary, new WindowsWslFileSystem())
    {
    }

    internal WslFileOperationAdapter(
        WindowsLocalIoExecutionBoundary executionBoundary,
        IWslFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(executionBoundary);
        ArgumentNullException.ThrowIfNull(fileSystem);
        _executionBoundary = executionBoundary;
        _fileSystem = fileSystem;
    }

    public Task<FileInspectionOutcome> InspectAsync(FileSystemPath path, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(path);
        return _executionBoundary.ExecuteAsync(() => Inspect(path));
    }

    public Task<TransferPreflightOutcome> PreflightTransferAsync(
        IReadOnlyList<FileEntrySnapshot> sources,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(destination);
        return _executionBoundary.ExecuteAsync(
            () => ConvertPreflight(
                Guarded(() => Preflight(sources, destination), FileOperationFailureKind.ProviderUnavailable),
                sources,
                destination));
    }

    public Task<AtomicMoveCapabilityOutcome> GetAtomicMoveCapabilityAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        return source.Path is WslPath sourceWsl &&
            destination is WslPath destinationWsl &&
            SharesDistribution(sourceWsl, destinationWsl)
                ? Task.FromResult(AtomicMoveCapabilityOutcome.Unsupported)
                : Task.FromResult(
                    AtomicMoveCapabilityOutcome.Failed(FileOperationFailureKind.ProviderUnavailable));
    }

    public Task<ProviderStepOutcome> MoveAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        return RejectTransfer(source, destination);
    }

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

    public Task<ProviderStepOutcome> CreateDirectoryAsync(
        FileEntrySnapshot location,
        FileSystemPath target,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(target);
        return _executionBoundary.ExecuteAsync(
            () => Guarded(() => CreateDirectory(location, target), FileOperationFailureKind.ProviderUnavailable));
    }

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

    private FileInspectionOutcome Inspect(FileSystemPath path)
    {
        if (path is not WslPath wsl)
        {
            return FileInspectionOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }

        try
        {
            WslFileSystemEntry? entry = _fileSystem.Find(wsl);
            return entry is null
                ? FileInspectionOutcome.Failed(FileOperationFailureKind.NotFound)
                : FileInspectionOutcome.Succeeded(FileEntrySnapshot.Create(
                    path,
                    entry.Identity,
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

    private ProviderStepOutcome CreateDirectory(FileEntrySnapshot location, FileSystemPath target)
    {
        return target is not WslPath wslTarget
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : WithRevalidatedEntry(location, entry =>
                entry.Kind != DirectoryEntryKind.Directory
                    ? ProviderStepOutcome.Failed(FileOperationFailureKind.NotFound)
                    : IsReparsePoint(entry) || !IsDirectChild(location.Path, wslTarget)
                        ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
                        : _fileSystem.TargetExists(wslTarget)
                            ? ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict)
                            : CreateDirectory(wslTarget));
    }

    private ProviderStepOutcome Preflight(
        IReadOnlyList<FileEntrySnapshot> sources,
        FileSystemPath destination)
    {
        if (sources.Count == 0 ||
            destination is not WslPath wslDestination ||
            !SourcesMatchDistribution(sources, wslDestination))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        WslFileSystemEntry? destinationEntry = _fileSystem.Find(wslDestination);
        return destinationEntry is null || destinationEntry.Kind != DirectoryEntryKind.Directory
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.NotFound)
            : IsReparsePoint(destinationEntry)
                ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
                : sources
                    .Select(source => WithRevalidatedEntry(
                        source,
                        entry => PreflightSource(entry, wslDestination)))
                    .FirstOrDefault(outcome => outcome.Failure is not null) ??
                    ProviderStepOutcome.Succeeded();
    }

    private TransferPreflightOutcome ConvertPreflight(
        ProviderStepOutcome outcome,
        IReadOnlyList<FileEntrySnapshot> sources,
        FileSystemPath destination)
    {
        if (outcome.Failure is FileOperationFailureKind failure)
        {
            return TransferPreflightOutcome.Rejected(failure);
        }
        WslPath wslDestination = (WslPath)destination;
        List<TransferPlanEntry> plan = [];
        foreach (FileEntrySnapshot source in sources)
        {
            WslFileSystemEntry entry = _fileSystem.Find((WslPath)source.Path) ??
                throw new InvalidOperationException("A successful preflight source must still exist.");
            WslPath target = BuildTarget(entry, wslDestination) ??
                throw new InvalidOperationException("A successful preflight target must be representable.");
            plan.Add(TransferPlanEntry.Transfer(source, target));
        }
        return TransferPreflightOutcome.Succeeded(plan);
    }

    private ProviderStepOutcome PreflightSource(WslFileSystemEntry source, WslPath destination)
    {
        return IsReparsePoint(source) ||
            _fileSystem.ContainsReparsePoint(source) ||
            BuildTarget(source, destination) is not WslPath target
                ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
                : ProviderPathContainment.Evaluate(source.Path, destination) is ContainedPath ||
                    _fileSystem.TargetExists(target)
                    ? ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict)
                    : ProviderStepOutcome.Succeeded();
    }

    private ProviderStepOutcome Copy(FileEntrySnapshot source, FileSystemPath destination)
    {
        return destination is not WslPath wslDestination
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : WithRevalidatedEntry(source, entry => Copy(entry, wslDestination));
    }

    private ProviderStepOutcome Copy(WslFileSystemEntry source, WslPath destination)
    {
        if (!SharesDistribution(source.Path, destination) ||
            !IsUsableDestination(destination) ||
            IsReparsePoint(source) ||
            _fileSystem.ContainsReparsePoint(source) ||
            BuildTarget(source, destination) is not WslPath target)
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        if (ProviderPathContainment.Evaluate(source.Path, destination) is ContainedPath)
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict);
        }
        if (_fileSystem.TargetExists(target))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict);
        }

        try
        {
            _fileSystem.Copy(source, target);
            return ProviderStepOutcome.Succeeded();
        }
        catch (UnauthorizedAccessException exception)
        {
            return FailedCopy(target, Normalize(exception.HResult, FileOperationFailureKind.Copy));
        }
        catch (IOException exception)
        {
            return FailedCopy(target, Normalize(exception.HResult, FileOperationFailureKind.Copy));
        }
    }

    private ProviderStepOutcome Verify(FileEntrySnapshot source, FileSystemPath destination)
    {
        return destination is not WslPath wslDestination
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : WithRevalidatedEntry(source, entry =>
                SharesDistribution(entry.Path, wslDestination) &&
                IsUsableDestination(wslDestination) &&
                !IsReparsePoint(entry) &&
                !_fileSystem.ContainsReparsePoint(entry) &&
                ProviderPathContainment.Evaluate(entry.Path, wslDestination) is not ContainedPath &&
                BuildTarget(entry, wslDestination) is WslPath target &&
                _fileSystem.TargetExists(target) &&
                !_fileSystem.ContainsReparsePoint(target) &&
                _fileSystem.Matches(entry, target)
                    ? ProviderStepOutcome.Succeeded()
                    : ProviderStepOutcome.Failed(FileOperationFailureKind.Verification));
    }

    private ProviderStepOutcome FailedCopy(WslPath target, FileOperationFailureKind failure)
    {
        return _fileSystem.TargetExists(target)
            ? ProviderStepOutcome.FailedAfterEffect(failure, ProviderStepEffectKind.CopyTargetCreated)
            : ProviderStepOutcome.Failed(failure);
    }

    private bool IsUsableDestination(WslPath destination)
    {
        return _fileSystem.Find(destination) is WslFileSystemEntry entry &&
            entry.Kind == DirectoryEntryKind.Directory &&
            !IsReparsePoint(entry);
    }

    private ProviderStepOutcome CreateDirectory(WslPath target)
    {
        _fileSystem.CreateDirectory(target);
        return ProviderStepOutcome.Succeeded();
    }

    private ProviderStepOutcome Rename(FileEntrySnapshot source, FileSystemPath target)
    {
        return target is not WslPath wslTarget
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : WithRevalidatedEntry(source, entry =>
                IsReparsePoint(entry) || !SharesParent(source.Path, wslTarget)
                    ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
                    : _fileSystem.TargetExists(wslTarget)
                        ? ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict)
                        : Rename(entry, wslTarget));
    }

    private ProviderStepOutcome Rename(WslFileSystemEntry source, WslPath target)
    {
        _fileSystem.Rename(source, target);
        return ProviderStepOutcome.Succeeded();
    }

    private ProviderStepOutcome Delete(FileEntrySnapshot source, DeletionExecutionMode mode)
    {
        return mode != DeletionExecutionMode.Permanent
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : WithRevalidatedEntry(source, entry =>
                IsReparsePoint(entry) || _fileSystem.ContainsReparsePoint(entry)
                    ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
                    : Delete(entry));
    }

    private ProviderStepOutcome Delete(WslFileSystemEntry source)
    {
        _fileSystem.Delete(source);
        return ProviderStepOutcome.Succeeded();
    }

    private ProviderStepOutcome WithRevalidatedEntry(
        FileEntrySnapshot source,
        Func<WslFileSystemEntry, ProviderStepOutcome> step)
    {
        if (source.Path is not WslPath wsl)
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }

        WslFileSystemEntry? current = _fileSystem.Find(wsl);
        return current is null
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.NotFound)
            : current.Identity != source.Identity
                ? ProviderStepOutcome.Failed(FileOperationFailureKind.IdentityChanged)
                : step(current);
    }

    private static bool SharesParent(FileSystemPath source, WslPath target)
    {
        return source.Parent is FileSystemPath sourceParent &&
            target.Parent is FileSystemPath targetParent &&
            FileSystemPathIdentityComparer.Instance.Equals(sourceParent, targetParent);
    }

    private static bool IsDirectChild(FileSystemPath location, WslPath target)
    {
        return target.Parent is FileSystemPath targetParent &&
            FileSystemPathIdentityComparer.Instance.Equals(location, targetParent);
    }

    private static bool IsReparsePoint(WslFileSystemEntry entry)
    {
        return (entry.Attributes & FileAttributes.ReparsePoint) != 0;
    }

    private static WslPath? BuildTarget(WslFileSystemEntry source, WslPath destination)
    {
        return (destination.Child(source.Name) as PathParseSuccess)?.Path as WslPath;
    }

    private static bool SharesDistribution(WslPath source, WslPath destination)
    {
        return source.DistributionName.Equals(
            destination.DistributionName,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool SourcesMatchDistribution(
        IReadOnlyList<FileEntrySnapshot> sources,
        WslPath destination)
    {
        return sources.All(source =>
            source.Path is WslPath wsl && SharesDistribution(wsl, destination));
    }

    private static Task<ProviderStepOutcome> RejectTransfer(
        FileEntrySnapshot source,
        FileSystemPath destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        return FailedStep();
    }

    private static Task<ProviderStepOutcome> FailedStep()
    {
        return Task.FromResult(ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable));
    }

    private static ProviderStepOutcome Guarded(
        Func<ProviderStepOutcome> step,
        FileOperationFailureKind fallback)
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

    internal static FileOperationFailureKind Normalize(
        int hResult,
        FileOperationFailureKind fallback)
    {
        return WindowsLocalFileOperationAdapter.Normalize(hResult, fallback);
    }
}
