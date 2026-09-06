using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

internal sealed class BlockingInspectionPort : IFileOperationPort
{
    private readonly FileInspectionOutcome _inspection;
    private readonly TaskCompletionSource<FileInspectionOutcome> _pendingInspection;

    private BlockingInspectionPort(FileInspectionOutcome inspection)
    {
        _inspection = inspection;
        _pendingInspection = new TaskCompletionSource<FileInspectionOutcome>(
            TaskCreationOptions.RunContinuationsAsynchronously);
    }

    internal static BlockingInspectionPort Create(FileInspectionOutcome inspection)
    {
        ArgumentNullException.ThrowIfNull(inspection);
        return new BlockingInspectionPort(inspection);
    }

    internal void Release()
    {
        _pendingInspection.SetResult(_inspection);
    }

    public Task<FileInspectionOutcome> InspectAsync(
        FileSystemPath path,
        CancellationToken cancellationToken)
    {
        return _pendingInspection.Task;
    }

    public Task<TransferPreflightOutcome> PreflightTransferAsync(
        IReadOnlyList<FileEntrySnapshot> sources,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        List<TransferPlanEntry> plan = [];
        foreach (FileEntrySnapshot source in sources)
        {
            string name = source.Path.CanonicalText[(source.Path.CanonicalText.LastIndexOf('\\') + 1)..];
            plan.Add(TransferPlanEntry.Transfer(
                source,
                ((PathParseSuccess)destination.Child(name)).Path));
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
