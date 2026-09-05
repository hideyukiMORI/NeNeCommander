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

    /// <summary>Initializes the gateway with its sole provider-neutral side-effect port.</summary>
    /// <param name="port">Provider-neutral file operation port.</param>
    public FileOperationGateway(IFileOperationPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _port = port;
        _executionLease = new SemaphoreSlim(1, 1);
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
        List<FileOperationEffect> effects = [];
        InspectionBatch inspection = await InspectAllAsync(sources, cancellationToken);
        if (inspection.Completion == InspectionBatchCompletion.Cancelled)
        {
            return FileOperationOutcome.Cancelled(effects);
        }
        if (inspection.Failure is not null)
        {
            return FileOperationOutcome.Failed(effects, inspection.Failure);
        }

        ProviderStepOutcome preflight = await _port.PreflightTransferAsync(
            inspection.Snapshots,
            destination,
            cancellationToken);
        if (cancellationToken.IsCancellationRequested)
        {
            return FileOperationOutcome.Cancelled(effects);
        }
        if (preflight.Failure is not null)
        {
            return FileOperationOutcome.Failed(effects, preflight.Failure);
        }

        IReadOnlyList<AtomicMoveCapabilityOutcome> capabilities = await GetAtomicMoveCapabilitiesAsync(
            request,
            inspection.Snapshots,
            destination,
            cancellationToken);
        AtomicMoveCapabilityFailed? capabilityFailure = capabilities
            .OfType<AtomicMoveCapabilityFailed>()
            .FirstOrDefault();
        if (cancellationToken.IsCancellationRequested)
        {
            return FileOperationOutcome.Cancelled(effects);
        }
        if (capabilityFailure is not null)
        {
            return FileOperationOutcome.Failed(effects, capabilityFailure.Failure);
        }

        int completed = 0;
        for (int index = 0; index < inspection.Snapshots.Count; index++)
        {
            FileEntrySnapshot snapshot = inspection.Snapshots[index];
            FileOperationOutcome? stopped = request is MoveRequest
                ? await MoveOneAsync(snapshot, destination, capabilities[index], effects, cancellationToken)
                : await CopyOneAsync(snapshot, destination, effects, cancellationToken);
            if (stopped is not null)
            {
                return stopped;
            }
            completed++;
            progress.Report(FileOperationProgress.Create(completed, sources.Count));
        }
        return FileOperationOutcome.Succeeded(effects);
    }

    private async Task<IReadOnlyList<AtomicMoveCapabilityOutcome>> GetAtomicMoveCapabilitiesAsync(
        FileOperationRequest request,
        IReadOnlyList<FileEntrySnapshot> snapshots,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        List<AtomicMoveCapabilityOutcome> capabilities = [];
        foreach (FileEntrySnapshot snapshot in snapshots)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            AtomicMoveCapabilityOutcome capability = request is MoveRequest
                ? await _port.GetAtomicMoveCapabilityAsync(snapshot, destination, cancellationToken)
                : AtomicMoveCapabilityOutcome.Unsupported;
            capabilities.Add(capability);
        }
        return capabilities.AsReadOnly();
    }

    private async Task<FileOperationOutcome> ExecuteDeleteAsync(
        DeleteRequest request,
        IFileOperationProgressObserver progress,
        CancellationToken cancellationToken)
    {
        List<FileOperationEffect> effects = [];
        InspectionBatch inspection = await InspectAllAsync(request.Sources, cancellationToken);
        if (inspection.Completion == InspectionBatchCompletion.Cancelled)
        {
            return FileOperationOutcome.Cancelled(effects);
        }
        if (inspection.Failure is not null)
        {
            return FileOperationOutcome.Failed(effects, inspection.Failure);
        }
        if (NeedsConfirmation(inspection.Snapshots) &&
            (request.Confirmation is null || !request.Confirmation.Covers(request.Sources)))
        {
            return FileOperationOutcome.Failed(effects, FileOperationFailureKind.ConfirmationRequired);
        }

        foreach (FileEntrySnapshot snapshot in inspection.Snapshots)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return FileOperationOutcome.Cancelled(effects);
            }
            DeletionExecutionMode mode = snapshot.DeletionCapability == DeletionCapability.Recycle
                ? DeletionExecutionMode.Recycle
                : DeletionExecutionMode.Permanent;
            ProviderStepOutcome deletion = await _port.DeleteAsync(snapshot, mode, cancellationToken);
            if (deletion.Failure is not null)
            {
                return FileOperationOutcome.Failed(effects, deletion.Failure);
            }
            FileOperationEffectKind effect = mode == DeletionExecutionMode.Recycle
                ? FileOperationEffectKind.Recycled
                : FileOperationEffectKind.PermanentlyDeleted;
            effects.Add(FileOperationEffect.Create(snapshot.Path, effect));
            progress.Report(FileOperationProgress.Create(effects.Count, request.Sources.Count));
        }
        return FileOperationOutcome.Succeeded(effects);
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

    private async Task<FileOperationOutcome?> CopyOneAsync(
        FileEntrySnapshot snapshot,
        FileSystemPath destination,
        List<FileOperationEffect> effects,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return FileOperationOutcome.Cancelled(effects);
        }
        ProviderStepOutcome copy = await _port.CopyAsync(snapshot, destination, cancellationToken);
        AddCopyProviderEffect(snapshot, copy, effects);
        if (copy.Failure is not null)
        {
            return FileOperationOutcome.Failed(effects, copy.Failure);
        }
        effects.Add(FileOperationEffect.Create(snapshot.Path, FileOperationEffectKind.Copied));

        if (cancellationToken.IsCancellationRequested)
        {
            return FileOperationOutcome.Cancelled(effects);
        }

        ProviderStepOutcome verification = await _port.VerifyCopyAsync(snapshot, destination, cancellationToken);
        if (verification.Failure is not null)
        {
            return FileOperationOutcome.Failed(effects, verification.Failure);
        }
        effects.Add(FileOperationEffect.Create(snapshot.Path, FileOperationEffectKind.Verified));
        return null;
    }

    private static void AddCopyProviderEffect(
        FileEntrySnapshot snapshot,
        ProviderStepOutcome copy,
        List<FileOperationEffect> effects)
    {
        if (copy.Effect is not null)
        {
            effects.Add(FileOperationEffect.Create(snapshot.Path, FileOperationEffectKind.CopyTargetCreated));
        }
    }

    private async Task<FileOperationOutcome?> MoveOneAsync(
        FileEntrySnapshot snapshot,
        FileSystemPath destination,
        AtomicMoveCapabilityOutcome capability,
        List<FileOperationEffect> effects,
        CancellationToken cancellationToken)
    {
        if (capability == AtomicMoveCapabilityOutcome.Supported)
        {
            return await MoveAtomicallyAsync(snapshot, destination, effects, cancellationToken);
        }
        FileOperationOutcome? stopped = await CopyOneAsync(snapshot, destination, effects, cancellationToken);
        if (stopped is not null)
        {
            return stopped;
        }
        if (cancellationToken.IsCancellationRequested)
        {
            return FileOperationOutcome.Cancelled(effects);
        }

        ProviderStepOutcome deletion = await _port.DeleteAsync(snapshot, DeletionExecutionMode.Permanent, cancellationToken);
        if (deletion.Failure is not null)
        {
            return FileOperationOutcome.Failed(effects, deletion.Failure);
        }
        effects.Add(FileOperationEffect.Create(snapshot.Path, FileOperationEffectKind.SourceDeleted));
        return null;
    }

    private async Task<FileOperationOutcome?> MoveAtomicallyAsync(
        FileEntrySnapshot snapshot,
        FileSystemPath destination,
        List<FileOperationEffect> effects,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return FileOperationOutcome.Cancelled(effects);
        }
        ProviderStepOutcome move = await _port.MoveAsync(snapshot, destination, cancellationToken);
        if (move.Failure is not null)
        {
            return FileOperationOutcome.Failed(effects, move.Failure);
        }
        effects.Add(FileOperationEffect.Create(snapshot.Path, FileOperationEffectKind.AtomicallyMoved));
        return null;
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
