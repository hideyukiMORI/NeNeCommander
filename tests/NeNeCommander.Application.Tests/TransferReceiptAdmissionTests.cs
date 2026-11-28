using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves transfer receipt boundaries reject missing or contradictory evidence before consumers observe it.</summary>
[TestClass]
public sealed class TransferReceiptAdmissionTests
{
    /// <summary>Absent collections and source-qualified receipts report their own missing component.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public void CreateWhenProviderEvidenceIsAbsentRejectsAtItsOwningBoundary()
    {
        FileSystemPath source = GatewayTestContext.ParsePath(@"C:\source.txt");

        TransferConflictTestContext.AssertNullParameter(() => ProviderStepReceipt.Create(null!, null, []), "effects");
        TransferConflictTestContext.AssertNullParameter(() => ProviderStepReceipt.Create([], null, null!), "artifacts");
        TransferConflictTestContext.AssertNullParameter(() => ProviderStepReceipt.Create([null!], null, []), "effect");
        TransferConflictTestContext.AssertNullParameter(() => ProviderStepReceipt.Create([], null, [null!]), "artifact");
        TransferConflictTestContext.AssertNullParameter(() => OperationProviderStep.Create(null!, ProviderStepReceipt.Empty), "source");
        TransferConflictTestContext.AssertNullParameter(() => OperationProviderStep.Create(source, null!), "receipt");
    }

    /// <summary>Null members cannot masquerade as completed effects, explicit skips or provider observations.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public void CreateWhenSummaryContainsAbsentEntriesRejectsBeforePublishingOutcome()
    {
        TransferConflictTestContext.AssertNullParameter(() => FileOperationEffectSummary.Create([null!], [], []), "effect");
        TransferConflictTestContext.AssertNullParameter(() => FileOperationEffectSummary.Create([], [null!], []), "source");
        TransferConflictTestContext.AssertNullParameter(() => FileOperationEffectSummary.Create([], [], [null!]), "step");
        TransferConflictTestContext.AssertNullParameter(() => FileOperationOutcome.Failed(null!, FileOperationFailureKind.Copy), "summary");
        TransferConflictTestContext.AssertNullParameter(() => FileOperationOutcome.Failed(FileOperationEffectSummary.Empty, null!), "failure");
        TransferConflictTestContext.AssertNullParameter(() => FileOperationOutcome.Cancelled(null!), "summary");
        TransferConflictTestContext.AssertNullParameter(() => FileOperationOutcome.Succeeded(null!), "summary");
    }

    /// <summary>Distinct preparation objects still cannot claim the same role in one operation receipt.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public void CreateWhenDistinctArtifactsRepeatRoleRejectsRoleAmbiguity()
    {
        ProviderTargetObservation target = ProviderTargetObservation.Untouched(
            ReceiptObservationTestContext.Entry(@"D:\destination\target.txt", TransferPathObservation.CurrentPathVerified));
        TransferArtifactReceipt first = TransferArtifactReceipt.Identified(TransferArtifactRole.OriginalBackup,
            ReceiptObservationTestContext.ArtifactEntry(@"D:\destination\first.bin", TransferPathObservation.LastKnownPath),
            ArtifactContentObservation.Unknown, ArtifactLinkObservation.Retained);
        TransferArtifactReceipt second = TransferArtifactReceipt.Identified(TransferArtifactRole.OriginalBackup,
            ReceiptObservationTestContext.ArtifactEntry(@"D:\destination\second.bin", TransferPathObservation.LastKnownPath),
            ArtifactContentObservation.Partial, ArtifactLinkObservation.Retained);

        Assert.AreNotEqual(Assert.IsInstanceOfType<IdentifiedTransferArtifact>(first).Artifact.ObjectIdentity,
            Assert.IsInstanceOfType<IdentifiedTransferArtifact>(second).Artifact.ObjectIdentity);
        ArgumentException failure = Assert.ThrowsExactly<ArgumentException>(() => ProviderStepReceipt.Create([], target, [first, second]));
        Assert.AreEqual("artifacts", failure.ParamName);
    }

