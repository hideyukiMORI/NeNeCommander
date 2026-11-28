using System.Collections.Generic;
using System.Collections.Immutable;
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
    private readonly FileOperationEffectJournal _effects = new();
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
        ImmutableList<FileEntrySnapshot> preflightSources = [.. _transfer.Sources
            .Select(snapshot => snapshot.WithConflictChoice(resolution.Find(snapshot.Path)))];
        TransferPreflightOutcome preflight = await _port.PreflightTransferAsync(
            preflightSources,
            _transfer.Destination,
            cancellationToken);
        if (cancellationToken.IsCancellationRequested)
        {
            return FileOperationOutcome.Cancelled(_effects.Snapshot(_notTransferred));
        }
        if (preflight is TransferPreflightRejected rejected)
        {
            return FileOperationOutcome.Failed(_effects.Snapshot(_notTransferred), rejected.Failure);
        }
        if (preflight is ConflictSet conflicts)
        {
            return TransferPreflightValidation.IsValidConflictSet(conflicts, preflightSources, _transfer.Destination)
                ? FileOperationOutcome.AwaitingConflict(
                    conflicts,
                    TransferContinuation.Create(_transfer, resolution, conflicts, _continuationOwner))
                : FileOperationOutcome.Failed(_effects.Snapshot(_notTransferred), FileOperationFailureKind.ProviderUnavailable);
        }
        IReadOnlyList<TransferPlanEntry> plan = ((TransferPreflightSucceeded)preflight).Plan;
        return TransferPreflightValidation.IsValidPlan(plan, preflightSources, _transfer.Destination)
            ? await ExecutePlanAsync(plan, progress, cancellationToken)
            : FileOperationOutcome.Failed(_effects.Snapshot(_notTransferred), FileOperationFailureKind.ProviderUnavailable);
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
            return FileOperationOutcome.Cancelled(_effects.Snapshot(_notTransferred));
        }
        if (capabilityFailure is not null)
        {
            return FileOperationOutcome.Failed(_effects.Snapshot(_notTransferred), capabilityFailure.Failure);
        }
        int completed = 0;
        for (int index = 0; index < plan.Count; index++)
        {
            FileOperationOutcome? stopped = await ExecuteEntryAsync(plan[index], capabilities[index], cancellationToken);
            if (stopped is not null)
            {
                return stopped;
            }
            completed++;
            progress.Report(FileOperationProgress.Create(completed, plan.Count));
        }
        return FileOperationOutcome.Succeeded(_effects.Snapshot(_notTransferred));
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
        // Only local capability state continues here; the caller retains effect and progress affinity.
        List<AtomicMoveCapabilityOutcome> capabilities = [];
        foreach (TransferPlanEntry entry in plan)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            AtomicMoveCapabilityOutcome capability = _transfer.Request is MoveRequest &&
                entry.Disposition == TransferDisposition.Transfer &&
                entry.Source.ConflictChoice is null
                ? await _port.GetAtomicMoveCapabilityAsync(
                    entry.Source.WithTransferTarget(entry.Target),
                    _transfer.Destination,
                    cancellationToken).ConfigureAwait(false)
                : AtomicMoveCapabilityOutcome.Unsupported;
            capabilities.Add(capability);
        }
        return capabilities.AsReadOnly();
    }

    private async Task<CopyExecution> CopyOneAsync(
        FileEntrySnapshot snapshot,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return CopyExecution.Stop(FileOperationOutcome.Cancelled(_effects.Snapshot(_notTransferred)));
        }
        ProviderStepOutcome copy = await _port.CopyAsync(snapshot, _transfer.Destination, cancellationToken);
        _effects.RecordProvider(snapshot.Path, copy.Receipt);
        AddCopyProviderEffect(snapshot, copy);
        if (copy.Completion == ProviderStepCompletion.Cancelled)
        {
            return CopyExecution.Stop(FileOperationOutcome.Cancelled(_effects.Snapshot(_notTransferred)));
        }
        if (copy.Failure is not null)
        {
            return CopyExecution.Stop(FileOperationOutcome.Failed(_effects.Snapshot(_notTransferred), copy.Failure));
        }
        _effects.Add(FileOperationEffect.Create(snapshot.Path, FileOperationEffectKind.Copied));

        return cancellationToken.IsCancellationRequested
            ? CopyExecution.Stop(FileOperationOutcome.Cancelled(_effects.Snapshot(_notTransferred)))
            : await VerifyCopiedAsync(snapshot, cancellationToken);
    }

    private async Task<CopyExecution> VerifyCopiedAsync(
        FileEntrySnapshot snapshot,
        CancellationToken cancellationToken)
    {
        CopyVerificationOutcome verification = await _port.VerifyCopyAsync(snapshot, _transfer.Destination, cancellationToken);
        if (verification.Failure is not null)
        {
            return CopyExecution.Stop(FileOperationOutcome.Failed(_effects.Snapshot(_notTransferred), verification.Failure));
        }
        VerifiedCopyReceipt receipt = ((CopyVerified)verification).Receipt;
        if (receipt.Source.Identity != snapshot.Identity ||
            !FileSystemPathIdentityComparer.Instance.Equals(receipt.Source.Path, snapshot.Path) ||
            !FileSystemPathIdentityComparer.Instance.Equals(receipt.Root.Target.Path, snapshot.TransferTarget) ||
            !MatchesOverwriteVerification(snapshot.ConflictChoice, receipt.Root))
        {
            return CopyExecution.Stop(FileOperationOutcome.Failed(_effects.Snapshot(_notTransferred), FileOperationFailureKind.Verification));
        }
        _effects.Add(FileOperationEffect.Create(snapshot.Path, FileOperationEffectKind.Verified));
        return CopyExecution.Complete(receipt);
    }

    private static bool MatchesOverwriteVerification(TransferConflictChoice? choice, VerifiedCopyEntry root)
    {
        return choice?.Decision != TransferConflictDecision.OverwriteContents ||
            (choice.Target.OverwriteEvidence is TransferOverwriteEvidence original &&
                root is VerifiedCopyEntry.VerifiedFile file &&
                file.Requirements == FileVerificationRequirements.OverwriteContents &&
                file.Source.ObjectIdentity == original.Source.Observation.Entry.ObjectIdentity &&
                file.Target.ObjectIdentity == original.Target.Observation.Entry.ObjectIdentity);
    }

    private void AddCopyProviderEffect(
        FileEntrySnapshot snapshot,
        ProviderStepOutcome copy)
    {
        if (copy.Receipt.Effects.Contains(ProviderStepEffectKind.CopyTargetCreated))
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
            return FileOperationOutcome.Cancelled(_effects.Snapshot(_notTransferred));
        }

        ProviderStepOutcome deletion = await _port.DeleteAsync(
            snapshot,
            DeletionExecutionMode.VerifiedCopy(((CopyCompleted)copy).Receipt),
            cancellationToken);
        _effects.RecordProvider(snapshot.Path, deletion.Receipt);
        if (deletion.Receipt.Effects.Contains(ProviderStepEffectKind.SourcePartiallyDeleted))
        {
            _effects.Add(FileOperationEffect.Create(snapshot.Path, FileOperationEffectKind.SourcePartiallyDeleted));
        }
        if (deletion.Completion == ProviderStepCompletion.Cancelled)
        {
            return FileOperationOutcome.Cancelled(_effects.Snapshot(_notTransferred));
        }
        if (deletion.Failure is not null)
        {
            return FileOperationOutcome.Failed(_effects.Snapshot(_notTransferred), deletion.Failure);
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
            return FileOperationOutcome.Cancelled(_effects.Snapshot(_notTransferred));
        }
        ProviderStepOutcome move = await _port.MoveAsync(snapshot, _transfer.Destination, cancellationToken);
        _effects.RecordProvider(snapshot.Path, move.Receipt);
        if (move.Completion == ProviderStepCompletion.Cancelled)
        {
            return FileOperationOutcome.Cancelled(_effects.Snapshot(_notTransferred));
        }
        if (move.Failure is not null)
        {
            return FileOperationOutcome.Failed(_effects.Snapshot(_notTransferred), move.Failure);
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
