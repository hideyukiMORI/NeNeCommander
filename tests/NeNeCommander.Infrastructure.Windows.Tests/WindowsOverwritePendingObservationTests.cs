using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves a suspended stage exposes saved content history without claiming an admitted current pathname.</summary>
[TestClass]
public sealed class WindowsOverwritePendingObservationTests
{
    /// <summary>Append and cleanup drop current target-path qualification before their first pending I/O; cleanup drops every artifact path too.</summary>
    [TestMethod]
    [DataRow("append")]
    [DataRow("cleanup")]
    public async Task StageAsyncWhenFirstIoIsPendingRetainsOnlyLastKnownPathsAsync(string stage)
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        TransferSuspensionIo io = new(System.IO.RandomAccess.FlushToDisk);
        using WindowsOverwriteAttempt attempt = files.Attempt(new DeterministicIdentifierSource(), io.Io, WindowsTransferArtifactQueries.Native);
        _ = await attempt.PrepareAsync(CancellationToken.None);
        await attempt.OverwritePreparedAsync(CancellationToken.None);
        if (stage == "cleanup")
        {
            await attempt.AppendCompletionAsync(CancellationToken.None);
        }
        FileContentEvidence desired = Assert.IsInstanceOfType<DesiredTargetContentVerified>(attempt.Receipt.Target).Content;
        io.Arm(1);
        Task pending = stage == "append" ? attempt.AppendCompletionAsync(CancellationToken.None) : attempt.CleanupAsync(CancellationToken.None);
        try
        {
            Assert.IsTrue(io.Suspended);
            Assert.IsFalse(pending.IsCompleted);
            DesiredTargetContentVerified target = Assert.IsInstanceOfType<DesiredTargetContentVerified>(attempt.Receipt.Target);
            Assert.AreEqual(desired, target.Content);
            Assert.AreEqual(TransferPathObservation.LastKnownPath, target.Target.PathObservation);
            if (stage == "cleanup")
            {
                Assert.IsTrue(attempt.Receipt.Artifacts.Cast<IdentifiedTransferArtifact>()
                    .All(artifact => artifact.Artifact.PathObservation == TransferPathObservation.LastKnownPath));
            }
        }
        finally
        {
            io.Release();
            await pending;
        }
        Assert.AreEqual("source", files.Read(files.Pair.Source.Path));
        Assert.AreEqual("source", files.Read(files.Pair.Target.Path));
    }
}
