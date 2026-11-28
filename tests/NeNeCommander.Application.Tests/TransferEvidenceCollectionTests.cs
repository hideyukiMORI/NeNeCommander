using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves confirmation and verified-transfer collections cannot expose mutable backing state.</summary>
[TestClass]
public sealed class TransferEvidenceCollectionTests
{
    /// <summary>Proves synchronization storage cannot erase already frozen transfer facts.</summary>
    [TestMethod]
    [DataRow("conflicts")]
    [DataRow("plan")]
    [DataRow("choices")]
    [DataRow("sources")]
    [DataRow("children")]
    [DataRow("sourceAncestors")]
    [DataRow("targetAncestors")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-016")]
    [TestProperty("ThreatId", "ADV-006")]
    [TestProperty("ThreatId", "ADV-007")]
    public void CreateWhenSynchronizationStorageIsExposedCannotEraseFrozenFacts(string boundary)
    {
        IReadOnlyList<object> facts = Collection(boundary);
        object original = facts[0];
        ICollection collection = Assert.IsInstanceOfType<ICollection>(facts);
        IList storage = Assert.IsInstanceOfType<IList>(collection.SyncRoot);
        _ = Assert.ThrowsExactly<NotSupportedException>(storage.Clear);
        Assert.HasCount(1, facts);
        Assert.AreSame(original, facts[0]);
    }

    /// <summary>Proves the allowed-decision array cannot replace a provider's capability through SyncRoot.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-006")]
    public void CreateWhenDecisionStorageIsExposedCannotReplaceAllowedChoice()
    {
        TransferConflict conflict = Conflict();
        ICollection collection = Assert.IsInstanceOfType<ICollection>(conflict.AllowedDecisions);
        IList storage = Assert.IsInstanceOfType<IList>(collection.SyncRoot);
        _ = Assert.ThrowsExactly<NotSupportedException>(() => storage[0] = TransferConflictDecision.Cancel);
        Assert.AreSame(TransferConflictDecision.Skip, conflict.AllowedDecisions[0]);
    }

    /// <summary>Proves verification validates the same single snapshot it ultimately retains.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    public void CreateWhenVerifiedCollectionsChangeOnSecondEnumerationRetainsValidatedFacts()
    {
        VerifiedEntrySnapshot source = Observation(@"C:\source");
        VerifiedEntrySnapshot target = Observation(@"D:\target");
        VerifiedCopyEntry child = VerifiedCopyEntry.File(source, target, FileContentHash.Create(new string('0', 64)), FileVerificationRequirements.DefaultStream);
        ChangingReceiptCollection<VerifiedCopyEntry> children = new(child, null!);
        ChangingReceiptCollection<VerifiedEntrySnapshot> sourceAncestors = new(source, null!);
        ChangingReceiptCollection<VerifiedEntrySnapshot> targetAncestors = new(target, null!);

        VerifiedCopyEntry.VerifiedDirectory directory = Assert.IsInstanceOfType<VerifiedCopyEntry.VerifiedDirectory>(
            VerifiedCopyEntry.Directory(source, target, children));
        VerifiedTreeBoundary boundary = VerifiedTreeBoundary.Create(sourceAncestors, targetAncestors);

        Assert.AreSame(child, directory.Children[0]);
        Assert.AreSame(source, boundary.SourceAncestors[0]);
        Assert.AreSame(target, boundary.TargetAncestors[0]);
    }

    /// <summary>Proves conflict admission validates materialized count instead of a caller's conflicting Count property.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-006")]
    public void ConflictedWhenAdvertisedCountDisagreesWithEmptyEnumerationRefusesIt()
    {
        _ = Assert.ThrowsExactly<ArgumentException>(() => TransferPreflightOutcome.Conflicted(new EmptyAdvertisedConflict()));
    }

    /// <summary>Proves null elements cannot enter complete preflight facts.</summary>
    [TestMethod]
    public void PreflightWhenStoredEntriesAreAbsentRefusesThem()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferPreflightOutcome.Succeeded([null!]));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferPreflightOutcome.Conflicted([null!]));
    }

    /// <summary>Proves the real gateway never gives a provider mutable access to its reference batch.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-016")]
    public async Task ExecuteAsyncWhenProviderAttemptsToErasePreflightSourcesPreservesTheBatch()
    {
        TransferConflict conflict = Conflict();
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(TransferConflictTestContext.Inspection(conflict.Source.Path));
        port.EnqueuePreflight(TransferPreflightOutcome.Rejected(FileOperationFailureKind.ProviderUnavailable));
        port.ObservePreflightSources(sources =>
        {
            ICollection collection = Assert.IsInstanceOfType<ICollection>(sources);
            IList storage = Assert.IsInstanceOfType<IList>(collection.SyncRoot);
            _ = Assert.ThrowsExactly<NotSupportedException>(storage.Clear);
            Assert.HasCount(1, sources);
            Assert.AreEqual(conflict.Source, sources[0]);
        });
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            TransferConflictTestContext.CreateCopy([conflict.Source.Path], TransferConflictTestContext.ParsePath(@"D:\target")),
            RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
        Assert.IsEmpty(outcome.Effects);
        Assert.HasCount(2, port.Calls);
    }

    private static IReadOnlyList<object> Collection(string boundary)
    {
        TransferConflict conflict = Conflict();
        ConflictSet conflicts = Assert.IsInstanceOfType<ConflictSet>(TransferPreflightOutcome.Conflicted([conflict]));
        VerifiedEntrySnapshot source = Observation(@"C:\source");
        VerifiedEntrySnapshot target = Observation(@"D:\target");
        VerifiedCopyEntry file = VerifiedCopyEntry.File(source, target, FileContentHash.Create(new string('0', 64)), FileVerificationRequirements.DefaultStream);
        VerifiedTreeBoundary tree = VerifiedTreeBoundary.Create([source], [target]);
        return boundary switch
        {
            "conflicts" => conflicts.Conflicts,
            "plan" => Assert.IsInstanceOfType<TransferPreflightSucceeded>(TransferPreflightOutcome.Succeeded(
                [TransferPlanEntry.Transfer(conflict.Source, conflict.ExistingTarget)])).Plan,
            "choices" => TransferResolution.None.Add(conflicts, TransferConflictDecision.Skip, TransferConflictScope.Current).Choices,
            "sources" => FrozenTransfer.Create(TransferConflictTestContext.CreateCopy([conflict.Source.Path], target.Path), [conflict.Source], target.Path).Sources,
            "children" => Assert.IsInstanceOfType<VerifiedCopyEntry.VerifiedDirectory>(VerifiedCopyEntry.Directory(source, target, [file])).Children,
            "sourceAncestors" => tree.SourceAncestors,
            "targetAncestors" => tree.TargetAncestors,
            _ => throw new ArgumentOutOfRangeException(nameof(boundary))
        };
    }

    private static TransferConflict Conflict()
    {
        return TransferConflictTestContext.Conflict(TransferConflictTestContext.ParsePath(@"C:\source.txt"),
            TransferConflictTestContext.ParsePath(@"D:\target"), "source (2).txt");
    }

    private static VerifiedEntrySnapshot Observation(string path)
    {
        FileEntrySnapshot source = TransferConflictTestContext.Inspection(TransferConflictTestContext.ParsePath(path)).Snapshot;
        return VerifiedEntrySnapshot.Create(source.Path, source.Identity, FileObjectIdentity.Create(path));
    }

    private sealed class EmptyAdvertisedConflict : IReadOnlyList<TransferConflict>
    {
        public int Count => 1;
        public TransferConflict this[int index] => throw new ArgumentOutOfRangeException(nameof(index));
        public IEnumerator<TransferConflict> GetEnumerator()
        {
            yield break;
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
