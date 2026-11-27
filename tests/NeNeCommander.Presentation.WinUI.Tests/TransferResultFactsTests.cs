using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Panes;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves partial results show exact content knowledge, path qualification and cleanup uncertainty.</summary>
[TestClass]
public sealed class TransferResultFactsTests
{
    /// <summary>All four target states retain current versus last-known path meaning without hiding a copy failure.</summary>
    [TestMethod]
    [DataRow("untouched", "current")]
    [DataRow("untouched", "last")]
    [DataRow("original", "current")]
    [DataRow("original", "last")]
    [DataRow("uncertain", "current")]
    [DataRow("uncertain", "last")]
    [DataRow("desired", "current")]
    [DataRow("desired", "last")]
    public async Task ComposeAsyncWhenTargetKnowledgeVariesRetainsItsExactMeaning(string kind, string pathKind)
    {
        TransferPathObservation path = pathKind == "current" ? TransferPathObservation.CurrentPathVerified : TransferPathObservation.LastKnownPath;
        ProviderStepReceipt receipt = ProviderStepReceipt.Create([], TransferResultFixture.Target(kind, path), []);
        TransferResultDetail detail = await TransferResultFixture.RunAsync(OperationKind.Copy,
            ProviderStepOutcome.Create(ProviderStepCompletion.Failed(FileOperationFailureKind.Copy), receipt), Success());

        IReadOnlyList<string> rows = TransferResultFacts.Compose(detail, TransferResultFixture.Localize);

        Assert.HasCount(3, rows);
        Assert.AreEqual("TransferFailureCopy", rows[0]);
        Assert.AreEqual(kind + "|" + pathKind + ":D:\\right\\source", rows[1]);
        Assert.AreEqual("source-kept|C:\\left\\source", rows[2]);
        Assert.AreEqual(0, detail.Copied);
        Assert.AreEqual(0, detail.SourceDeleted);
    }

    /// <summary>Verified destination contents never conceal partial backup data, uncertain removal or a failed copy stage.</summary>
    [TestMethod]
    public async Task ComposeAsyncWhenCleanupIsPartialShowsEveryIndependentObservation()
    {
        TransferArtifactReceipt original = TransferResultFixture.Artifact(TransferArtifactRole.OriginalBackup,
            ArtifactContentObservation.Verified(TransferResultFixture.Content()), ArtifactLinkObservation.Retained, TransferPathObservation.LastKnownPath);
        TransferArtifactReceipt prepared = TransferResultFixture.Artifact(TransferArtifactRole.PreparedContent,
            ArtifactContentObservation.Partial, ArtifactLinkObservation.Uncertain, TransferPathObservation.LastKnownPath);
        FileContentEvidence content = TransferResultFixture.Content();
        RecoveryFrameObservation frame = RecoveryFrameObservation.Create(RecoveryFrameEvidence.Create(content.Size, content.Hash), RecoveryFlushObservation.Confirmed);
        TransferArtifactReceipt recovery = TransferResultFixture.Artifact(TransferArtifactRole.RecoveryRecord,
            ArtifactContentObservation.RecoveryMapping(frame, RecoveryAppendObservation.OutcomeUnconfirmed), ArtifactLinkObservation.Removed, TransferPathObservation.LastKnownPath);
        ProviderStepReceipt receipt = ProviderStepReceipt.Create([], TransferResultFixture.Target("desired", TransferPathObservation.LastKnownPath), [original, prepared, recovery]);
        TransferResultDetail detail = await TransferResultFixture.RunAsync(OperationKind.Move,
            ProviderStepOutcome.Create(ProviderStepCompletion.Failed(FileOperationFailureKind.Copy), receipt), Success());

        IReadOnlyList<string> rows = TransferResultFacts.Compose(detail, TransferResultFixture.Localize);

        Assert.HasCount(7, rows);
        Assert.AreEqual("TransferFailureCopy", rows[0]);
        Assert.AreEqual("desired|last:D:\\right\\source", rows[1]);
        Assert.AreEqual("source-kept|C:\\left\\source", rows[2]);
        Assert.AreEqual("TransferArtifactOriginalTitle|TransferArtifactContentsVerified|TransferArtifactKept|last:D:\\right\\original", rows[3]);
        Assert.AreEqual("TransferArtifactPreparedTitle|TransferArtifactContentsPartial|TransferArtifactRemovalUnconfirmed|last:D:\\right\\prepared", rows[4]);
        Assert.AreEqual("TransferArtifactRecoveryTitle|TransferArtifactMappingVerified|TransferArtifactRemoved|last:D:\\right\\recovery", rows[5]);
        Assert.AreEqual("TransferRecoveryInspectionNotice", rows[6]);
        Assert.AreSame(FileOperationCompletionKind.PartiallyCompleted, detail.Outcome.Completion);
    }

