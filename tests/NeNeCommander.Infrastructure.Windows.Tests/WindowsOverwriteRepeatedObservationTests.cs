using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves repeated cleanup observations revoke current path claims after earlier checks restored them.</summary>
[TestClass]
public sealed class WindowsOverwriteRepeatedObservationTests
{
    /// <summary>Both the second target verification and the recovery read revoke the corresponding current path until their byte checks finish.</summary>
    [TestMethod]
    [DataRow("secondTarget")]
    [DataRow("recovery")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task CleanupAsyncWhenRepeatedReadIsPendingRetainsOnlySavedObservationsAsync(string boundary)
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsOverwriteAttempt? observed = null;
        int sourceReads = 0;
        bool sawCurrentTarget = false;
        TransferReadSuspension io = new((handle, offset) =>
        {
            sawCurrentTarget |= observed?.Receipt.Target?.Target.PathObservation == TransferPathObservation.CurrentPathVerified;
            return offset == 0 && (boundary == "secondTarget"
                ? ReferenceEquals(handle, files.Pair.Source.Handle) && ++sourceReads == 2
                : WindowsOverwritePreparationFixture.IsArtifact(handle, observed, TransferArtifactRole.RecoveryRecord));
        });
        using WindowsOverwriteAttempt attempt = files.Attempt(new DeterministicIdentifierSource(), io.Io, WindowsTransferArtifactQueries.Native);
        observed = attempt;
        _ = await attempt.PrepareAsync(CancellationToken.None);
        await attempt.OverwritePreparedAsync(CancellationToken.None);
        await attempt.AppendCompletionAsync(CancellationToken.None);
        ProviderStepReceipt previous = attempt.Receipt;
        Assert.AreEqual(TransferPathObservation.CurrentPathVerified, Recovery(previous).Artifact.PathObservation);
        io.Arm();
        Task pending = attempt.CleanupAsync(CancellationToken.None);
        try
        {
            Assert.IsTrue(io.Suspended);
            Assert.IsFalse(pending.IsCompleted);
            Assert.IsTrue(sawCurrentTarget);
            RequirePending(attempt.Receipt, previous, boundary);
        }
        finally
        {
            io.Release();
            await pending;
        }
        Assert.AreEqual("source", files.Read(files.Pair.Source.Path));
        Assert.AreEqual("source", files.Read(files.Pair.Target.Path));
        Assert.IsTrue(attempt.Receipt.Artifacts.Cast<IdentifiedTransferArtifact>().All(artifact => artifact.Link == ArtifactLinkObservation.Removed));
    }

    private static void RequirePending(ProviderStepReceipt current, ProviderStepReceipt previous, string boundary)
    {
        DesiredTargetContentVerified target = Assert.IsInstanceOfType<DesiredTargetContentVerified>(current.Target);
        Assert.AreEqual(Assert.IsInstanceOfType<DesiredTargetContentVerified>(previous.Target).Content, target.Content);
        Assert.AreEqual(Recovery(previous).Content, Recovery(current).Content);
        Assert.HasCount(3, current.Artifacts);
        Assert.IsTrue(current.Artifacts.Cast<IdentifiedTransferArtifact>().All(artifact => artifact.Link == ArtifactLinkObservation.Retained));
        Assert.AreEqual(TransferPathObservation.LastKnownPath, boundary == "secondTarget"
            ? target.Target.PathObservation : Recovery(current).Artifact.PathObservation);
    }

    private static IdentifiedTransferArtifact Recovery(ProviderStepReceipt receipt)
    {
        return receipt.Artifacts.Cast<IdentifiedTransferArtifact>().Single(artifact => artifact.Role == TransferArtifactRole.RecoveryRecord);
    }
}
