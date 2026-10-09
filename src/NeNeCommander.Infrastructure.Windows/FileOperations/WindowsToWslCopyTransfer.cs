using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>
/// Copies Windows local sources into one WSL distribution for the router's
/// <see cref="TransferRoute.WindowsLocalToWsl"/> pair (ADR-0059). The source side reuses the Windows
/// local adapter's revalidation and reparse rejection, the destination side reuses the WSL adapter's
/// target derivation, collision, and non-link directory checks, and copying and verification are
/// the shared <see cref="WindowsLocalTreeCopy"/>. A move across the pair and every member that is not
/// a transfer fail closed.
/// </summary>
internal sealed class WindowsToWslCopyTransfer : IFileOperationPort
{
    private readonly WindowsLocalIoExecutionBoundary _executionBoundary;
    private readonly IWslFileSystem _destination;

    internal WindowsToWslCopyTransfer(
        WindowsLocalIoExecutionBoundary executionBoundary,
        IWslFileSystem destination)
    {
        ArgumentNullException.ThrowIfNull(executionBoundary);
        ArgumentNullException.ThrowIfNull(destination);
        _executionBoundary = executionBoundary;
        _destination = destination;
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
        return _executionBoundary.ExecuteAsync(
            () => WslFileOperationAdapter.GuardedPreflight(
                () => Preflight(sources, destination),
                FileOperationFailureKind.ProviderUnavailable));
    }

    /// <summary>
    /// Fails closed because this decision copies only. The port receives no request kind at
    /// preflight, and the gateway asks this question only for a move and before its first step, so
    /// this answer is where a move across the pair stops with zero effects.
    /// </summary>
    public Task<AtomicMoveCapabilityOutcome> GetAtomicMoveCapabilityAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        return Task.FromResult(AtomicMoveCapabilityOutcome.Failed(FileOperationFailureKind.ProviderUnavailable));
    }

    public Task<ProviderStepOutcome> MoveAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        return FailedStep();
    }

    public Task<ProviderStepOutcome> CopyAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        return _executionBoundary.ExecuteAsync(
            () => WslFileOperationAdapter.Guarded(
                () => Copy(source, destination),
                FileOperationFailureKind.Copy));
    }

    public Task<ProviderStepOutcome> VerifyCopyAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        return _executionBoundary.ExecuteAsync(
            () => WslFileOperationAdapter.Guarded(
                () => Verify(source, destination),
                FileOperationFailureKind.Verification));
    }

    public Task<ProviderStepOutcome> DeleteAsync(
        FileEntrySnapshot source,
        DeletionExecutionMode mode,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(mode);
        return FailedStep();
    }

    public Task<ProviderStepOutcome> CreateDirectoryAsync(
        FileEntrySnapshot location,
        FileSystemPath target,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(target);
        return FailedStep();
    }

    public Task<ProviderStepOutcome> RenameAsync(
        FileEntrySnapshot source,
        FileSystemPath target,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        return FailedStep();
    }

    private TransferPreflightOutcome Preflight(
        IReadOnlyList<FileEntrySnapshot> sources,
        FileSystemPath destination)
    {
        if (destination is not WslPath wslDestination)
        {
            return TransferPreflightOutcome.Rejected(FileOperationFailureKind.ProviderUnavailable);
        }
        if (WslFileOperationAdapter.TransferDestinationFailure(_destination, wslDestination)
            is FileOperationFailureKind destinationFailure)
        {
            return TransferPreflightOutcome.Rejected(destinationFailure);
        }

        HashSet<FileSystemPath> reserved = new(FileSystemPathIdentityComparer.Instance);
        List<TransferPlanEntry> plan = [];
        foreach (FileEntrySnapshot source in sources)
        {
            RevalidationOutcome revalidation = WindowsLocalFileOperationAdapter.RevalidateTransferSource(source);
            if (revalidation is EntryRejected rejected)
            {
                return TransferPreflightOutcome.Rejected(rejected.Failure);
            }
            FileSystemInfo entry = ((EntryMatched)revalidation).Entry;
            if (WslFileOperationAdapter.BuildTarget(entry.Name, wslDestination) is not WslPath target)
            {
                return TransferPreflightOutcome.Rejected(FileOperationFailureKind.ProviderUnavailable);
            }
            if (!reserved.Add(target) || _destination.TargetExists(target))
            {
                return TransferPreflightOutcome.Rejected(FileOperationFailureKind.Conflict);
            }
            plan.Add(TransferPlanEntry.Transfer(source, target));
        }
        return TransferPreflightOutcome.Succeeded(plan);
    }

    private ProviderStepOutcome Copy(FileEntrySnapshot source, FileSystemPath destination)
    {
        return destination is not WslPath wslDestination
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : WindowsLocalFileOperationAdapter.WithRevalidatedEntry(
                source,
                entry => CopyEntry(entry, wslDestination));
    }

    private ProviderStepOutcome CopyEntry(FileSystemInfo source, WslPath destination)
    {
        if (!WslFileOperationAdapter.IsUsableDestination(_destination, destination) ||
            WindowsLocalTreeCopy.ContainsReparsePoint(source) ||
            WslFileOperationAdapter.BuildTarget(source.Name, destination) is not WslPath target)
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        if (_destination.TargetExists(target))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict);
        }

        try
        {
            _destination.CopyFromWindowsLocal(source, target);
            return ProviderStepOutcome.Succeeded();
        }
        catch (UnauthorizedAccessException exception)
        {
            return WslFileOperationAdapter.FailedCopy(
                _destination,
                target,
                WslFileOperationAdapter.Normalize(exception.HResult, FileOperationFailureKind.Copy));
        }
        catch (IOException exception)
        {
            return WslFileOperationAdapter.FailedCopy(
                _destination,
                target,
                WslFileOperationAdapter.Normalize(exception.HResult, FileOperationFailureKind.Copy));
        }
    }

    private ProviderStepOutcome Verify(FileEntrySnapshot source, FileSystemPath destination)
    {
        return destination is not WslPath wslDestination
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : WindowsLocalFileOperationAdapter.WithRevalidatedEntry(source, entry =>
                WslFileOperationAdapter.IsUsableDestination(_destination, wslDestination) &&
                !WindowsLocalTreeCopy.ContainsReparsePoint(entry) &&
                WslFileOperationAdapter.BuildTarget(entry.Name, wslDestination) is WslPath target &&
                _destination.TargetExists(target) &&
                !_destination.ContainsReparsePoint(target) &&
                _destination.MatchesWindowsLocal(entry, target)
                    ? ProviderStepOutcome.Succeeded()
                    : ProviderStepOutcome.Failed(FileOperationFailureKind.Verification));
    }

    private static Task<ProviderStepOutcome> FailedStep()
    {
        return Task.FromResult(ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable));
    }
}