    /// <summary>An unidentified creation reports only an attempted path, never a current or last-known owned object.</summary>
    [TestMethod]
    public async Task ComposeAsyncWhenArtifactIdentityIsUnknownShowsOnlyCandidateKnowledge()
    {
        TransferArtifactReceipt artifact = TransferArtifactReceipt.Unidentified(TransferArtifactRole.OriginalBackup,
            DualPaneTestContext.ParsePath(@"D:\right\candidate"));
        ProviderStepReceipt receipt = ProviderStepReceipt.Create([], TransferResultFixture.Target("untouched", TransferPathObservation.LastKnownPath), [artifact]);
        TransferResultDetail detail = await TransferResultFixture.RunAsync(OperationKind.Copy,
            ProviderStepOutcome.Create(ProviderStepCompletion.Cancelled, receipt), Success());

        IReadOnlyList<string> rows = TransferResultFacts.Compose(detail, TransferResultFixture.Localize);

        Assert.HasCount(4, rows);
        Assert.AreEqual("untouched|last:D:\\right\\source", rows[0]);
        Assert.AreEqual("TransferArtifactOriginalTitle|unidentified|candidate:D:\\right\\candidate", rows[2]);
        Assert.AreEqual("TransferRecoveryInspectionNotice", rows[3]);
        Assert.AreSame(FileOperationCompletionKind.Cancelled, detail.Outcome.Completion);
    }

    /// <summary>Already removed artifacts do not imply a retained backup or offer recovery advice about them.</summary>
    [TestMethod]
    public async Task ComposeAsyncWhenArtifactWasRemovedRetainsRemovalWithoutRecoveryPromise()
    {
        TransferArtifactReceipt artifact = TransferResultFixture.Artifact(TransferArtifactRole.OriginalBackup,
            ArtifactContentObservation.Unknown, ArtifactLinkObservation.Removed, TransferPathObservation.LastKnownPath);
        ProviderStepReceipt receipt = ProviderStepReceipt.Create([], TransferResultFixture.Target("desired", TransferPathObservation.LastKnownPath), [artifact]);
        TransferResultDetail detail = await TransferResultFixture.RunAsync(OperationKind.Copy,
            ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, receipt), Success());

        IReadOnlyList<string> rows = TransferResultFacts.Compose(detail, TransferResultFixture.Localize);

        Assert.HasCount(3, rows);
        Assert.AreEqual("TransferArtifactOriginalTitle|TransferArtifactContentsUnverified|TransferArtifactRemoved|last:D:\\right\\original", rows[2]);
        Assert.AreSame(FileOperationCompletionKind.Succeeded, detail.Outcome.Completion);
    }

    /// <summary>Ordinary transfers stay outside overwrite disclosure and invalid presentation inputs are rejected.</summary>
    [TestMethod]
    public async Task ComposeAsyncWhenNoOverwriteEvidenceExistsProducesNoExtraRows()
    {
        TransferResultDetail detail = await TransferResultFixture.RunAsync(OperationKind.Copy, Success(), Success());

        Assert.IsEmpty(TransferResultFacts.Compose(detail, TransferResultFixture.Localize));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferResultFacts.Compose(null!, TransferResultFixture.Localize));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferResultFacts.Compose(detail, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new TransferResultDetail(null!, detail.Outcome));
        _ = Assert.ThrowsExactly<ArgumentException>(() => new TransferResultDetail(OperationKind.Delete, detail.Outcome));
    }

    private static ProviderStepOutcome Success()
    {
        return ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty);
    }
}
