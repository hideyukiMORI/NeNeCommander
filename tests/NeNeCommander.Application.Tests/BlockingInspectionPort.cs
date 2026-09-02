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

    public Task<ProviderStepOutcome> PreflightMoveAsync(
        IReadOnlyList<FileEntrySnapshot> sources,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(ProviderStepOutcome.Succeeded());
    }

    public Task<ProviderStepOutcome> CopyAsync(
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

    public Task<ProviderStepOutcome> DeleteAsync(
        FileEntrySnapshot source,
        DeletionExecutionMode mode,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(ProviderStepOutcome.Succeeded());
    }
}
