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
        TransferConflictDecision decision,
        TransferConflictScope scope,
        IFileOperationProgressObserver progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(continuation);
        ArgumentNullException.ThrowIfNull(decision);
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(progress);
        if (!continuation.IsOwnedBy(_continuationOwner))
        {
            return FileOperationOutcome.Failed([], FileOperationFailureKind.Reentrant);
        }
        if (decision == TransferConflictDecision.Cancel || cancellationToken.IsCancellationRequested)
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
                decision,
                scope);
            return await ExecuteFrozenTransferAsync(
                continuation.Request,
                continuation.Sources,
                continuation.Destination,
                resolution,
                progress,
                cancellationToken);
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
            : await ExecuteFrozenTransferAsync(
                request,
                inspection.Snapshots,
                destination,
                TransferResolution.None,
                progress,
                cancellationToken);
    }

    private async Task<FileOperationOutcome> ExecuteFrozenTransferAsync(
        FileOperationRequest request,
        IReadOnlyList<FileEntrySnapshot> snapshots,
        FileSystemPath destination,
        TransferResolution resolution,
        IFileOperationProgressObserver progress,
        CancellationToken cancellationToken)
    {
        List<FileOperationEffect> effects = [];
        List<FileSystemPath> notTransferred = [];

        IReadOnlyList<FileEntrySnapshot> preflightSources = snapshots
            .Select(snapshot => snapshot.WithConflictChoice(resolution.Find(snapshot.Path)))
            .ToList()
            .AsReadOnly();
        TransferPreflightOutcome preflight = await _port.PreflightTransferAsync(
            preflightSources,
            destination,
            cancellationToken);
        if (cancellationToken.IsCancellationRequested)
        {
            return FileOperationOutcome.Cancelled(effects);
        }
        if (preflight is TransferPreflightRejected rejected)
        {
            return FileOperationOutcome.Failed(effects, rejected.Failure);
        }
        if (preflight is ConflictSet conflictSet)
        {
            return IsValidConflictSet(conflictSet, preflightSources, destination)
                ? FileOperationOutcome.AwaitingConflict(
                    conflictSet,
                    TransferContinuation.Create(
                        request,
                        snapshots,
                        destination,
                        resolution,
                        conflictSet,
                        _continuationOwner))
                : FileOperationOutcome.Failed(effects, FileOperationFailureKind.ProviderUnavailable);
        }
        IReadOnlyList<TransferPlanEntry> plan = ((TransferPreflightSucceeded)preflight).Plan;
        if (!IsValidPlan(plan, preflightSources, destination))
        {
            return FileOperationOutcome.Failed(effects, FileOperationFailureKind.ProviderUnavailable);
        }

        IReadOnlyList<AtomicMoveCapabilityOutcome> capabilities = await GetAtomicMoveCapabilitiesAsync(
            request,
            plan,
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
        for (int index = 0; index < plan.Count; index++)
        {
            TransferPlanEntry entry = plan[index];
            if (entry.Disposition == TransferDisposition.Skip)
            {
                notTransferred.Add(entry.Source.Path);
                completed++;
                progress.Report(FileOperationProgress.Create(completed, plan.Count));
                continue;
            }
            FileEntrySnapshot snapshot = entry.Source.WithTransferTarget(entry.Target);
            FileOperationOutcome? stopped = request is MoveRequest
                ? await MoveOneAsync(snapshot, destination, capabilities[index], effects, cancellationToken)
                : await CopyOneAsync(snapshot, destination, effects, cancellationToken);
            if (stopped is not null)
            {
                return MergeNotTransferred(stopped, notTransferred);
            }
            completed++;
            progress.Report(FileOperationProgress.Create(completed, plan.Count));
        }
        return FileOperationOutcome.Succeeded(effects, notTransferred);
    }

    private static bool IsValidConflictSet(
        ConflictSet conflicts,
        IReadOnlyList<FileEntrySnapshot> sources,
        FileSystemPath destination)
    {
        Dictionary<FileSystemPath, int> sourceIndexes = new(FileSystemPathIdentityComparer.Instance);
        for (int sourceIndex = 0; sourceIndex < sources.Count; sourceIndex++)
        {
            sourceIndexes.Add(sources[sourceIndex].Path, sourceIndex);
        }
        int previousSourceIndex = -1;
        foreach (TransferConflict conflict in conflicts.Conflicts)
        {
            if (!sourceIndexes.TryGetValue(conflict.Source.Path, out int sourceIndex) ||
                sourceIndex <= previousSourceIndex ||
                !MatchesSnapshot(sources[sourceIndex], conflict.Source) ||
                !IsDirectChild(conflict.ExistingTarget, destination) ||
                !IsDirectChild(conflict.KeepBothCandidate, destination) ||
                FileSystemPathIdentityComparer.Instance.Equals(
                    conflict.ExistingTarget,
                    conflict.KeepBothCandidate))
            {
                return false;
            }
            previousSourceIndex = sourceIndex;
        }
        return true;
    }

    private static bool IsValidPlan(
        IReadOnlyList<TransferPlanEntry> plan,
        IReadOnlyList<FileEntrySnapshot> sources,
        FileSystemPath destination)
    {
        if (plan.Count != sources.Count)
        {
            return false;
        }
        for (int index = 0; index < plan.Count; index++)
        {
            TransferPlanEntry entry = plan[index];
            FileEntrySnapshot expected = sources[index];
            if (!MatchesSnapshot(expected, entry.Source) ||
                !IsDirectChild(entry.Target, destination) ||
                !MatchesDisposition(expected.ConflictChoice, entry))
            {
                return false;
            }
        }
        return true;
    }

    private static bool MatchesSnapshot(FileEntrySnapshot expected, FileEntrySnapshot actual)
    {
        return FileSystemPathIdentityComparer.Instance.Equals(expected.Path, actual.Path) &&
            expected.Identity == actual.Identity &&
            expected.DeletionCapability == actual.DeletionCapability &&
            actual.TransferTarget is null &&
            MatchesChoice(expected.ConflictChoice, actual.ConflictChoice);
    }

    private static bool MatchesChoice(
        TransferConflictChoice? expected,
        TransferConflictChoice? actual)
    {
        return expected is null
            ? actual is null
            : actual is not null &&
                FileSystemPathIdentityComparer.Instance.Equals(expected.Source, actual.Source) &&
                expected.Decision == actual.Decision &&
                FileSystemPathIdentityComparer.Instance.Equals(
                    expected.KeepBothCandidate,
                    actual.KeepBothCandidate);
    }

    private static bool MatchesDisposition(
        TransferConflictChoice? choice,
        TransferPlanEntry entry)
    {
        return choice is null
            ? entry.Disposition == TransferDisposition.Transfer
            : choice.Decision == TransferConflictDecision.Skip
                ? entry.Disposition == TransferDisposition.Skip
                : choice.Decision == TransferConflictDecision.KeepBoth &&
                    entry.Disposition == TransferDisposition.Transfer &&
                    FileSystemPathIdentityComparer.Instance.Equals(
                        choice.KeepBothCandidate,
                        entry.Target);
    }

    private static bool IsDirectChild(FileSystemPath target, FileSystemPath destination)
    {
        return target.Parent is FileSystemPath parent &&
            FileSystemPathIdentityComparer.Instance.Equals(parent, destination);
    }

    private async Task<IReadOnlyList<AtomicMoveCapabilityOutcome>> GetAtomicMoveCapabilitiesAsync(
        FileOperationRequest request,
        IReadOnlyList<TransferPlanEntry> plan,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        List<AtomicMoveCapabilityOutcome> capabilities = [];
        foreach (TransferPlanEntry entry in plan)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            AtomicMoveCapabilityOutcome capability = request is MoveRequest &&
                entry.Disposition == TransferDisposition.Transfer &&
                entry.Source.ConflictChoice?.Decision != TransferConflictDecision.KeepBoth
                ? await _port.GetAtomicMoveCapabilityAsync(
                    entry.Source.WithTransferTarget(entry.Target),
                    destination,
                    cancellationToken)
                : AtomicMoveCapabilityOutcome.Unsupported;
            capabilities.Add(capability);
        }
        return capabilities.AsReadOnly();
    }

    private static FileOperationOutcome MergeNotTransferred(
        FileOperationOutcome outcome,
        IReadOnlyList<FileSystemPath> notTransferred)
    {
        return outcome.Failure is FileOperationFailureKind failure
            ? FileOperationOutcome.Failed(outcome.Effects, notTransferred, failure)
            : FileOperationOutcome.Cancelled(outcome.Effects, notTransferred);
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
