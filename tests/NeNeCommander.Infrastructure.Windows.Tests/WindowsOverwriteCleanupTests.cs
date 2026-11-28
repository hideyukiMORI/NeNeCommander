using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves exact held-link cleanup order, pre-effect uncertainty and retained observations through the single attempt receipt.</summary>
[TestClass]
public sealed class WindowsOverwriteCleanupTests
{
    /// <summary>Proves complete content/record admission and actual removal of only the three owned links, including empty and multibuffer content.</summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(7)]
    [DataRow(131073)]
    public async Task CleanupAsyncWhenCompletedRemovesOwnedLinksWithRecoveryLastAsync(int length)
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create(new string('s', length), "original");
        WindowsOverwriteCleanupFixture calls = new();
        List<TransferArtifactRole> order = [];
        WindowsTransferArtifactRemoval removal = new(handle =>
        {
            IdentifiedTransferArtifact pending = calls.Attempt?.Receipt.Artifacts.OfType<IdentifiedTransferArtifact>().Single(artifact => artifact.Link == ArtifactLinkObservation.Uncertain)
                ?? throw new AssertFailedException("No canonical pending removal receipt.");
            Assert.AreEqual(TransferPathObservation.LastKnownPath, pending.Artifact.PathObservation);
            order.Add(pending.Role);
            return WindowsTransferArtifactRemoval.Native.RequestDisposition(handle);
        }, WindowsTransferArtifactRemoval.Native.Close);
        using WindowsOverwriteAttempt attempt = files.Attempt(new DeterministicIdentifierSource(), calls.Io, WindowsTransferArtifactQueries.Native, removal);
        calls.Attempt = attempt;
        WindowsTransferRecoveryMapping mapping = await WindowsOverwriteCleanupFixture.CompleteAsync(attempt);
        IdentifiedTransferArtifact[] before = [.. attempt.Receipt.Artifacts.Cast<IdentifiedTransferArtifact>()];

        await attempt.CleanupAsync(CancellationToken.None);
        CollectionAssert.AreEqual(new[] { TransferArtifactRole.PreparedContent, TransferArtifactRole.OriginalBackup, TransferArtifactRole.RecoveryRecord }, order);
        foreach (IdentifiedTransferArtifact old in before)
        {
            IdentifiedTransferArtifact current = WindowsOverwritePreparationFixture.Artifact(attempt, old.Role);
            Assert.AreEqual(old.Artifact.ObjectIdentity, current.Artifact.ObjectIdentity);
            Assert.AreEqual(old.Content, current.Content);
            Assert.AreEqual(ArtifactLinkObservation.Removed, current.Link);
            Assert.AreEqual(TransferPathObservation.LastKnownPath, current.Artifact.PathObservation);
            Assert.IsFalse(File.Exists(current.Artifact.Path.CanonicalText));
        }
        Assert.AreEqual(new string('s', length), files.Read(files.Pair.Source.Path));
        Assert.AreEqual(files.Read(files.Pair.Source.Path), files.Read(files.Pair.Target.Path));
        Assert.AreEqual(mapping.Desired, Assert.IsInstanceOfType<DesiredTargetContentVerified>(attempt.Receipt.Target).Content);
        Assert.HasCount(0, attempt.Receipt.Effects);
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.CleanupAsync(CancellationToken.None));
    }

    /// <summary>Proves failed dispositions preserve partial removal and uncertainty without retry or erasing the original target observation.</summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task CleanupAsyncWhenDispositionFailsRetainsExactPartialReceiptAsync(int failureAt)
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("desired", "original");
        int count = 0;
        WindowsTransferArtifactRemoval removal = new(handle => ++count == failureAt ? unchecked((int)0xC0000022) : WindowsTransferArtifactRemoval.Native.RequestDisposition(handle), WindowsTransferArtifactRemoval.Native.Close);
        using WindowsOverwriteAttempt attempt = files.Attempt(new DeterministicIdentifierSource(), WindowsTransferContentIo.Native, WindowsTransferArtifactQueries.Native, removal);
        _ = await WindowsOverwriteCleanupFixture.CompleteAsync(attempt);

        _ = await Assert.ThrowsAsync<IOException>(() => attempt.CleanupAsync(CancellationToken.None));
        Assert.AreEqual(failureAt, count);
        RequirePartial(attempt, failureAt, ArtifactLinkObservation.Uncertain);
        Assert.AreEqual("desired", files.Read(files.Pair.Source.Path));
        Assert.AreEqual("desired", files.Read(files.Pair.Target.Path));
        _ = Assert.IsInstanceOfType<DesiredTargetContentVerified>(attempt.Receipt.Target);
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.CleanupAsync(CancellationToken.None));
        attempt.Dispose();
        RequirePartial(attempt, failureAt, ArtifactLinkObservation.Uncertain);
    }

    /// <summary>Proves unacknowledged close may remove the exact link at disposal while its canonical result stays uncertain.</summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task CleanupAsyncWhenCloseFailsRetainsUncertaintyAfterLaterDisposalAsync(int failureAt)
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("desired", "original");
        int count = 0;
        WindowsTransferArtifactRemoval removal = new(WindowsTransferArtifactRemoval.Native.RequestDisposition, handle => ++count != failureAt && WindowsTransferArtifactRemoval.Native.Close(handle));
        using WindowsOverwriteAttempt attempt = files.Attempt(new DeterministicIdentifierSource(), WindowsTransferContentIo.Native, WindowsTransferArtifactQueries.Native, removal);
        _ = await WindowsOverwriteCleanupFixture.CompleteAsync(attempt);

        _ = await Assert.ThrowsAsync<IOException>(() => attempt.CleanupAsync(CancellationToken.None));
        RequirePartial(attempt, failureAt, ArtifactLinkObservation.Uncertain);
        IdentifiedTransferArtifact uncertain = attempt.Receipt.Artifacts.Cast<IdentifiedTransferArtifact>().Single(artifact => artifact.Link == ArtifactLinkObservation.Uncertain);
        attempt.Dispose();
        Assert.IsFalse(File.Exists(uncertain.Artifact.Path.CanonicalText));
        RequirePartial(attempt, failureAt, ArtifactLinkObservation.Uncertain);
        Assert.AreEqual("desired", files.Read(files.Pair.Source.Path));
        Assert.AreEqual("desired", files.Read(files.Pair.Target.Path));
        _ = await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => attempt.CleanupAsync(CancellationToken.None));
    }

    internal static void RequirePartial(WindowsOverwriteAttempt attempt, int attempted, ArtifactLinkObservation last)
    {
        TransferArtifactRole[] order = [TransferArtifactRole.PreparedContent, TransferArtifactRole.OriginalBackup, TransferArtifactRole.RecoveryRecord];
        for (int index = 0; index < order.Length; index++)
        {
            IdentifiedTransferArtifact current = WindowsOverwritePreparationFixture.Artifact(attempt, order[index]);
            Assert.AreEqual(index + 1 < attempted ? ArtifactLinkObservation.Removed : index + 1 == attempted ? last : ArtifactLinkObservation.Retained, current.Link);
            Assert.AreEqual(TransferPathObservation.LastKnownPath, current.Artifact.PathObservation);
        }
        Assert.HasCount(0, attempt.Receipt.Effects);
    }
}
