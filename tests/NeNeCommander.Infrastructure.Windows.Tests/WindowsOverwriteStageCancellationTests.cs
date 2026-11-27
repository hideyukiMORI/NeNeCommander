using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves cancelled established stages do not begin fresh native admission.</summary>
[TestClass]
public sealed class WindowsOverwriteStageCancellationTests
{
    /// <summary>Cancellation at overwrite or cleanup entry prevents drive queries and further content effects.</summary>
    [TestMethod]
    [DataRow("overwrite")]
    [DataRow("cleanup")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task StageAsyncWhenAlreadyCancelledDoesNotRevalidateDriveAsync(string stage)
    {
        int queries = 0;
        WindowsDriveBoundary boundary = new(_ => { queries++; return DriveType.Fixed; });
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target", driveBoundary: boundary);
        using WindowsOverwriteAttempt attempt = files.Attempt(new DeterministicIdentifierSource(), WindowsTransferContentIo.Native, WindowsTransferArtifactQueries.Native);
        _ = await attempt.PrepareAsync(CancellationToken.None);
        if (stage == "cleanup")
        {
            await attempt.OverwritePreparedAsync(CancellationToken.None);
            await attempt.AppendCompletionAsync(CancellationToken.None);
        }
        queries = 0;
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => stage == "overwrite"
            ? attempt.OverwritePreparedAsync(cancellation.Token) : attempt.CleanupAsync(cancellation.Token));

        Assert.AreEqual(0, queries);
        Assert.HasCount(3, attempt.Receipt.Artifacts);
        Assert.HasCount(0, attempt.Receipt.Effects);
        Assert.AreEqual("source", files.Read(files.Pair.Source.Path));
        Assert.AreEqual(stage == "overwrite" ? "target" : "source", files.Read(files.Pair.Target.Path));
    }
}
