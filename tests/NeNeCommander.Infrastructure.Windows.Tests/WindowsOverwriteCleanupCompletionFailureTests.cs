using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves a failed completion stage never grants cleanup even when desired target or a complete appended frame was already observed.</summary>
[TestClass]
public sealed class WindowsOverwriteCleanupCompletionFailureTests
{
    /// <summary>Proves failed completion readback or flush leaves all three artifacts and no cleanup continuation.</summary>
    [TestMethod]
    [DataRow("readThrow")]
    [DataRow("flush")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task CleanupAsyncWhenCompletionStageFailsRefusesWithoutRemovingArtifactsAsync(string fault)
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsOverwriteCompletionFault calls = new(fault);
        using WindowsOverwriteAttempt attempt = files.Attempt(new DeterministicIdentifierSource(), calls.Io, WindowsTransferArtifactQueries.Native);
        _ = await attempt.PrepareAsync(CancellationToken.None);
        RecoveryMappingContent initial = Assert.IsInstanceOfType<RecoveryMappingContent>(WindowsOverwritePreparationFixture.Artifact(attempt, TransferArtifactRole.RecoveryRecord).Content);
        await attempt.OverwritePreparedAsync(CancellationToken.None);
        calls.Arm(initial.InitialFrame.Frame);
        _ = await Assert.ThrowsAsync<IOException>(() => attempt.AppendCompletionAsync(CancellationToken.None));

        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.CleanupAsync(CancellationToken.None));
        Assert.IsTrue(attempt.Receipt.Artifacts.Cast<IdentifiedTransferArtifact>().All(artifact => artifact.Link == ArtifactLinkObservation.Retained));
        Assert.HasCount(3, attempt.Receipt.Artifacts);
        Assert.HasCount(0, attempt.Receipt.Effects);
        _ = Assert.IsInstanceOfType<DesiredTargetContentVerified>(attempt.Receipt.Target);
        Assert.AreEqual("source", files.Read(files.Pair.Source.Path));
    }
}
