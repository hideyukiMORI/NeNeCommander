using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves target uncertainty and retained preparations survive the canonical outcome without legacy effect counts.</summary>
[TestClass]
public sealed class ProviderReceiptObservationTests
{
    /// <summary>Proves any preparation or possible target mutation makes an expected failure partial.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public void FailedWhenOnlyTargetOrArtifactFactsExistNeverReportsNoEffectRejection()
    {
        ObservedTransferEntry target = ReceiptObservationTestContext.Entry("D:\\destination\\source.txt", TransferPathObservation.LastKnownPath);
        FileContentEvidence content = ReceiptObservationTestContext.Content(0);
        ProviderStepReceipt[] receipts = [
            ProviderStepReceipt.Create([], ProviderTargetObservation.Untouched(target), [TransferArtifactReceipt.Unidentified(TransferArtifactRole.RecoveryRecord, target.Entry.Path)]),
            ProviderStepReceipt.Create([], ProviderTargetObservation.Untouched(target), [ReceiptObservationTestContext.Artifact(TransferArtifactRole.OriginalBackup, ArtifactLinkObservation.Removed)]),
            ProviderStepReceipt.Create([], ProviderTargetObservation.MayHaveChanged(target), []),
            ProviderStepReceipt.Create([], ProviderTargetObservation.OriginalVerified(target, content), []),
            ProviderStepReceipt.Create([], ProviderTargetObservation.DesiredVerified(target, content), [])];

        foreach (ProviderStepReceipt receipt in receipts)
        {
            FileOperationEffectSummary summary = FileOperationEffectSummary.Create([], [], [OperationProviderStep.Create(target.Entry.Path, receipt)]);
            FileOperationOutcome outcome = FileOperationOutcome.Failed(summary, FileOperationFailureKind.Conflict);
            Assert.AreSame(FileOperationCompletionKind.PartiallyCompleted, outcome.Completion);
            Assert.IsEmpty(outcome.Effects);
            Assert.AreSame(receipt, outcome.ProviderSteps[0].Receipt);
            Assert.IsNull(outcome.Conflicts);
            Assert.IsNull(outcome.Continuation);
        }
    }

    /// <summary>Proves a genuinely untouched target can be rejected while retaining its observation.</summary>
    [TestMethod]
    public void FailedWhenTargetIsUntouchedWithoutPreparationsKeepsNoEffectClassification()
    {
        ObservedTransferEntry target = ReceiptObservationTestContext.Entry("D:\\destination\\source.txt", TransferPathObservation.CurrentPathVerified);
        ProviderStepReceipt receipt = ProviderStepReceipt.Create([], ProviderTargetObservation.Untouched(target), []);

        FileOperationOutcome outcome = FileOperationOutcome.Failed(FileOperationEffectSummary.Create([], [], [OperationProviderStep.Create(target.Entry.Path, receipt)]), FileOperationFailureKind.IdentityChanged);

        Assert.AreSame(FileOperationCompletionKind.Rejected, outcome.Completion);
        Assert.AreSame(receipt, outcome.ProviderSteps[0].Receipt);
    }

    /// <summary>Proves success and cancellation keep artifact cleanup facts independently of content verification.</summary>
    [TestMethod]
    public void CompletedWhenArtifactsRemainOrWereRemovedPreservesTheirExactStates()
    {
        ObservedTransferEntry target = ReceiptObservationTestContext.Entry("D:\\destination\\source.txt", TransferPathObservation.LastKnownPath);
        ProviderTargetObservation desired = ProviderTargetObservation.DesiredVerified(target, ReceiptObservationTestContext.Content(0));
        ProviderStepReceipt receipt = ProviderStepReceipt.Create([], desired, [ReceiptObservationTestContext.Artifact(TransferArtifactRole.OriginalBackup, ArtifactLinkObservation.Retained), ReceiptObservationTestContext.Artifact(TransferArtifactRole.PreparedContent, ArtifactLinkObservation.Removed)]);
        FileOperationEffectSummary summary = FileOperationEffectSummary.Create([], [], [OperationProviderStep.Create(target.Entry.Path, receipt)]);

        FileOperationOutcome succeeded = FileOperationOutcome.Succeeded(summary);
        FileOperationOutcome cancelled = FileOperationOutcome.Cancelled(summary);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, succeeded.Completion);
        Assert.AreSame(FileOperationCompletionKind.Cancelled, cancelled.Completion);
        Assert.AreSame(receipt, succeeded.ProviderSteps[0].Receipt);
        Assert.AreSame(receipt, cancelled.ProviderSteps[0].Receipt);
        Assert.AreSame(desired, receipt.Target);
        Assert.AreSame(ArtifactLinkObservation.Retained, ((IdentifiedTransferArtifact)receipt.Artifacts[0]).Link);
        Assert.AreSame(ArtifactLinkObservation.Removed, ((IdentifiedTransferArtifact)receipt.Artifacts[1]).Link);
    }

    /// <summary>Proves artifact inputs are owned once before validation and cannot be erased through SyncRoot.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public void CreateWhenArtifactInputChangesKeepsTheValidatedSnapshot()
    {
        TransferArtifactReceipt artifact = ReceiptObservationTestContext.Artifact(TransferArtifactRole.OriginalBackup, ArtifactLinkObservation.Retained);
        ChangingReceiptCollection<TransferArtifactReceipt> input = new(artifact, null!);

        ObservedTransferEntry target = ReceiptObservationTestContext.Entry("D:\\destination\\source.txt", TransferPathObservation.LastKnownPath);
        ProviderStepReceipt receipt = ProviderStepReceipt.Create([], ProviderTargetObservation.Untouched(target), input);
        ICollection collection = Assert.IsInstanceOfType<ICollection>(receipt.Artifacts);
        ICollection<TransferArtifactReceipt> exposed = Assert.IsInstanceOfType<ICollection<TransferArtifactReceipt>>(collection.SyncRoot);
        _ = Assert.ThrowsExactly<NotSupportedException>(exposed.Clear);

        Assert.HasCount(1, receipt.Artifacts);
        Assert.AreSame(artifact, receipt.Artifacts[0]);
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => ProviderStepReceipt.Create([], null, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => ProviderStepReceipt.Create([], null, [null!]));
    }
}
