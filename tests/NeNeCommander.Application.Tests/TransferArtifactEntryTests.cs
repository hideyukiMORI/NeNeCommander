using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves incomplete metadata cannot erase a known artifact object or bypass receipt ownership checks.</summary>
[TestClass]
public sealed class TransferArtifactEntryTests
{
    /// <summary>Proves a known object with no full metadata remains identified without claiming verified bytes.</summary>
    [TestMethod]
    public void CreateWhenFullIdentityHasNotBeenObservedRetainsKnownObject()
    {
        TransferArtifactEntry entry = Entry("D:\\destination\\backup", "backup-object");
        TransferArtifactReceipt receipt = TransferArtifactReceipt.Identified(TransferArtifactRole.OriginalBackup,
            entry, ArtifactContentObservation.Unknown, ArtifactLinkObservation.Retained);

        IdentifiedTransferArtifact identified = Assert.IsInstanceOfType<IdentifiedTransferArtifact>(receipt);
        Assert.AreSame(entry, identified.Artifact);
        Assert.AreEqual("backup-object", identified.Artifact.ObjectIdentity.Value);
        Assert.AreSame(ArtifactFullIdentityObservation.NotObserved, identified.Artifact.FullIdentity);
        Assert.AreSame(TransferPathObservation.LastKnownPath, identified.Artifact.PathObservation);
        Assert.AreSame(ArtifactContentObservation.Unknown, identified.Content);
        Assert.IsFalse(receipt is UnidentifiedTransferArtifact);
    }

    /// <summary>Proves a previously known full identity remains representable when the path is now only last-known.</summary>
    [TestMethod]
    public void CreateWhenFullIdentityWasObservedRetainsItsLastValueIndependentlyOfPath()
    {
        TransferArtifactEntry original = ReceiptObservationTestContext.ArtifactEntry("D:\\destination\\backup", TransferPathObservation.CurrentPathVerified);
        TransferArtifactEntry later = TransferArtifactEntry.Create(original.Path, original.ObjectIdentity,
            original.FullIdentity, TransferPathObservation.LastKnownPath);
        TransferArtifactEntry clone = later with { };

        Assert.AreSame(original.ObjectIdentity, later.ObjectIdentity);
        Assert.AreSame(original.FullIdentity, later.FullIdentity);
        ObservedArtifactFullIdentity observed = Assert.IsInstanceOfType<ObservedArtifactFullIdentity>(later.FullIdentity);
        Assert.AreEqual("full:D:\\destination\\backup", observed.Identity.Value);
        Assert.AreSame(TransferPathObservation.LastKnownPath, later.PathObservation);
        Assert.AreEqual(later, clone);
        Assert.AreEqual(later.GetHashCode(), clone.GetHashCode());
    }

    /// <summary>Proves a missing full identity cannot hide stable aliases to the target or another artifact.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    [TestProperty("ThreatId", "ADV-006")]
    public void CreateWhenStableOnlyArtifactsAliasTargetOrEachOtherRefusesInventory()
    {
        ObservedTransferEntry targetEntry = ReceiptObservationTestContext.Entry("D:\\destination\\target", TransferPathObservation.LastKnownPath);
        ProviderTargetObservation target = ProviderTargetObservation.Untouched(targetEntry);
        TransferArtifactReceipt backup = Receipt(TransferArtifactRole.OriginalBackup, Entry("D:\\destination\\backup", "same-object"));
        TransferArtifactReceipt stage = Receipt(TransferArtifactRole.PreparedContent, Entry("D:\\destination\\stage", "same-object"));
        TransferArtifactReceipt alias = Receipt(TransferArtifactRole.OriginalBackup,
            Entry("D:\\destination\\target-alias", targetEntry.Entry.ObjectIdentity.Value));

        _ = Assert.ThrowsExactly<ArgumentException>(() => ProviderStepReceipt.Create([], target, [backup, stage]));
        _ = Assert.ThrowsExactly<ArgumentException>(() => ProviderStepReceipt.Create([], target, [alias]));
    }

    /// <summary>Proves path qualification remains enforced even before full metadata is known.</summary>
    [TestMethod]
    [DataRow("removed")]
    [DataRow("uncertain")]
    public void IdentifiedWhenStableOnlyCurrentPathContradictsCleanupRefusesIt(string state)
    {
        TransferArtifactEntry original = Entry("D:\\destination\\backup", "object");
        TransferArtifactEntry current = TransferArtifactEntry.Create(original.Path, original.ObjectIdentity,
            original.FullIdentity, TransferPathObservation.CurrentPathVerified);
        ArtifactLinkObservation link = state == "removed" ? ArtifactLinkObservation.Removed : ArtifactLinkObservation.Uncertain;

        _ = Assert.ThrowsExactly<ArgumentException>(() => TransferArtifactReceipt.Identified(TransferArtifactRole.OriginalBackup,
            current, ArtifactContentObservation.Unknown, link));
    }

    /// <summary>Proves roots and absent required facts cannot create an identified artifact.</summary>
    [TestMethod]
    public void CreateWhenRequiredFactsAreAbsentOrPathIsRootRejectsThem()
    {
        TransferArtifactEntry entry = Entry("D:\\destination\\backup", "object");
        WindowsLocalPath root = Assert.IsInstanceOfType<WindowsLocalPath>(GatewayTestContext.ParsePath("D:\\"));

        _ = Assert.ThrowsExactly<ArgumentException>(() => TransferArtifactEntry.Create(root, entry.ObjectIdentity, entry.FullIdentity, entry.PathObservation));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferArtifactEntry.Create(null!, entry.ObjectIdentity, entry.FullIdentity, entry.PathObservation));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferArtifactEntry.Create(entry.Path, null!, entry.FullIdentity, entry.PathObservation));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferArtifactEntry.Create(entry.Path, entry.ObjectIdentity, null!, entry.PathObservation));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferArtifactEntry.Create(entry.Path, entry.ObjectIdentity, entry.FullIdentity, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => ArtifactFullIdentityObservation.Observed(null!));
    }

    private static TransferArtifactEntry Entry(string path, string objectIdentity)
    {
        return TransferArtifactEntry.Create(Assert.IsInstanceOfType<WindowsLocalPath>(GatewayTestContext.ParsePath(path)),
            FileObjectIdentity.Create(objectIdentity), ArtifactFullIdentityObservation.NotObserved, TransferPathObservation.LastKnownPath);
    }

    private static TransferArtifactReceipt Receipt(TransferArtifactRole role, TransferArtifactEntry entry)
    {
        return TransferArtifactReceipt.Identified(role, entry, ArtifactContentObservation.Unknown, ArtifactLinkObservation.Retained);
    }
}
