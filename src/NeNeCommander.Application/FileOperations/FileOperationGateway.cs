using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Owns the sole validated, serialized execution path for filesystem mutations.
/// </summary>
public sealed class FileOperationGateway : IDisposable
{
    private readonly SemaphoreSlim _executionLease;
    private readonly IFileOperationPort _port;
    private readonly object _continuationOwner;

    /// <summary>Initializes the gateway with its sole provider-neutral side-effect port.</summary>
    /// <param name="port">Provider-neutral file operation port.</param>
    public FileOperationGateway(IFileOperationPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _port = port;
        _executionLease = new SemaphoreSlim(1, 1);
        _continuationOwner = new object();
    }

    /// <summary>Executes one immutable request without permitting reentrant mutation.</summary>
    /// <param name="request">Validated operation request.</param>
    /// <param name="progress">Observer told once per source whose every step completed.</param>
    /// <param name="cancellationToken">Token that stops new work after observation.</param>
    /// <returns>The complete typed outcome and ordered completed effects.</returns>
    public async Task<FileOperationOutcome> ExecuteAsync(
        FileOperationRequest request,
        IFileOperationProgressObserver progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(progress);
        if (cancellationToken.IsCancellationRequested)
        {
            return FileOperationOutcome.Cancelled(Array.Empty<FileOperationEffect>());
        }

        bool entered = await _executionLease.WaitAsync(0, CancellationToken.None);
        if (!entered)
        {
            return FileOperationOutcome.Failed(
                Array.Empty<FileOperationEffect>(),
                FileOperationFailureKind.Reentrant);
        }

        try
        {
            return request switch
            {
                MoveRequest moveRequest => await ExecuteTransferAsync(moveRequest, progress, cancellationToken),
                CopyRequest copyRequest => await ExecuteTransferAsync(copyRequest, progress, cancellationToken),
                DeleteRequest deleteRequest => await ExecuteDeleteAsync(deleteRequest, progress, cancellationToken),
                CreateDirectoryRequest createRequest => await ExecuteCreateDirectoryAsync(createRequest, progress, cancellationToken),
                RenameRequest renameRequest => await ExecuteRenameAsync(renameRequest, progress, cancellationToken),
                _ => throw new InvalidOperationException("The validated request variant is not executable."),
            };
        }
        finally
        {
            _ = _executionLease.Release();
        }
    }

