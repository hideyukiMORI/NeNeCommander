using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

internal sealed class BlockingInspectionPort : IFileOperationPort
{
    private FileInspectionOutcome? _firstInspection;
    private TransferPreflightOutcome? _firstPreflight;
    private readonly FileInspectionOutcome _inspection;
    private readonly TaskCompletionSource<FileInspectionOutcome> _pendingInspection;

    private BlockingInspectionPort(
        FileInspectionOutcome inspection,
        FileInspectionOutcome? firstInspection,
        TransferPreflightOutcome? firstPreflight)
    {
        _inspection = inspection;
        _firstInspection = firstInspection;
        _firstPreflight = firstPreflight;
        _pendingInspection = new TaskCompletionSource<FileInspectionOutcome>(
            TaskCreationOptions.RunContinuationsAsynchronously);
    }

    internal static BlockingInspectionPort Create(FileInspectionOutcome inspection)
    {
        ArgumentNullException.ThrowIfNull(inspection);
        return new BlockingInspectionPort(inspection, null, null);
    }

    internal static BlockingInspectionPort CreateAfterFirst(
        FileInspectionOutcome firstInspection,
        TransferPreflightOutcome firstPreflight,
        FileInspectionOutcome blockedInspection)
    {
        ArgumentNullException.ThrowIfNull(firstInspection);
        ArgumentNullException.ThrowIfNull(firstPreflight);
        ArgumentNullException.ThrowIfNull(blockedInspection);
        return new BlockingInspectionPort(blockedInspection, firstInspection, firstPreflight);
    }

    internal void Release()
    {
        _pendingInspection.SetResult(_inspection);
    }

    public Task<FileInspectionOutcome> InspectAsync(
        FileSystemPath path,
        CancellationToken cancellationToken)
    {
        if (_firstInspection is FileInspectionOutcome firstInspection)
        {
            _firstInspection = null;
            return Task.FromResult(firstInspection);
        }
        return _pendingInspection.Task;
    }

    public Task<TransferPreflightOutcome> PreflightTransferAsync(
        IReadOnlyList<FileEntrySnapshot> sources,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        if (_firstPreflight is TransferPreflightOutcome firstPreflight)
        {
            _firstPreflight = null;
            return Task.FromResult(firstPreflight);
        }
        List<TransferPlanEntry> plan = [];
        foreach (FileEntrySnapshot source in sources)
        {
            string name = source.Path.CanonicalText[(source.Path.CanonicalText.LastIndexOf('\\') + 1)..];
            FileSystemPath target = ((PathParseSuccess)destination.Child(name)).Path;
            plan.Add(source.ConflictChoice?.Decision == TransferConflictDecision.Skip
                ? TransferPlanEntry.Skip(source, target)
                : TransferPlanEntry.Transfer(source, target));
        }
        return Task.FromResult(TransferPreflightOutcome.Succeeded(plan));
    }

    public Task<ProviderStepOutcome> CopyAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(ProviderStepOutcome.Succeeded());
    }

    public Task<AtomicMoveCapabilityOutcome> GetAtomicMoveCapabilityAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(AtomicMoveCapabilityOutcome.Unsupported);
    }

    public Task<ProviderStepOutcome> MoveAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(ProviderStepOutcome.Succeeded());
    }

    public Task<ProviderStepOutcome> VerifyCopyAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(ProviderStepOutcome.Succeeded());
    }

    public Task<ProviderStepOutcome> CreateDirectoryAsync(
        FileEntrySnapshot location,
        FileSystemPath target,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(ProviderStepOutcome.Succeeded());
    }

    public Task<ProviderStepOutcome> RenameAsync(
        FileEntrySnapshot source,
        FileSystemPath target,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(ProviderStepOutcome.Succeeded());
    }

    public Task<ProviderStepOutcome> DeleteAsync(
        FileEntrySnapshot source,
        DeletionExecutionMode mode,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(ProviderStepOutcome.Succeeded());
    }
}
