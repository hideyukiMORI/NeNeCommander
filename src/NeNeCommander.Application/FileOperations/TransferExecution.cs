using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Executes one frozen transfer attempt under its gateway's existing execution lease.</summary>
internal sealed class TransferExecution
{
    private readonly IFileOperationPort _port;
    private readonly FrozenTransfer _transfer;
    private readonly object _continuationOwner;
    private readonly List<FileOperationEffect> _effects = [];
    private readonly List<FileSystemPath> _notTransferred = [];

    internal TransferExecution(IFileOperationPort port, FrozenTransfer transfer, object continuationOwner)
    {
        _port = port;
        _transfer = transfer;
        _continuationOwner = continuationOwner;
    }

    internal async Task<FileOperationOutcome> ExecuteAsync(
        TransferResolution resolution,
        IFileOperationProgressObserver progress,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<FileEntrySnapshot> preflightSources = _transfer.Sources
            .Select(snapshot => snapshot.WithConflictChoice(resolution.Find(snapshot.Path)))
            .ToList()
            .AsReadOnly();
        TransferPreflightOutcome preflight = await _port.PreflightTransferAsync(
            preflightSources,
            _transfer.Destination,
            cancellationToken);
        if (cancellationToken.IsCancellationRequested)
        {
            return FileOperationOutcome.Cancelled(_effects);
        }
        if (preflight is TransferPreflightRejected rejected)
        {
            return FileOperationOutcome.Failed(_effects, rejected.Failure);
        }
        if (preflight is ConflictSet conflicts)
        {
            return TransferPreflightValidation.IsValidConflictSet(conflicts, preflightSources, _transfer.Destination)
                ? FileOperationOutcome.AwaitingConflict(
                    conflicts,
                    TransferContinuation.Create(_transfer, resolution, conflicts, _continuationOwner))
                : FileOperationOutcome.Failed(_effects, FileOperationFailureKind.ProviderUnavailable);
        }
        IReadOnlyList<TransferPlanEntry> plan = ((TransferPreflightSucceeded)preflight).Plan;
        return TransferPreflightValidation.IsValidPlan(plan, preflightSources, _transfer.Destination)
            ? await ExecutePlanAsync(plan, progress, cancellationToken)
            : FileOperationOutcome.Failed(_effects, FileOperationFailureKind.ProviderUnavailable);
    }

    private async Task<FileOperationOutcome> ExecutePlanAsync(
        IReadOnlyList<TransferPlanEntry> plan,
        IFileOperationProgressObserver progress,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AtomicMoveCapabilityOutcome> capabilities = await GetAtomicMoveCapabilitiesAsync(plan, cancellationToken);
        AtomicMoveCapabilityFailed? capabilityFailure = capabilities.OfType<AtomicMoveCapabilityFailed>().FirstOrDefault();
        if (cancellationToken.IsCancellationRequested)
        {
            return FileOperationOutcome.Cancelled(_effects);
        }
        if (capabilityFailure is not null)
        {
            return FileOperationOutcome.Failed(_effects, capabilityFailure.Failure);
        }
        int completed = 0;
        for (int index = 0; index < plan.Count; index++)
        {
            FileOperationOutcome? stopped = await ExecuteEntryAsync(plan[index], capabilities[index], cancellationToken);
            if (stopped is not null)
            {
                return MergeNotTransferred(stopped, _notTransferred);
            }
            completed++;
            progress.Report(FileOperationProgress.Create(completed, plan.Count));
        }
        return FileOperationOutcome.Succeeded(_effects, _notTransferred);
    }

    private async Task<FileOperationOutcome?> ExecuteEntryAsync(
        TransferPlanEntry entry,
        AtomicMoveCapabilityOutcome capability,
        CancellationToken cancellationToken)
    {
        if (entry.Disposition == TransferDisposition.Skip)
        {
            _notTransferred.Add(entry.Source.Path);
            return null;
        }
        FileEntrySnapshot snapshot = entry.Source.WithTransferTarget(entry.Target);
        return _transfer.Request is MoveRequest
            ? await MoveOneAsync(snapshot, capability, cancellationToken)
            : (await CopyOneAsync(snapshot, cancellationToken)).Stopped;
    }

