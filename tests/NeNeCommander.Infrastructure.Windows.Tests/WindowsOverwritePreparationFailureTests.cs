using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves real artifact write, truncate and flush effects survive exceptions without changing the original target.</summary>
[TestClass]
public sealed class WindowsOverwritePreparationFailureTests
{
    /// <summary>Proves a throwing call may have written partial or complete bytes, so the receipt retains unknown content and the owned artifact.</summary>
    [TestMethod]
    [DataRow("backup", "partial")]
    [DataRow("backup", "full")]
    [DataRow("backup", "truncate")]
    [DataRow("prepared", "partial")]
    [DataRow("prepared", "full")]
    [DataRow("prepared", "truncate")]
    [DataRow("recovery", "partial")]
    [DataRow("recovery", "full")]
    [DataRow("recovery", "truncate")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task PrepareAsyncWhenArtifactMutationThrowsRetainsUncertainContentAsync(string artifact, string failure)
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        TransferArtifactRole role = Role(artifact);
        WindowsOverwriteAttempt? observed = null;
        WindowsTransferContentIo io = FailingIo(() => observed, role, failure);
        using WindowsOverwriteAttempt attempt = fixture.Attempt(new DeterministicIdentifierSource(), io, WindowsTransferArtifactQueries.Native);
        observed = attempt;

        _ = await Assert.ThrowsAsync<IOException>(() => attempt.PrepareAsync(CancellationToken.None));
        IdentifiedTransferArtifact retained = WindowsOverwritePreparationFixture.Artifact(attempt, role);
        Assert.AreEqual(ArtifactContentObservation.Unknown, retained.Content);
        Assert.AreEqual(TransferPathObservation.LastKnownPath, retained.Artifact.PathObservation);
        Assert.AreEqual(ArtifactLinkObservation.Retained, retained.Link);
        long length = new FileInfo(retained.Artifact.Path.CanonicalText).Length;
        Assert.IsTrue(failure == "partial" ? length == 1 : length >= 6);
        Assert.HasCount(3, attempt.Receipt.Artifacts);
        fixture.RequireUntouched(attempt, "source", "target");
    }

    /// <summary>Proves flush uncertainty and acknowledgement are recorded before exception or cancellation exits, without losing complete mapping bytes.</summary>
    [TestMethod]
    [DataRow("throw")]
    [DataRow("cancel")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task PrepareAsyncWhenFlushFailsOrCancelsRetainsActualAcknowledgementAsync(string failure)
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        using CancellationTokenSource cancellation = new();
        WindowsOverwriteAttempt? observed = null;
        WindowsTransferContentIo io = new(RandomAccess.ReadAsync, RandomAccess.WriteAsync, RandomAccess.SetLength, handle =>
        {
            RecoveryMappingContent before = Assert.IsInstanceOfType<RecoveryMappingContent>(WindowsOverwritePreparationFixture.Artifact(observed ?? throw new AssertFailedException(), TransferArtifactRole.RecoveryRecord).Content);
            Assert.AreEqual(RecoveryFlushObservation.OutcomeUnconfirmed, before.InitialFrame.Flush);
            RandomAccess.FlushToDisk(handle);
            if (failure == "throw")
            {
                throw new IOException("Injected exception after native flush.");
            }
            cancellation.Cancel();
        });
        using WindowsOverwriteAttempt attempt = fixture.Attempt(new DeterministicIdentifierSource(), io, WindowsTransferArtifactQueries.Native);
        observed = attempt;

        _ = failure == "throw"
            ? (Exception)await Assert.ThrowsAsync<IOException>(() => attempt.PrepareAsync(cancellation.Token))
            : await Assert.ThrowsAsync<OperationCanceledException>(() => attempt.PrepareAsync(cancellation.Token));
        RecoveryMappingContent after = Assert.IsInstanceOfType<RecoveryMappingContent>(WindowsOverwritePreparationFixture.Artifact(attempt, TransferArtifactRole.RecoveryRecord).Content);
        Assert.AreEqual(failure == "throw" ? RecoveryFlushObservation.OutcomeUnconfirmed : RecoveryFlushObservation.Confirmed, after.InitialFrame.Flush);
        Assert.IsTrue(after.InitialFrame.Frame.Size.Bytes > 0);
        Assert.AreEqual(RecoveryAppendObservation.NotAttempted, after.Append);
        fixture.RequireUntouched(attempt, "source", "target");
    }

    private static WindowsTransferContentIo FailingIo(Func<WindowsOverwriteAttempt?> observed, TransferArtifactRole role, string failure)
    {
        return new WindowsTransferContentIo(RandomAccess.ReadAsync, async (handle, bytes, offset, cancellationToken) =>
        {
            if (failure != "truncate" && WindowsOverwritePreparationFixture.IsArtifact(handle, observed(), role))
            {
                await RandomAccess.WriteAsync(handle, failure == "partial" ? bytes[..1] : bytes, offset, cancellationToken);
                throw new IOException("Injected exception after native artifact bytes.");
            }
            await RandomAccess.WriteAsync(handle, bytes, offset, cancellationToken);
        }, (handle, length) =>
        {
            RandomAccess.SetLength(handle, length);
            if (failure == "truncate" && WindowsOverwritePreparationFixture.IsArtifact(handle, observed(), role))
            {
                throw new IOException("Injected exception after native artifact truncation.");
            }
        }, RandomAccess.FlushToDisk);
    }

    private static TransferArtifactRole Role(string artifact)
    {
        return artifact switch
        {
            "backup" => TransferArtifactRole.OriginalBackup,
            "prepared" => TransferArtifactRole.PreparedContent,
            "recovery" => TransferArtifactRole.RecoveryRecord,
            _ => throw new ArgumentOutOfRangeException(nameof(artifact)),
        };
    }
}
