using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves fully read-back completion bytes survive a subsequent failed native observation without inventing flush evidence.</summary>
[TestClass]
public sealed class WindowsOverwriteCompletionObservationTests
{
    /// <summary>Proves identity/metadata failure after exact readback retains the verified frame before a flush was attempted.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task AppendCompletionAsyncWhenReadbackSucceedsBeforeMetadataFailureRetainsFrameAsync()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        bool armed = false;
        bool written = false;
        bool readbackEnded = false;
        int flushes = 0;
        WindowsTransferContentIo io = new(async (handle, bytes, offset, token) =>
        {
            int count = await RandomAccess.ReadAsync(handle, bytes, offset, token);
            readbackEnded |= armed && written && count == 0;
            return count;
        }, async (handle, bytes, offset, token) =>
        {
            await RandomAccess.WriteAsync(handle, bytes, offset, token);
            written |= armed;
        }, RandomAccess.SetLength, handle =>
        {
            flushes += armed ? 1 : 0;
            RandomAccess.FlushToDisk(handle);
        });
        WindowsTransferArtifactQueries queries = new(WindowsTransferArtifactQueries.Native.ReadIdentity,
            handle => readbackEnded ? throw new IOException("Injected metadata failure after complete readback.") : WindowsTransferArtifactQueries.Native.ReadFacts(handle),
            WindowsTransferArtifactQueries.Native.RequirePath);
        using WindowsOverwriteAttempt attempt = fixture.Attempt(new DeterministicIdentifierSource(), io, queries);
        WindowsTransferRecoveryMapping mapping = await attempt.PrepareAsync(CancellationToken.None);
        await attempt.OverwritePreparedAsync(CancellationToken.None);
        armed = true;

        _ = await Assert.ThrowsAsync<IOException>(() => attempt.AppendCompletionAsync(CancellationToken.None));
        IdentifiedTransferArtifact artifact = WindowsOverwritePreparationFixture.Artifact(attempt, TransferArtifactRole.RecoveryRecord);
        RecoveryMappingContent saved = Assert.IsInstanceOfType<RecoveryMappingContent>(artifact.Content);
        VerifiedRecoveryAppend append = Assert.IsInstanceOfType<VerifiedRecoveryAppend>(saved.Append);
        Assert.AreEqual(RecoveryFlushObservation.NotAttempted, append.Frame.Flush);
        Assert.AreEqual(RecoveryFlushObservation.Confirmed, saved.InitialFrame.Flush);
        Assert.AreEqual(TransferPathObservation.LastKnownPath, artifact.Artifact.PathObservation);
        Assert.AreEqual(0, flushes);
        Assert.AreEqual(mapping.Desired, Assert.IsInstanceOfType<DesiredTargetContentVerified>(attempt.Receipt.Target).Content);
        Assert.HasCount(0, attempt.Receipt.Effects);
        Assert.HasCount(3, attempt.Receipt.Artifacts);
    }
}
