using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the one attempt appends exact initial-bound completion while retaining target, source and every preparation.</summary>
[TestClass]
public sealed class WindowsOverwriteCompletionTests
{
    /// <summary>Proves an actual verified target can record completion without rewriting its initial mapping or completing provider effects.</summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(3)]
    [DataRow(131073)]
    public async Task AppendCompletionAsyncWhenDesiredContentWasVerifiedPreservesInitialAndAllArtifactsAsync(int sourceLength)
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create(new string('s', sourceLength), "old");
        WindowsOverwriteCompletionFault calls = new("none");
        using WindowsOverwriteAttempt attempt = fixture.Attempt(new DeterministicIdentifierSource(), calls.Io, WindowsTransferArtifactQueries.Native);
        WindowsTransferRecoveryMapping mapping = await attempt.PrepareAsync(CancellationToken.None);
        RecoveryMappingContent initial = Recovery(attempt);
        await attempt.OverwritePreparedAsync(CancellationToken.None);
        calls.Arm(initial.InitialFrame.Frame);

        await attempt.AppendCompletionAsync(CancellationToken.None);
        byte[] bytes = Encoding.UTF8.GetBytes(fixture.Read(mapping.Names.Recovery));
        byte[] prefix = WindowsTransferRecoveryCodec.EncodeInitial(mapping);
        CollectionAssert.AreEqual(prefix, bytes[..prefix.Length]);
        WindowsTransferRecoveryCompleted decoded = Assert.IsInstanceOfType<WindowsTransferRecoveryCompleted>(WindowsTransferRecoveryCodec.Inspect(bytes));
        Assert.AreEqual(mapping, decoded.Mapping);
        RecoveryMappingContent saved = Recovery(attempt);
        Assert.AreEqual(initial.InitialFrame, saved.InitialFrame);
        VerifiedRecoveryAppend append = Assert.IsInstanceOfType<VerifiedRecoveryAppend>(saved.Append);
        Assert.AreEqual(decoded.Completion, append.Frame.Frame);
        Assert.AreEqual(RecoveryFlushObservation.Confirmed, append.Frame.Flush);
        Assert.AreEqual(1, calls.Writes);
        Assert.AreEqual(1, calls.Flushes);
        Assert.AreEqual(mapping.Desired, Assert.IsInstanceOfType<DesiredTargetContentVerified>(attempt.Receipt.Target).Content);
        RequireRetained(fixture, attempt, mapping);
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.AppendCompletionAsync(CancellationToken.None));
        attempt.Dispose();
        Assert.AreEqual(saved, Recovery(attempt));
        _ = await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => attempt.AppendCompletionAsync(CancellationToken.None));
    }

    /// <summary>Proves native append, readback and flush failures preserve the first mapping and never lose target verification or source.</summary>
    [TestMethod]
    [DataRow("before")]
    [DataRow("partial")]
    [DataRow("after")]
    [DataRow("readThrow")]
    [DataRow("readMismatch")]
    [DataRow("extra")]
    [DataRow("flush")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task AppendCompletionAsyncWhenNativeStageFailsRetainsIndependentEvidenceAsync(string defect)
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("desired", "old");
        WindowsOverwriteCompletionFault calls = new(defect);
        using WindowsOverwriteAttempt attempt = fixture.Attempt(new DeterministicIdentifierSource(), calls.Io, WindowsTransferArtifactQueries.Native);
        WindowsTransferRecoveryMapping mapping = await attempt.PrepareAsync(CancellationToken.None);
        RecoveryMappingContent initial = Recovery(attempt);
        await attempt.OverwritePreparedAsync(CancellationToken.None);
        calls.Arm(initial.InitialFrame.Frame);

        _ = await Assert.ThrowsAsync<IOException>(() => attempt.AppendCompletionAsync(CancellationToken.None));
        RecoveryMappingContent saved = Recovery(attempt);
        Assert.AreEqual(initial.InitialFrame, saved.InitialFrame);
        if (defect == "flush")
        {
            VerifiedRecoveryAppend append = Assert.IsInstanceOfType<VerifiedRecoveryAppend>(saved.Append);
            Assert.AreEqual(RecoveryFlushObservation.OutcomeUnconfirmed, append.Frame.Flush);
        }
        else
        {
            Assert.AreEqual(RecoveryAppendObservation.OutcomeUnconfirmed, saved.Append);
        }
        Assert.AreEqual(defect == "flush" ? 1 : 0, calls.Flushes);
        byte[] prefix = WindowsTransferRecoveryCodec.EncodeInitial(mapping);
        byte[] savedBytes = Encoding.UTF8.GetBytes(fixture.Read(mapping.Names.Recovery));
        CollectionAssert.AreEqual(prefix, savedBytes[..prefix.Length]);
        Assert.AreEqual(mapping.Desired, Assert.IsInstanceOfType<DesiredTargetContentVerified>(attempt.Receipt.Target).Content);
        RequireRetained(fixture, attempt, mapping);
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.AppendCompletionAsync(CancellationToken.None));
    }

    private static RecoveryMappingContent Recovery(WindowsOverwriteAttempt attempt)
    {
        return Assert.IsInstanceOfType<RecoveryMappingContent>(WindowsOverwritePreparationFixture.Artifact(attempt, TransferArtifactRole.RecoveryRecord).Content);
    }

    private static void RequireRetained(WindowsOverwritePreparationFixture fixture, WindowsOverwriteAttempt attempt, WindowsTransferRecoveryMapping mapping)
    {
        Assert.AreEqual(fixture.Read(fixture.Pair.Source.Path), fixture.Read(fixture.Pair.Target.Path));
        Assert.AreEqual("old", fixture.Read(mapping.Names.OriginalBackup));
        Assert.AreEqual(fixture.Read(fixture.Pair.Source.Path), fixture.Read(mapping.Names.PreparedContent));
        Assert.HasCount(0, attempt.Receipt.Effects);
        Assert.HasCount(3, attempt.Receipt.Artifacts);
        Assert.IsTrue(attempt.Receipt.Artifacts.Cast<IdentifiedTransferArtifact>().All(artifact => artifact.Link == ArtifactLinkObservation.Retained));
    }
}
