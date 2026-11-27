using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Retains one shown conflict and controlled provider evidence for resumed move admission.</summary>
internal sealed class TransferOverwriteGatewayFixture
{
    internal TransferOverwriteGatewayFixture()
    {
        TransferOverwriteEvidence original = TransferOverwriteEvidence.Create(Observation(@"C:\source.txt"), Observation(@"D:\destination\source.txt"));
        FileEntrySnapshot source = FileEntrySnapshot.Create(original.Source.Observation.Entry.Path, original.Source.Observation.Entry.Identity, DeletionCapability.NotRequested);
        Conflict = TransferConflict.Create(source, original.Target.Observation.Entry.Path,
            GatewayTestContext.ParsePath(@"D:\destination\source (2).txt"), original);
        ResolvedSource = source.WithConflictChoice(TransferConflictChoice.Create(Conflict, TransferConflictDecision.OverwriteContents));
    }

    internal TransferConflict Conflict { get; }
    internal FileEntrySnapshot ResolvedSource { get; }
    internal ScriptedFileOperationPort Port { get; } = ScriptedFileOperationPort.Create(null, null);
    internal FileSystemPath Destination { get; } = GatewayTestContext.ParsePath(@"D:\destination");

    internal VerifiedCopyReceipt Receipt(string kind)
    {
        TransferOverwriteEvidence original = Conflict.Target.OverwriteEvidence!;
        VerifiedEntrySnapshot source = original.Source.Observation.Entry;
        VerifiedEntrySnapshot target = original.Target.Observation.Entry;
        if (kind == "foreignSource")
        {
            source = VerifiedEntrySnapshot.Create(source.Path, source.Identity, FileObjectIdentity.Create("foreign-source"));
        }
        if (kind == "foreignTarget")
        {
            target = VerifiedEntrySnapshot.Create(target.Path, target.Identity, FileObjectIdentity.Create("foreign-target"));
        }
        VerifiedCopyEntry root = kind == "directory" ? VerifiedCopyEntry.Directory(source, target, [])
            : VerifiedCopyEntry.File(source, target, FileContentHash.Create(new string('A', 64)),
                kind == "defaultStream" ? FileVerificationRequirements.DefaultStream : FileVerificationRequirements.OverwriteContents);
        return VerifiedCopyReceipt.Create(ResolvedSource, root, VerifiedTreeBoundary.Create([], []));
    }

    internal async Task<FileOperationOutcome> MoveAsync(VerifiedCopyReceipt receipt)
    {
        Port.EnqueueInspection(FileInspectionOutcome.Succeeded(Conflict.Source));
        Port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([Conflict]));
        Port.EnqueuePreflight(TransferPreflightOutcome.Succeeded([TransferPlanEntry.Transfer(ResolvedSource, Conflict.ExistingTarget)]));
        Port.EnqueueAtomicMoveCapability(AtomicMoveCapabilityOutcome.Supported);
        Port.EnqueueAtomicMove(ProviderStepOutcome.Create(ProviderStepCompletion.Failed(FileOperationFailureKind.Move), ProviderStepReceipt.Empty));
        Port.EnqueueCopy(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Create([],
            ProviderTargetObservation.DesiredVerified(Conflict.Target.OverwriteEvidence!.Target.Observation, ReceiptObservationTestContext.Content(17)), [])));
        Port.EnqueueVerification(CopyVerificationOutcome.Succeeded(receipt));
        Port.EnqueueDeletion(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        MoveRequest request = Assert.IsInstanceOfType<MoveRequest>(Assert.IsInstanceOfType<FileOperationRequestAccepted>(
            MoveRequest.Create([Conflict.Source.Path], Destination)).Request);
        using FileOperationGateway gateway = new(Port);
        FileOperationOutcome paused = await gateway.ExecuteAsync(request, RecordingFileOperationProgress.Create(), CancellationToken.None);
        TransferContinuation continuation = Assert.IsInstanceOfType<TransferContinuation>(paused.Continuation);
        return await gateway.ResumeAsync(continuation, TransferConflictResponse.Create(TransferConflictDecision.OverwriteContents, TransferConflictScope.Current),
            RecordingFileOperationProgress.Create(), CancellationToken.None);
    }

    private static TransferOverwriteFileObservation Observation(string path)
    {
        return TransferOverwriteFileObservation.Create(ReceiptObservationTestContext.Entry(path, TransferPathObservation.CurrentPathVerified),
            DirectoryEntryKind.File, Assert.IsInstanceOfType<KnownEntrySize>(EntrySize.Create(17)),
            Assert.IsInstanceOfType<KnownEntryTimestamp>(EntryTimestamp.Create(new DateTimeOffset(2026, 11, 25, 0, 0, 0, TimeSpan.Zero))));
    }
}
