using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves preparation evidence cannot turn an unconfirmed name into verified content or a current owned path.</summary>
[TestClass]
public sealed class TransferArtifactReceiptTests
{
    /// <summary>Proves an unidentified creation retains only its role and attempted name.</summary>
    [TestMethod]
    public void UnidentifiedWhenObjectQueryFailsRetainsCandidateWithoutOwnershipClaim()
    {
        FileSystemPath candidate = GatewayTestContext.ParsePath("D:\\destination\\candidate");

        TransferArtifactReceipt receipt = TransferArtifactReceipt.Unidentified(TransferArtifactRole.RecoveryRecord, candidate);

        UnidentifiedTransferArtifact unidentified = Assert.IsInstanceOfType<UnidentifiedTransferArtifact>(receipt);
        Assert.AreSame(TransferArtifactRole.RecoveryRecord, unidentified.Role);
        Assert.AreSame(candidate, unidentified.CandidatePath);
        Assert.IsFalse(receipt is IdentifiedTransferArtifact);
    }

    /// <summary>Proves a removed link keeps its verified object/content while its location remains only last-known.</summary>
    [TestMethod]
    public void IdentifiedWhenCleanupRemovesOwnedLinkKeepsObjectAndContentEvidence()
    {
        IdentifiedTransferArtifact removed = ReceiptObservationTestContext.Artifact(TransferArtifactRole.OriginalBackup, ArtifactLinkObservation.Removed);

        TransferArtifactReceipt clone = removed with { };

        Assert.AreSame(TransferArtifactRole.OriginalBackup, removed.Role);
        Assert.AreSame(TransferPathObservation.LastKnownPath, removed.Artifact.PathObservation);
        Assert.AreSame(ArtifactLinkObservation.Removed, removed.Link);
        Assert.AreEqual(0, Assert.IsInstanceOfType<VerifiedArtifactContent>(removed.Content).Content.Size.Bytes);
        Assert.AreEqual(removed, clone);
        Assert.AreEqual(removed.GetHashCode(), clone.GetHashCode());
    }

    /// <summary>Proves incomplete content is never silently promoted to a verified backup.</summary>
    [TestMethod]
    public void IdentifiedWhenPreparationIsPartialOrUnknownKeepsThatDistinctContentState()
    {
        TransferArtifactEntry entry = ReceiptObservationTestContext.ArtifactEntry("D:\\destination\\stage", TransferPathObservation.CurrentPathVerified);

        IdentifiedTransferArtifact partial = Assert.IsInstanceOfType<IdentifiedTransferArtifact>(TransferArtifactReceipt.Identified(
            TransferArtifactRole.PreparedContent, entry, ArtifactContentObservation.Partial, ArtifactLinkObservation.Retained));
        IdentifiedTransferArtifact unknown = Assert.IsInstanceOfType<IdentifiedTransferArtifact>(TransferArtifactReceipt.Identified(
            TransferArtifactRole.RecoveryRecord, entry, ArtifactContentObservation.Unknown, ArtifactLinkObservation.Retained));

        Assert.AreSame(ArtifactContentObservation.Partial, partial.Content);
        Assert.AreSame(ArtifactContentObservation.Unknown, unknown.Content);
        Assert.AreSame(entry, partial.Artifact);
        Assert.AreSame(ArtifactLinkObservation.Retained, partial.Link);
        Assert.IsFalse(partial.Content is VerifiedArtifactContent);
        Assert.IsFalse(unknown.Content is VerifiedArtifactContent);
    }

    /// <summary>Proves removed or uncertain link observations cannot claim a current object path.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public void IdentifiedWhenCurrentPathContradictsCleanupRefusesTheClaim()
    {
        TransferArtifactEntry current = ReceiptObservationTestContext.ArtifactEntry("D:\\destination\\backup", TransferPathObservation.CurrentPathVerified);

        _ = Assert.ThrowsExactly<ArgumentException>(() => TransferArtifactReceipt.Identified(
            TransferArtifactRole.OriginalBackup, current, ArtifactContentObservation.Unknown, ArtifactLinkObservation.Removed));
        _ = Assert.ThrowsExactly<ArgumentException>(() => TransferArtifactReceipt.Identified(
            TransferArtifactRole.OriginalBackup, current, ArtifactContentObservation.Partial, ArtifactLinkObservation.Uncertain));

        IdentifiedTransferArtifact uncertain = ReceiptObservationTestContext.Artifact(TransferArtifactRole.OriginalBackup, ArtifactLinkObservation.Uncertain);
        Assert.AreSame(ArtifactLinkObservation.Uncertain, uncertain.Link);
        Assert.AreSame(TransferPathObservation.LastKnownPath, uncertain.Artifact.PathObservation);
    }

    /// <summary>Proves an absent observation cannot stand for an unknown artifact fact.</summary>
    [TestMethod]
    public void FactoriesWhenRequiredFieldsAreAbsentRejectInsteadOfInferringUnknown()
    {
        TransferArtifactEntry entry = ReceiptObservationTestContext.ArtifactEntry("D:\\destination\\backup", TransferPathObservation.LastKnownPath);
        FileSystemPath candidate = entry.Path;

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferArtifactReceipt.Unidentified(null!, candidate));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferArtifactReceipt.Unidentified(TransferArtifactRole.OriginalBackup, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferArtifactReceipt.Identified(null!, entry, ArtifactContentObservation.Unknown, ArtifactLinkObservation.Retained));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferArtifactReceipt.Identified(TransferArtifactRole.OriginalBackup, null!, ArtifactContentObservation.Unknown, ArtifactLinkObservation.Retained));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferArtifactReceipt.Identified(TransferArtifactRole.OriginalBackup, entry, null!, ArtifactLinkObservation.Retained));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferArtifactReceipt.Identified(TransferArtifactRole.OriginalBackup, entry, ArtifactContentObservation.Unknown, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => ArtifactContentObservation.Verified(null!));
    }
}