    private async Task<IReadOnlyList<AtomicMoveCapabilityOutcome>> GetAtomicMoveCapabilitiesAsync(
        IReadOnlyList<TransferPlanEntry> plan,
        CancellationToken cancellationToken)
    {
        List<AtomicMoveCapabilityOutcome> capabilities = [];
        foreach (TransferPlanEntry entry in plan)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            AtomicMoveCapabilityOutcome capability = _transfer.Request is MoveRequest &&
                entry.Disposition == TransferDisposition.Transfer &&
                entry.Source.ConflictChoice?.Decision != TransferConflictDecision.KeepBoth
                ? await _port.GetAtomicMoveCapabilityAsync(
                    entry.Source.WithTransferTarget(entry.Target),
                    _transfer.Destination,
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

    private async Task<CopyExecution> CopyOneAsync(
        FileEntrySnapshot snapshot,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return CopyExecution.Stop(FileOperationOutcome.Cancelled(_effects));
        }
        ProviderStepOutcome copy = await _port.CopyAsync(snapshot, _transfer.Destination, cancellationToken);
        AddCopyProviderEffect(snapshot, copy);
        if (copy.Failure is not null)
        {
            return CopyExecution.Stop(FileOperationOutcome.Failed(_effects, copy.Failure));
        }
        _effects.Add(FileOperationEffect.Create(snapshot.Path, FileOperationEffectKind.Copied));

        if (cancellationToken.IsCancellationRequested)
        {
            return CopyExecution.Stop(FileOperationOutcome.Cancelled(_effects));
        }

        CopyVerificationOutcome verification = await _port.VerifyCopyAsync(snapshot, _transfer.Destination, cancellationToken);
        if (verification.Failure is not null)
        {
            return CopyExecution.Stop(FileOperationOutcome.Failed(_effects, verification.Failure));
        }
        VerifiedCopyReceipt receipt = ((CopyVerified)verification).Receipt;
        if (receipt.Source.Identity != snapshot.Identity ||
            !FileSystemPathIdentityComparer.Instance.Equals(receipt.Source.Path, snapshot.Path) ||
            !FileSystemPathIdentityComparer.Instance.Equals(receipt.Root.Target.Path, snapshot.TransferTarget))
        {
            return CopyExecution.Stop(FileOperationOutcome.Failed(_effects, FileOperationFailureKind.Verification));
        }
        _effects.Add(FileOperationEffect.Create(snapshot.Path, FileOperationEffectKind.Verified));
        return CopyExecution.Complete(receipt);
    }

    private void AddCopyProviderEffect(
        FileEntrySnapshot snapshot,
        ProviderStepOutcome copy)
    {
        if (copy.Effect == ProviderStepEffectKind.CopyTargetCreated)
        {
            _effects.Add(FileOperationEffect.Create(snapshot.Path, FileOperationEffectKind.CopyTargetCreated));
        }
    }

    private async Task<FileOperationOutcome?> MoveOneAsync(
        FileEntrySnapshot snapshot,
        AtomicMoveCapabilityOutcome capability,
        CancellationToken cancellationToken)
    {
        if (capability == AtomicMoveCapabilityOutcome.Supported)
        {
            return await MoveAtomicallyAsync(snapshot, cancellationToken);
        }
        CopyExecution copy = await CopyOneAsync(snapshot, cancellationToken);
        if (copy.Stopped is not null)
        {
            return copy.Stopped;
        }
        if (cancellationToken.IsCancellationRequested)
        {
            return FileOperationOutcome.Cancelled(_effects);
        }

        ProviderStepOutcome deletion = await _port.DeleteAsync(
            snapshot,
            DeletionExecutionMode.VerifiedCopy(((CopyCompleted)copy).Receipt),
            cancellationToken);
        if (deletion.Effect == ProviderStepEffectKind.SourcePartiallyDeleted)
        {
            _effects.Add(FileOperationEffect.Create(snapshot.Path, FileOperationEffectKind.SourcePartiallyDeleted));
        }
        if (deletion.Failure is not null)
        {
            return FileOperationOutcome.Failed(_effects, deletion.Failure);
        }
        _effects.Add(FileOperationEffect.Create(snapshot.Path, FileOperationEffectKind.SourceDeleted));
        return null;
    }

    private async Task<FileOperationOutcome?> MoveAtomicallyAsync(
        FileEntrySnapshot snapshot,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return FileOperationOutcome.Cancelled(_effects);
        }
        ProviderStepOutcome move = await _port.MoveAsync(snapshot, _transfer.Destination, cancellationToken);
        if (move.Failure is not null)
        {
            return FileOperationOutcome.Failed(_effects, move.Failure);
        }
        _effects.Add(FileOperationEffect.Create(snapshot.Path, FileOperationEffectKind.AtomicallyMoved));
        return null;
    }

    private abstract record CopyExecution
    {
        private protected CopyExecution()
        {
        }

        internal abstract FileOperationOutcome? Stopped { get; }

        internal static CopyStopped Stop(FileOperationOutcome outcome)
        {
            return new CopyStopped(outcome);
        }

        internal static CopyCompleted Complete(VerifiedCopyReceipt receipt)
        {
            return new CopyCompleted(receipt);
        }
    }

    private sealed record CopyStopped : CopyExecution
    {
        internal CopyStopped(FileOperationOutcome stopped)
        {
            Stopped = stopped;
        }

        internal override FileOperationOutcome Stopped { get; }
    }

    private sealed record CopyCompleted : CopyExecution
    {
        internal CopyCompleted(VerifiedCopyReceipt receipt)
        {
            Receipt = receipt;
        }

        internal VerifiedCopyReceipt Receipt { get; }
        internal override FileOperationOutcome? Stopped => null;
    }
}
