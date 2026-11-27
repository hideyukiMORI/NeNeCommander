using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves original admission is repeated after artifact creation and cancellation is observed after final native evidence.</summary>
[TestClass]
public sealed class WindowsOverwritePreparationReadAdmissionTests
{
    /// <summary>A source change during the last artifact creation prevents every preparation read/write, preserving three empty owned artifacts.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task PrepareAsyncWhenSourceChangesDuringArtifactCreationRefusesBeforeContentIoAsync()
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        bool changed = false;
        int contentCalls = 0;
        WindowsTransferArtifactQueries native = WindowsTransferArtifactQueries.Native;
        WindowsTransferArtifactQueries queries = new(native.ReadIdentity, native.ReadFacts, (handle, path) =>
        {
            native.RequirePath(handle, path);
            if (!changed && path.CanonicalText.EndsWith("-prepared.bin", StringComparison.Ordinal))
            {
                files.Root.SetLastWriteTimeUtc("source", new DateTime(2000, 1, 2, 3, 4, 5, DateTimeKind.Utc));
                changed = true;
            }
        });
        WindowsTransferContentIo io = new((handle, bytes, offset, token) => { contentCalls++; return RandomAccess.ReadAsync(handle, bytes, offset, token); },
            (handle, bytes, offset, token) => { contentCalls++; return RandomAccess.WriteAsync(handle, bytes, offset, token); },
            (handle, length) => { contentCalls++; RandomAccess.SetLength(handle, length); }, handle => { contentCalls++; RandomAccess.FlushToDisk(handle); });
        using WindowsOverwriteAttempt attempt = files.Attempt(new DeterministicIdentifierSource(), io, queries);

        _ = await Assert.ThrowsAsync<IOException>(() => attempt.PrepareAsync(CancellationToken.None));

        Assert.IsTrue(changed);
        Assert.AreEqual(0, contentCalls);
        Assert.HasCount(3, attempt.Receipt.Artifacts);
        foreach (IdentifiedTransferArtifact artifact in attempt.Receipt.Artifacts.Cast<IdentifiedTransferArtifact>())
        {
            Assert.AreEqual(ArtifactContentObservation.Unknown, artifact.Content);
            Assert.AreEqual(ArtifactLinkObservation.Retained, artifact.Link);
            Assert.AreEqual(0L, new FileInfo(artifact.Artifact.Path.CanonicalText).Length);
        }
        _ = Assert.IsInstanceOfType<UntouchedTransferTarget>(attempt.Receipt.Target);
        Assert.AreEqual("source", files.Read(files.Pair.Source.Path));
        Assert.AreEqual("target", files.Read(files.Pair.Target.Path));
    }

    /// <summary>Cancellation after the last recovery readback and native admission cannot turn a confirmed mapping into a successfully prepared attempt.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task PrepareAsyncWhenFinalRecoveryPathObservationCancelsDoesNotAdvancePreparedStateAsync()
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        using CancellationTokenSource cancellation = new();
        WindowsOverwriteCleanupFixture calls = new();
        bool flushed = false;
        bool finalRead = false;
        int cancellations = 0;
        WindowsTransferArtifactQueries native = WindowsTransferArtifactQueries.Native;
        WindowsTransferArtifactQueries queries = new(native.ReadIdentity, native.ReadFacts, (handle, path) =>
        {
            native.RequirePath(handle, path);
            if (finalRead && path.CanonicalText.EndsWith("-recovery.txt", StringComparison.Ordinal))
            {
                cancellations++;
                cancellation.Cancel();
            }
        });
        WindowsTransferContentIo io = new(async (handle, bytes, offset, token) =>
        {
            int count = await calls.Read(handle, bytes, offset, token);
            finalRead |= flushed && count == 0 && ReferenceEquals(handle, calls.Handle(TransferArtifactRole.RecoveryRecord));
            return count;
        }, calls.Io.WriteAsync, calls.Io.SetLength, handle => { RandomAccess.FlushToDisk(handle); flushed = true; });
        using WindowsOverwriteAttempt attempt = files.Attempt(new DeterministicIdentifierSource(), io, queries);
        calls.Attempt = attempt;

        _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => attempt.PrepareAsync(cancellation.Token));

        Assert.IsTrue(finalRead);
        Assert.AreEqual(1, cancellations);
        RecoveryMappingContent saved = Assert.IsInstanceOfType<RecoveryMappingContent>(
            WindowsOverwritePreparationFixture.Artifact(attempt, TransferArtifactRole.RecoveryRecord).Content);
        Assert.AreEqual(RecoveryFlushObservation.Confirmed, saved.InitialFrame.Flush);
        Assert.AreEqual(RecoveryAppendObservation.NotAttempted, saved.Append);
        Assert.HasCount(3, attempt.Receipt.Artifacts);
        Assert.AreEqual("source", files.Read(files.Pair.Source.Path));
        Assert.AreEqual("target", files.Read(files.Pair.Target.Path));
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.OverwritePreparedAsync(CancellationToken.None));
    }
}