    /// <summary>A missing source list or snapshot is invalid rather than an empty transfer request.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-006")]
    public void CreateWhenFrozenSourcesAreAbsentRejectsWithoutInventingAnEmptyBatch()
    {
        FileSystemPath source = GatewayTestContext.ParsePath(@"C:\source.txt");
        FileSystemPath destination = GatewayTestContext.ParsePath(@"D:\destination");
        CopyRequest request = TransferConflictTestContext.CreateCopy([source], destination);

        TransferConflictTestContext.AssertNullParameter(() => CopyRequest.Create(null!, destination), "sources");
        TransferConflictTestContext.AssertNullParameter(() => MoveRequest.Create(null!, destination), "sources");
        TransferConflictTestContext.AssertNullParameter(() => FrozenTransfer.Create(request, [null!], destination), "source");
        TransferConflictTestContext.AssertNullParameter(() => AtomicMoveCapabilityOutcome.Failed(null!), "failure");
    }

    /// <summary>Missing original targets and decisions cannot enter a resumed conflict plan.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-006")]
    public void CreateWhenConflictFactsAreAbsentRejectsTheMissingOriginalFact()
    {
        TransferOverwriteGatewayFixture fixture = new();
        TransferConflict conflict = fixture.Conflict;
        ConflictSet set = Assert.IsInstanceOfType<ConflictSet>(TransferPreflightOutcome.Conflicted([conflict]));

        TransferConflictTestContext.AssertNullParameter(() => TransferConflict.Create(conflict.Source, null!, conflict.KeepBothCandidate, conflict.Target.Overwrite), "existingTarget");
        TransferConflictTestContext.AssertNullParameter(() => TransferConflict.Create(conflict.Source, conflict.ExistingTarget, conflict.KeepBothCandidate, null!), "overwrite");
        TransferConflictTestContext.AssertNullParameter(() => TransferPreflightOutcome.Conflicted(null!), "conflicts");
        TransferConflictTestContext.AssertNullParameter(() => TransferResolution.None.Add(set, null!, TransferConflictScope.Current), "decision");
    }

    /// <summary>An unavailable current overwrite must fail rather than silently discard the requested choice.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-006")]
    public void AddWhenCurrentConflictDoesNotAllowOverwriteRejectsInsteadOfFilteringItAway()
    {
        TransferConflict conflict = TransferConflictTestContext.Conflict(GatewayTestContext.ParsePath(@"C:\source.txt"),
            GatewayTestContext.ParsePath(@"D:\destination"), "source (2).txt");
        ConflictSet set = Assert.IsInstanceOfType<ConflictSet>(TransferPreflightOutcome.Conflicted([conflict]));

        ArgumentException failure = Assert.ThrowsExactly<ArgumentException>(() => TransferResolution.None.Add(
            set, TransferConflictDecision.OverwriteContents, TransferConflictScope.Current));

        Assert.AreEqual("decision", failure.ParamName);
        Assert.IsEmpty(TransferResolution.None.Choices);
    }

    /// <summary>A known mapping still requires its explicit role before any role-specific interpretation.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public void IdentifiedWhenMappingRoleIsAbsentRejectsTheMissingRole()
    {
        TransferArtifactEntry entry = ReceiptObservationTestContext.ArtifactEntry(@"D:\destination\record", TransferPathObservation.LastKnownPath);
        ArtifactContentObservation mapping = ArtifactContentObservation.RecoveryMapping(
            ReceiptObservationTestContext.RecoveryFrame(127, RecoveryFlushObservation.Confirmed), RecoveryAppendObservation.OutcomeUnconfirmed);

        TransferConflictTestContext.AssertNullParameter(() => TransferArtifactReceipt.Identified(
            null!, entry, mapping, ArtifactLinkObservation.Retained), "role");
    }

    /// <summary>Missing descendant or ancestor collections cannot stand for verified empty evidence.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    public void CreateWhenVerificationCollectionsAreAbsentRejectsBeforeSourceDeletionEvidence()
    {
        VerifiedEntrySnapshot source = ReceiptObservationTestContext.Entry(@"C:\source", TransferPathObservation.CurrentPathVerified).Entry;
        VerifiedEntrySnapshot target = ReceiptObservationTestContext.Entry(@"D:\destination\source", TransferPathObservation.CurrentPathVerified).Entry;

        TransferConflictTestContext.AssertNullParameter(() => VerifiedCopyEntry.Directory(source, target, null!), "children");
        TransferConflictTestContext.AssertNullParameter(() => VerifiedTreeBoundary.Create(null!, []), "source");
        TransferConflictTestContext.AssertNullParameter(() => VerifiedTreeBoundary.Create([], null!), "target");
    }
}
