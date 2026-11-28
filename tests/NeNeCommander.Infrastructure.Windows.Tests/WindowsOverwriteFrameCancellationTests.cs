using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves cancellation at fallible metadata acknowledgements retains exact frame/flush history without advancing the attempt.</summary>
[TestClass]
public sealed class WindowsOverwriteFrameCancellationTests
{
    /// <summary>Before-flush cancellation invokes no flush; after-flush cancellation retains Confirmed evidence but never enables cleanup.</summary>
    [TestMethod]
    [DataRow("initialBeforeFlush")]
    [DataRow("completionBeforeFlush")]
    [DataRow("completionAfterFlush")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task FrameAsyncWhenMetadataAcknowledgementCancelsRetainsPhaseWithoutSuccessAsync(string boundary)
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        using CancellationTokenSource cancellation = new();
        WindowsOverwriteAttempt? observed = null;
        bool armed = boundary == "initialBeforeFlush";
        int cancelled = 0;
        int flushes = 0;
        WindowsTransferArtifactQueries queries = new(WindowsTransferArtifactQueries.Native.ReadIdentity,
            WindowsTransferArtifactQueries.Native.ReadFacts, (handle, path) =>
            {
                WindowsTransferArtifactQueries.Native.RequirePath(handle, path);
                RecoveryMappingContent? saved = SavedOrAbsent(observed);
                if (armed && saved is not null && AtBoundary(saved, boundary))
                {
                    armed = false;
                    cancelled++;
                    cancellation.Cancel();
                }
            });
        WindowsTransferContentIo io = new(RandomAccess.ReadAsync, RandomAccess.WriteAsync, RandomAccess.SetLength, handle =>
        {
            flushes++;
            RandomAccess.FlushToDisk(handle);
        });
        using WindowsOverwriteAttempt attempt = files.Attempt(new DeterministicIdentifierSource(), io, queries);
        observed = attempt;
        await AdvanceBeforeAsync(attempt, boundary);
        armed = true;
        flushes = 0;

        _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => boundary == "initialBeforeFlush"
            ? attempt.PrepareAsync(cancellation.Token) : attempt.AppendCompletionAsync(cancellation.Token));

        Assert.AreEqual(1, cancelled);
        Assert.AreEqual(boundary == "completionAfterFlush" ? 1 : 0, flushes);
        Assert.IsTrue(AtBoundary(Assert.IsInstanceOfType<RecoveryMappingContent>(SavedOrAbsent(attempt)), boundary));
        Assert.AreEqual("source", files.Read(files.Pair.Source.Path));
        Assert.AreEqual(boundary == "initialBeforeFlush" ? "target" : "source", files.Read(files.Pair.Target.Path));
        Assert.HasCount(3, attempt.Receipt.Artifacts);
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.CleanupAsync(CancellationToken.None));
    }

    private static async Task AdvanceBeforeAsync(WindowsOverwriteAttempt attempt, string boundary)
    {
        if (boundary != "initialBeforeFlush")
        {
            _ = await attempt.PrepareAsync(CancellationToken.None);
            await attempt.OverwritePreparedAsync(CancellationToken.None);
        }
    }

    private static RecoveryMappingContent? SavedOrAbsent(WindowsOverwriteAttempt? attempt)
    {
        return attempt?.Receipt.Artifacts.OfType<IdentifiedTransferArtifact>()
            .SingleOrDefault(artifact => artifact.Role == TransferArtifactRole.RecoveryRecord)?.Content as RecoveryMappingContent;
    }

    private static bool AtBoundary(RecoveryMappingContent saved, string boundary)
    {
        return boundary switch
        {
            "initialBeforeFlush" => saved.InitialFrame.Flush == RecoveryFlushObservation.NotAttempted,
            "completionBeforeFlush" => saved.Append is VerifiedRecoveryAppend before && before.Frame.Flush == RecoveryFlushObservation.NotAttempted,
            "completionAfterFlush" => saved.Append is VerifiedRecoveryAppend after && after.Frame.Flush == RecoveryFlushObservation.Confirmed,
            _ => throw new ArgumentOutOfRangeException(nameof(boundary)),
        };
    }
}