    /// <summary>Resumes one conflict-paused transfer against its original frozen identities.</summary>
    public async Task<FileOperationOutcome> ResumeAsync(
        TransferContinuation continuation,
        TransferConflictResponse response,
        IFileOperationProgressObserver progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(continuation);
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(progress);
        if (!continuation.IsOwnedBy(_continuationOwner))
        {
            return FileOperationOutcome.Failed([], FileOperationFailureKind.Reentrant);
        }
        if (response.Decision == TransferConflictDecision.Cancel || cancellationToken.IsCancellationRequested)
        {
            return continuation.TryConsume()
                ? FileOperationOutcome.Cancelled([])
                : FileOperationOutcome.Failed([], FileOperationFailureKind.Reentrant);
        }
        bool entered = await _executionLease.WaitAsync(0, CancellationToken.None);
        if (!entered)
        {
            return FileOperationOutcome.Failed([], FileOperationFailureKind.Reentrant);
        }
        try
        {
            if (!continuation.TryConsume())
            {
                return FileOperationOutcome.Failed([], FileOperationFailureKind.Reentrant);
            }
            TransferResolution resolution = continuation.Resolution.Add(
                continuation.PendingConflicts,
                response.Decision,
                response.Scope);
            TransferExecution execution = new(_port, continuation.Transfer, _continuationOwner);
            return await execution.ExecuteAsync(resolution, progress, cancellationToken);
        }
        finally
        {
            _ = _executionLease.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _executionLease.Dispose();
    }

    private async Task<FileOperationOutcome> ExecuteTransferAsync(
        FileOperationRequest request,
        IFileOperationProgressObserver progress,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<FileSystemPath> sources;
        FileSystemPath destination;
        if (request is MoveRequest move)
        {
            sources = move.Sources;
            destination = move.Destination;
        }
        else
        {
            CopyRequest copy = (CopyRequest)request;
            sources = copy.Sources;
            destination = copy.Destination;
        }
        InspectionBatch inspection = await InspectAllAsync(sources, cancellationToken);
        return inspection.Completion == InspectionBatchCompletion.Cancelled
            ? FileOperationOutcome.Cancelled([])
            : inspection.Failure is FileOperationFailureKind failedInspection
            ? FileOperationOutcome.Failed([], failedInspection)
            : await new TransferExecution(
                _port,
                FrozenTransfer.Create(request, inspection.Snapshots, destination),
                _continuationOwner).ExecuteAsync(TransferResolution.None, progress, cancellationToken);
    }

    private async Task<FileOperationOutcome> ExecuteDeleteAsync(
        DeleteRequest request,
        IFileOperationProgressObserver progress,
        CancellationToken cancellationToken)
    {
        List<FileOperationEffect> effects = [];
        InspectionBatch inspection = await InspectAllAsync(request.Sources, cancellationToken);
        return inspection switch
        {
            _ when inspection.Completion == InspectionBatchCompletion.Cancelled => FileOperationOutcome.Cancelled(effects),
            { Failure: FileOperationFailureKind failure } => FileOperationOutcome.Failed(effects, failure),
            _ when NeedsConfirmation(inspection.Snapshots) &&
                (request.Confirmation is null || !request.Confirmation.Covers(request.Sources)) =>
                FileOperationOutcome.Failed(effects, FileOperationFailureKind.ConfirmationRequired),
            _ => await DeleteInspectedAsync(request, inspection.Snapshots, progress, cancellationToken),
        };
    }

    private async Task<FileOperationOutcome> DeleteInspectedAsync(
        DeleteRequest request,
        IReadOnlyList<FileEntrySnapshot> snapshots,
        IFileOperationProgressObserver progress,
        CancellationToken cancellationToken)
    {
        List<FileOperationEffect> effects = [];
        foreach (FileEntrySnapshot snapshot in snapshots)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return FileOperationOutcome.Cancelled(effects);
            }
            FileOperationOutcome? stopped = await DeleteOneAsync(snapshot, effects, cancellationToken);
            if (stopped is not null)
            {
                return stopped;
            }
            progress.Report(FileOperationProgress.Create(effects.Count, request.Sources.Count));
        }
        return FileOperationOutcome.Succeeded(effects);
    }

    private async Task<FileOperationOutcome?> DeleteOneAsync(
        FileEntrySnapshot snapshot,
        List<FileOperationEffect> effects,
        CancellationToken cancellationToken)
    {
        DeletionExecutionMode mode = snapshot.DeletionCapability == DeletionCapability.Recycle
            ? DeletionExecutionMode.Recycle
            : DeletionExecutionMode.Permanent;
        ProviderStepOutcome deletion = await _port.DeleteAsync(snapshot, mode, cancellationToken);
        if (deletion.Failure is not null)
        {
            if (deletion.Effect == ProviderStepEffectKind.Recycled)
            {
                effects.Add(FileOperationEffect.Create(snapshot.Path, FileOperationEffectKind.Recycled));
            }
            return cancellationToken.IsCancellationRequested
                ? FileOperationOutcome.Cancelled(effects)
                : FileOperationOutcome.Failed(effects, deletion.Failure);
        }
        FileOperationEffectKind effect = mode == DeletionExecutionMode.Recycle
            ? FileOperationEffectKind.Recycled
            : FileOperationEffectKind.PermanentlyDeleted;
        effects.Add(FileOperationEffect.Create(snapshot.Path, effect));
        return null;
    }

    private async Task<FileOperationOutcome> ExecuteCreateDirectoryAsync(
        CreateDirectoryRequest request,
        IFileOperationProgressObserver progress,
        CancellationToken cancellationToken)
    {
        List<FileOperationEffect> effects = [];
        FileInspectionOutcome inspection = await _port.InspectAsync(request.Location, cancellationToken);
        if (inspection is FileInspectionFailed failed)
        {
            return FileOperationOutcome.Failed(effects, failed.Failure);
        }
        if (cancellationToken.IsCancellationRequested)
        {
            return FileOperationOutcome.Cancelled(effects);
        }

        FileEntrySnapshot location = ((FileInspectionSucceeded)inspection).Snapshot;
        ProviderStepOutcome creation = await _port.CreateDirectoryAsync(location, request.Target, cancellationToken);
        if (creation.Failure is not null)
        {
            return FileOperationOutcome.Failed(effects, creation.Failure);
        }
        effects.Add(FileOperationEffect.Create(request.Target, FileOperationEffectKind.DirectoryCreated));
        progress.Report(FileOperationProgress.Create(1, 1));
        return FileOperationOutcome.Succeeded(effects);
    }

    private async Task<FileOperationOutcome> ExecuteRenameAsync(
        RenameRequest request,
        IFileOperationProgressObserver progress,
        CancellationToken cancellationToken)
    {
        List<FileOperationEffect> effects = [];
        FileInspectionOutcome inspection = await _port.InspectAsync(request.Source, cancellationToken);
        if (inspection is FileInspectionFailed failed)
        {
            return FileOperationOutcome.Failed(effects, failed.Failure);
        }
        if (cancellationToken.IsCancellationRequested)
        {
            return FileOperationOutcome.Cancelled(effects);
        }

        FileEntrySnapshot source = ((FileInspectionSucceeded)inspection).Snapshot;
        ProviderStepOutcome rename = await _port.RenameAsync(source, request.Target, cancellationToken);
        if (rename.Failure is not null)
        {
            return FileOperationOutcome.Failed(effects, rename.Failure);
        }
        effects.Add(FileOperationEffect.Create(request.Source, FileOperationEffectKind.Renamed));
        progress.Report(FileOperationProgress.Create(1, 1));
        return FileOperationOutcome.Succeeded(effects);
    }

    private async Task<InspectionBatch> InspectAllAsync(
        IReadOnlyList<FileSystemPath> sources,
        CancellationToken cancellationToken)
    {
        List<FileEntrySnapshot> snapshots = [];
        foreach (FileSystemPath source in sources)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return InspectionBatch.CancelledBatch();
            }
            FileInspectionOutcome inspection = await _port.InspectAsync(source, cancellationToken);
            switch (inspection)
            {
                case FileInspectionSucceeded succeeded:
                    snapshots.Add(succeeded.Snapshot);
                    break;
                case FileInspectionFailed failed:
                    return InspectionBatch.Failed(failed.Failure);
                default:
                    throw new InvalidOperationException("The inspection outcome variant is not executable.");
            }
        }
        return InspectionBatch.Succeeded(snapshots);
    }

    private static bool NeedsConfirmation(IReadOnlyList<FileEntrySnapshot> snapshots)
    {
        return snapshots.Any(snapshot => snapshot.DeletionCapability == DeletionCapability.PermanentOnly);
    }

    private sealed class InspectionBatch
    {
        private InspectionBatch(
            IReadOnlyList<FileEntrySnapshot> snapshots,
            FileOperationFailureKind? failure,
            InspectionBatchCompletion completion)
        {
            Snapshots = snapshots;
            Failure = failure;
            Completion = completion;
        }

        internal IReadOnlyList<FileEntrySnapshot> Snapshots { get; }
        internal FileOperationFailureKind? Failure { get; }
        internal InspectionBatchCompletion Completion { get; }

        internal static InspectionBatch Succeeded(IReadOnlyList<FileEntrySnapshot> snapshots)
        {
            return new InspectionBatch(snapshots, null, InspectionBatchCompletion.Succeeded);
        }

        internal static InspectionBatch Failed(FileOperationFailureKind failure)
        {
            return new InspectionBatch(Array.Empty<FileEntrySnapshot>(), failure, InspectionBatchCompletion.Failed);
        }

        internal static InspectionBatch CancelledBatch()
        {
            return new InspectionBatch(Array.Empty<FileEntrySnapshot>(), null, InspectionBatchCompletion.Cancelled);
        }
    }

    private abstract record InspectionBatchCompletion
    {
        internal static InspectionBatchCompletion Succeeded { get; } = new SucceededCompletion();
        internal static InspectionBatchCompletion Failed { get; } = new FailedCompletion();
        internal static InspectionBatchCompletion Cancelled { get; } = new CancelledCompletion();

        private InspectionBatchCompletion()
        {
        }

        private sealed record SucceededCompletion : InspectionBatchCompletion;
        private sealed record FailedCompletion : InspectionBatchCompletion;
        private sealed record CancelledCompletion : InspectionBatchCompletion;
    }
}
