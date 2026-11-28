using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves one receipt cannot make contradictory ownership or recovery-semantic claims.</summary>
[TestClass]
public sealed class ReceiptArtifactConsistencyTests
{
    /// <summary>Proves preparation objects are qualified by an admitted existing target and unique roles.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public void CreateWhenArtifactsHaveNoTargetOrRepeatRoleRejectsContradictoryInventory()
    {
        ProviderTargetObservation target = ProviderTargetObservation.Untouched(ReceiptObservationTestContext.Entry("D:\\destination\\source.txt", TransferPathObservation.CurrentPathVerified));
        TransferArtifactReceipt retained = ReceiptObservationTestContext.Artifact(TransferArtifactRole.OriginalBackup, ArtifactLinkObservation.Retained);
        TransferArtifactReceipt removed = ReceiptObservationTestContext.Artifact(TransferArtifactRole.OriginalBackup, ArtifactLinkObservation.Removed);

        _ = Assert.ThrowsExactly<ArgumentException>(() => ProviderStepReceipt.Create([], null, [retained]));
        _ = Assert.ThrowsExactly<ArgumentException>(() => ProviderStepReceipt.Create([], target, [retained, removed]));
    }

    /// <summary>Proves the target and different preparations cannot claim ownership of the same observed object.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    [TestProperty("ThreatId", "ADV-006")]
    public void CreateWhenPreparationObjectAliasesTargetOrAnotherArtifactRejectsOwnership()
    {
        ObservedTransferEntry targetEntry = ReceiptObservationTestContext.Entry("D:\\destination\\source.txt", TransferPathObservation.LastKnownPath);
        ProviderTargetObservation target = ProviderTargetObservation.Untouched(targetEntry);
        IdentifiedTransferArtifact backup = ReceiptObservationTestContext.Artifact(TransferArtifactRole.OriginalBackup, ArtifactLinkObservation.Retained);
        TransferArtifactReceipt duplicate = TransferArtifactReceipt.Identified(TransferArtifactRole.PreparedContent, backup.Artifact, ArtifactContentObservation.Partial, ArtifactLinkObservation.Retained);
        TransferArtifactReceipt targetAlias = TransferArtifactReceipt.Identified(TransferArtifactRole.OriginalBackup, ReceiptObservationTestContext.ArtifactEntry(targetEntry.Entry.Path.CanonicalText, targetEntry.PathObservation), ArtifactContentObservation.Unknown, ArtifactLinkObservation.Retained);

        _ = Assert.ThrowsExactly<ArgumentException>(() => ProviderStepReceipt.Create([], target, [backup, duplicate]));
        _ = Assert.ThrowsExactly<ArgumentException>(() => ProviderStepReceipt.Create([], target, [targetAlias]));
    }

    /// <summary>Proves full-file hashes cannot substitute for validated mapping frames or vice versa.</summary>
    [TestMethod]
    public void IdentifiedWhenContentEvidenceHasWrongRoleRefusesSemanticSubstitution()
    {
        TransferArtifactEntry entry = ReceiptObservationTestContext.ArtifactEntry("D:\\destination\\record", TransferPathObservation.LastKnownPath);
        ArtifactContentObservation bytes = ArtifactContentObservation.Verified(ReceiptObservationTestContext.Content(127));
        RecoveryFrameObservation prefix = ReceiptObservationTestContext.RecoveryFrame(127, RecoveryFlushObservation.Confirmed);
        ArtifactContentObservation mapping = ArtifactContentObservation.RecoveryMapping(prefix, RecoveryAppendObservation.OutcomeUnconfirmed);

        _ = Assert.ThrowsExactly<ArgumentException>(() => TransferArtifactReceipt.Identified(TransferArtifactRole.RecoveryRecord, entry, bytes, ArtifactLinkObservation.Retained));
        _ = Assert.ThrowsExactly<ArgumentException>(() => TransferArtifactReceipt.Identified(TransferArtifactRole.OriginalBackup, entry, mapping, ArtifactLinkObservation.Retained));
        _ = Assert.ThrowsExactly<ArgumentException>(() => TransferArtifactReceipt.Identified(TransferArtifactRole.PreparedContent, entry, mapping, ArtifactLinkObservation.Retained));

        IdentifiedTransferArtifact receipt = Assert.IsInstanceOfType<IdentifiedTransferArtifact>(TransferArtifactReceipt.Identified(TransferArtifactRole.RecoveryRecord, entry, mapping, ArtifactLinkObservation.Retained));
        Assert.AreSame(mapping, receipt.Content);
        Assert.AreSame(entry, receipt.Artifact);
    }
}
