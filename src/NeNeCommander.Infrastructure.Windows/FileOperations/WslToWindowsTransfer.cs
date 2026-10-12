using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Copies WSL sources into Windows; the gateway alone composes and deletes for a move.</summary>
internal sealed class WslToWindowsTransfer : IFileOperationPort
{
    private readonly WindowsLocalIoExecutionBoundary _executionBoundary;
    private readonly IWslFileSystem _source;

    internal WslToWindowsTransfer(WindowsLocalIoExecutionBoundary executionBoundary, IWslFileSystem source)
    {
        ArgumentNullException.ThrowIfNull(executionBoundary);
        ArgumentNullException.ThrowIfNull(source);
        _executionBoundary = executionBoundary;
        _source = source;
    }

    public Task<FileInspectionOutcome> InspectAsync(FileSystemPath path, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(path);
        return Task.FromResult(FileInspectionOutcome.Failed(FileOperationFailureKind.ProviderUnavailable));
    }

    public Task<TransferPreflightOutcome> PreflightTransferAsync(
        IReadOnlyList<FileEntrySnapshot> sources,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(destination);
        return _executionBoundary.ExecuteAsync(() => WslFileOperationAdapter.GuardedPreflight(
            () => Preflight(sources, destination), FileOperationFailureKind.ProviderUnavailable));
    }

    public Task<AtomicMoveCapabilityOutcome> GetAtomicMoveCapabilityAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        return Task.FromResult(TransferRoute.Derive(source.Path, destination) is TransferRoute.WslToWindows
            ? AtomicMoveCapabilityOutcome.Unsupported
            : AtomicMoveCapabilityOutcome.Failed(FileOperationFailureKind.ProviderUnavailable));
    }

    public Task<ProviderStepOutcome> CopyAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        return _executionBoundary.ExecuteAsync(() => WslFileOperationAdapter.Guarded(
            () => WithSource(source, destination, CopyEntry), FileOperationFailureKind.Copy));
    }

    public Task<ProviderStepOutcome> VerifyCopyAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        return _executionBoundary.ExecuteAsync(() => WslFileOperationAdapter.Guarded(
            () => WithSource(source, destination, VerifyEntry), FileOperationFailureKind.Verification));
    }

    public Task<ProviderStepOutcome> MoveAsync(
        FileEntrySnapshot source, FileSystemPath destination, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        return FailedStep();
    }

    public Task<ProviderStepOutcome> DeleteAsync(
        FileEntrySnapshot source, DeletionExecutionMode mode, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(mode);
        return FailedStep();
    }

    public Task<ProviderStepOutcome> CreateDirectoryAsync(
        FileEntrySnapshot location, FileSystemPath target, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(target);
        return FailedStep();
    }

    public Task<ProviderStepOutcome> RenameAsync(
        FileEntrySnapshot source, FileSystemPath target, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        return FailedStep();
    }

    private TransferPreflightOutcome Preflight(IReadOnlyList<FileEntrySnapshot> sources, FileSystemPath destination)
    {
        if (TransferRoute.Derive(sources, destination) is not TransferRoute.WslToWindows ||
            destination is not WindowsLocalPath local)
        {
            return TransferPreflightOutcome.Rejected(FileOperationFailureKind.ProviderUnavailable);
        }
        if (WindowsLocalTransferDestination.Failure(local) is FileOperationFailureKind failure)
        {
            return TransferPreflightOutcome.Rejected(failure);
        }
        HashSet<FileSystemPath> reserved = new(FileSystemPathIdentityComparer.Instance);
        List<TransferPlanEntry> plan = [];
        foreach (FileEntrySnapshot source in sources)
        {
            WslFileSystemEntry? entry = _source.Find((WslPath)source.Path);
            if (entry is null || entry.Identity != source.Identity)
            {
                return TransferPreflightOutcome.Rejected(entry is null
                    ? FileOperationFailureKind.NotFound : FileOperationFailureKind.IdentityChanged);
            }
            if (WindowsLocalTreeCopy.DeriveChild(local, entry.Name) is not WindowsLocalPath target)
            {
                return TransferPreflightOutcome.Rejected(FileOperationFailureKind.ProviderUnavailable);
            }
            if (!reserved.Add(target))
            {
                return TransferPreflightOutcome.Rejected(FileOperationFailureKind.Conflict);
            }
            if (_source.PreflightToWindowsLocal(entry, target) is FileOperationFailureKind rejected)
            {
                return TransferPreflightOutcome.Rejected(rejected);
            }
            plan.Add(TransferPlanEntry.Transfer(source, target));
        }
        return TransferPreflightOutcome.Succeeded(plan);
    }

    private ProviderStepOutcome WithSource(
        FileEntrySnapshot source,
        FileSystemPath destination,
        Func<WslFileSystemEntry, WindowsLocalPath, ProviderStepOutcome> step)
    {
        return destination is not WindowsLocalPath local
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : WindowsLocalTransferDestination.Failure(local) is FileOperationFailureKind failure
            ? ProviderStepOutcome.Failed(failure)
            : WslFileOperationAdapter.WithRevalidatedEntry(_source, source, entry => step(entry, local));
    }

    private ProviderStepOutcome CopyEntry(WslFileSystemEntry source, WindowsLocalPath destination)
    {
        if (WindowsLocalTreeCopy.DeriveChild(destination, source.Name) is not WindowsLocalPath target)
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        if (WindowsLocalTransferDestination.Exists(target))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict);
        }
        try
        {
            return _source.CopyToWindowsLocal(source, target);
        }
        catch (UnauthorizedAccessException exception)
        {
            return FailedCopy(target, exception.HResult);
        }
        catch (IOException exception)
        {
            return FailedCopy(target, exception.HResult);
        }
    }

    private ProviderStepOutcome VerifyEntry(WslFileSystemEntry source, WindowsLocalPath destination)
    {
        return WindowsLocalTreeCopy.DeriveChild(destination, source.Name) is WindowsLocalPath target && _source.MatchesWindowsLocalTarget(source, target)
                ? ProviderStepOutcome.Succeeded()
                : ProviderStepOutcome.Failed(FileOperationFailureKind.Verification);
    }

    private static ProviderStepOutcome FailedCopy(WindowsLocalPath target, int hResult)
    {
        FileOperationFailureKind failure = WslFileOperationAdapter.Normalize(hResult, FileOperationFailureKind.Copy);
        return WindowsLocalTransferDestination.Exists(target)
            ? ProviderStepOutcome.FailedAfterEffect(failure, ProviderStepEffectKind.CopyTargetCreated)
            : ProviderStepOutcome.Failed(failure);
    }

    private static Task<ProviderStepOutcome> FailedStep()
    {
        return Task.FromResult(ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable));
    }
}
