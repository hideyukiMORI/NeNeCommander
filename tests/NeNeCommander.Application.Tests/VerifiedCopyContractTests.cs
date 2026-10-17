using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves immutable verification evidence and its gateway deletion contract.</summary>
[TestClass]
public sealed class VerifiedCopyContractTests
{
    /// <summary>Proves content evidence is fixed-size canonical SHA-256 text.</summary>
    [TestMethod]
    [DataRow("")]
    [DataRow("00")]
    [DataRow("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [DataRow("GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG")]
    [DataRow("000000000000000000000000000000000000000000000000000000000000000G")]
    public void CreateWhenHashIsNotCanonicalRejects(string value)
    {
        _ = Assert.ThrowsExactly<ArgumentException>(() => FileContentHash.Create(value));
    }

    /// <summary>Proves opaque object identities are nonempty and bounded.</summary>
    [TestMethod]
    public void CreateWhenObjectIdentityIsEmptyOrTooLongRejects()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => FileObjectIdentity.Create(null!));
        _ = Assert.ThrowsExactly<ArgumentException>(() => FileObjectIdentity.Create(" "));
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => FileObjectIdentity.Create(new string('a', 513)));
        Assert.AreEqual(new string('a', 512), FileObjectIdentity.Create(new string('a', 512)).Value);
    }

    /// <summary>Proves evidence never borrows mutable child or ancestor lists.</summary>
    [TestMethod]
    public void CreateWhenCollectionsChangeKeepsOwnedImmutableEvidence()
    {
        FileEntrySnapshot source = Source();
        VerifiedEntrySnapshot observation = Observation(source.Path);
        VerifiedCopyEntry file = VerifiedCopyEntry.File(observation, Observation(Target()), Hash());
        List<VerifiedCopyEntry> children = [file];
        List<VerifiedEntrySnapshot> ancestors = [observation];
        VerifiedCopyEntry.VerifiedDirectory directory = Assert.IsInstanceOfType<VerifiedCopyEntry.VerifiedDirectory>(
            VerifiedCopyEntry.Directory(observation, Observation(Target()), children));
        VerifiedTreeBoundary boundary = VerifiedTreeBoundary.Create(ancestors, ancestors);

        children.Clear();
        ancestors.Clear();

        Assert.HasCount(1, directory.Children);
        Assert.HasCount(1, boundary.SourceAncestors);
        Assert.HasCount(1, boundary.TargetAncestors);
        Assert.AreEqual(new string('0', 64), Assert.IsInstanceOfType<VerifiedCopyEntry.VerifiedFile>(file).Content.Value);
        Assert.AreEqual("identity", observation.ObjectIdentity.Value);
    }

    /// <summary>Proves every absent required piece is rejected before evidence can be published.</summary>
    [TestMethod]
    public void CreateWhenRequiredEvidenceIsNullRejects()
    {
        FileEntrySnapshot source = Source();
        VerifiedEntrySnapshot observation = Observation(source.Path);
        VerifiedCopyEntry file = VerifiedCopyEntry.File(observation, Observation(Target()), Hash());
        VerifiedTreeBoundary boundary = VerifiedTreeBoundary.Create([], []);
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => FileContentHash.Create(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => VerifiedEntrySnapshot.Create(null!, source.Identity, observation.ObjectIdentity));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => VerifiedEntrySnapshot.Create(source.Path, null!, observation.ObjectIdentity));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => VerifiedEntrySnapshot.Create(source.Path, source.Identity, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => VerifiedCopyEntry.File(null!, observation, Hash()));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => VerifiedCopyEntry.File(observation, null!, Hash()));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => VerifiedCopyEntry.File(observation, observation, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => VerifiedCopyEntry.Directory(observation, observation, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => VerifiedCopyEntry.Directory(observation, observation, [null!]));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => VerifiedTreeBoundary.Create(null!, []));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => VerifiedTreeBoundary.Create([], null!));
        _ = Assert.ThrowsExactly<ArgumentException>(() => VerifiedTreeBoundary.Create([null!], []));
        _ = Assert.ThrowsExactly<ArgumentException>(() => VerifiedTreeBoundary.Create([], [null!]));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => VerifiedCopyReceipt.Create(null!, file, boundary));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => VerifiedCopyReceipt.Create(source, null!, boundary));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => VerifiedCopyReceipt.Create(source, file, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => CopyVerificationOutcome.Succeeded(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => CopyVerificationOutcome.Failed(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => DeletionExecutionMode.VerifiedCopy(null!));
    }

    /// <summary>Proves receipt roots cannot silently change source, identity or the preflighted target.</summary>
    [TestMethod]
    [DataRow("path")]
    [DataRow("identity")]
    [DataRow("target")]
    public void CreateWhenRootBindingDiffersRejects(string mismatch)
    {
        FileEntrySnapshot source = Source().WithTransferTarget(Target());
        VerifiedEntrySnapshot rootSource = mismatch == "path" ? Observation(Path("C:\\other")) :
            mismatch == "identity" ? VerifiedEntrySnapshot.Create(source.Path, Identity("other"), FileObjectIdentity.Create("identity")) :
            Observation(source.Path);
        VerifiedCopyEntry root = VerifiedCopyEntry.File(rootSource,
            Observation(mismatch == "target" ? Path("C:\\elsewhere\\item") : Target()), Hash());

        _ = Assert.ThrowsExactly<ArgumentException>(() => VerifiedCopyReceipt.Create(source, root, VerifiedTreeBoundary.Create([], [])));
    }

    /// <summary>Proves the gateway carries verification evidence in a distinct deletion instruction and retains partial effects.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    public async Task ExecuteAsyncWhenVerifiedDeletionPartlyFailsReportsOnlyPartialSourceRemoval()
    {
        ScriptedFileOperationPort port = Port();
        port.EnqueueVerification(ProviderStepOutcome.Succeeded());
        port.EnqueueDeletion(ProviderStepOutcome.FailedAfterEffect(FileOperationFailureKind.Delete, ProviderStepEffectKind.SourcePartiallyDeleted));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(Move(), RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.PartiallyCompleted, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.Delete, outcome.Failure);
        Assert.AreSame(FileOperationEffectKind.SourcePartiallyDeleted, outcome.Effects[^1].Kind);
        Assert.IsFalse(outcome.Effects.Any(effect => effect.Kind == FileOperationEffectKind.SourceDeleted));
        VerifiedCopyReceipt receipt = Assert.IsInstanceOfType<DeletionExecutionMode.VerifiedCopyMode>(port.DeletionModes[0]).Receipt;
        Assert.AreEqual(Target(), receipt.Root.Target.Path);
        Assert.AreEqual(Source().Path, receipt.Root.Source.Path);
    }

    /// <summary>Proves a provider's receipt for another source, target or identity never reaches deletion.</summary>
    [TestMethod]
    [DataRow("source")]
    [DataRow("target")]
    [DataRow("identity")]
    public async Task ExecuteAsyncWhenVerificationReceiptBelongsToDifferentTransferRejectsBeforeDelete(string mismatch)
    {
        ScriptedFileOperationPort port = Port();
        FileEntrySnapshot source = mismatch == "source" ? FileEntrySnapshot.Create(Path("C:\\foreign"), Identity("identity"), DeletionCapability.PermanentOnly) :
            mismatch == "identity" ? FileEntrySnapshot.Create(Source().Path, Identity("other"), DeletionCapability.PermanentOnly) : Source();
        port.EnqueueVerification(VerificationTestEvidence.ForTarget(source,
            mismatch == "target" ? Path("C:\\elsewhere\\item") : Target()));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(Move(), RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Verification, outcome.Failure);
        Assert.HasCount(0, port.DeletionModes);
        Assert.IsFalse(outcome.Effects.Any(effect => effect.Kind == FileOperationEffectKind.Verified));
    }

    /// <summary>Proves cancellation arriving with a partial deletion preserves its effect and starts no next source.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenCancellationArrivesWithPartialSourceFailureRetainsEffectsAndStopsBatch()
    {
        using CancellationTokenSource cancellation = new();
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(ScriptedCallbackPoint.AfterDeletion, cancellation.Cancel);
        FileEntrySnapshot second = FileEntrySnapshot.Create(Path("C:\\second"), Identity("second"), DeletionCapability.PermanentOnly);
        port.EnqueueInspection(FileInspectionOutcome.Succeeded(Source()));
        port.EnqueueInspection(FileInspectionOutcome.Succeeded(second));
        port.EnqueuePreflight(TransferPreflightOutcome.Succeeded([
            TransferPlanEntry.Transfer(Source(), Target()),
            TransferPlanEntry.Transfer(second, Path("C:\\destination\\second")),
        ]));
        port.EnqueueCopy(ProviderStepOutcome.Succeeded());
        port.EnqueueVerification(ProviderStepOutcome.Succeeded());
        port.EnqueueDeletion(ProviderStepOutcome.FailedAfterEffect(FileOperationFailureKind.Delete, ProviderStepEffectKind.SourcePartiallyDeleted));
        MoveRequest request = Assert.IsInstanceOfType<MoveRequest>(Assert.IsInstanceOfType<FileOperationRequestAccepted>(
            MoveRequest.Create([Source().Path, second.Path], Path("C:\\destination"))).Request);
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(request, RecordingFileOperationProgress.Create(), cancellation.Token);

        Assert.IsTrue(cancellation.IsCancellationRequested);
        Assert.HasCount(1, port.DeletionModes);
        Assert.AreEqual(1, port.Calls.Count(call => call.StartsWith("Copy:", StringComparison.Ordinal)));
        Assert.AreSame(FileOperationEffectKind.SourcePartiallyDeleted, outcome.Effects[^1].Kind);
        Assert.IsFalse(outcome.Effects.Any(effect => effect.Kind == FileOperationEffectKind.SourceDeleted));
    }

    private static ScriptedFileOperationPort Port()
    {
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(FileInspectionOutcome.Succeeded(Source()));
        port.EnqueuePreflight(TransferPreflightOutcome.Succeeded([TransferPlanEntry.Transfer(Source(), Target())]));
        port.EnqueueCopy(ProviderStepOutcome.Succeeded());
        return port;
    }

    private static MoveRequest Move()
    {
        return Assert.IsInstanceOfType<MoveRequest>(Assert.IsInstanceOfType<FileOperationRequestAccepted>(
            MoveRequest.Create([Source().Path], Path("C:\\destination"))).Request);
    }

    private static FileEntrySnapshot Source()
    {
        return FileEntrySnapshot.Create(Path("C:\\item"), Identity("identity"), DeletionCapability.PermanentOnly);
    }

    private static VerifiedEntrySnapshot Observation(FileSystemPath path)
    {
        return VerifiedEntrySnapshot.Create(path, Identity("identity"), FileObjectIdentity.Create("identity"));
    }

    private static FileContentHash Hash()
    {
        return FileContentHash.Create(new string('0', 64));
    }

    private static FileIdentity Identity(string value)
    {
        return Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse(value)).Identity;
    }

    private static FileSystemPath Target()
    {
        return Path("C:\\destination\\item");
    }

    private static FileSystemPath Path(string value)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(value)).Path;
    }
}
