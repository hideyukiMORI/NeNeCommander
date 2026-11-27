using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves held-handle transfer stages can finish without the caller context; flush success is injected, while read/write/truncate and close remain native.</summary>
[TestClass]
public sealed class WindowsOverwriteContinuationTests
{
    /// <summary>Every asynchronous acknowledgement is independently suspended; no timing, sleep or thread-pool luck chooses the tested boundary.</summary>
    [TestMethod]
    [DataRow("prepare")]
    [DataRow("overwrite")]
    [DataRow("append")]
    [DataRow("cleanup")]
    public async Task StageAsyncWhenEachIoAcknowledgementSuspendsCompletesWithoutCallingContextAsync(string stage)
    {
        int calls = await RunAsync(stage, 0);
        Assert.IsGreaterThan(0, calls);
        for (int suspension = 1; suspension <= calls; suspension++)
        {
            Assert.AreEqual(calls, await RunAsync(stage, suspension), $"The {stage} I/O transcript changed at suspension {suspension}.");
        }
    }

    private static async Task<int> RunAsync(string stage, int suspension)
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        TransferSuspensionIo io = new(_ => { });
        using WindowsOverwriteAttempt attempt = fixture.Attempt(new DeterministicIdentifierSource(), io.Io, WindowsTransferArtifactQueries.Native);
        await AdvanceBeforeAsync(attempt, stage);
        io.Arm(suspension);
        TransferContinuationContext context = new();
        Task operation = context.StartAsync(() => InvokeAsync(attempt, stage));
        try
        {
            Assert.AreEqual(suspension != 0, io.Suspended, $"The {stage} suspension was not reached.");
            if (suspension != 0)
            {
                Assert.IsFalse(operation.IsCompleted, "The operation returned before its suspended I/O acknowledgement.");
            }
        }
        finally
        {
            io.Release();
            await context.DrainAsync(operation);
        }
        Assert.AreEqual(0, context.Posts, $"The {stage} stage depended on its caller context at suspension {suspension}.");
        Assert.AreEqual("source", fixture.Read(fixture.Pair.Source.Path));
        Assert.AreEqual(stage == "prepare" ? "target" : "source", fixture.Read(fixture.Pair.Target.Path));
        if (stage == "prepare")
        {
            _ = Assert.IsInstanceOfType<UntouchedTransferTarget>(attempt.Receipt.Target);
        }
        else
        {
            _ = Assert.IsInstanceOfType<DesiredTargetContentVerified>(attempt.Receipt.Target);
        }
        if (stage == "cleanup")
        {
            Assert.IsTrue(attempt.Receipt.Artifacts.OfType<IdentifiedTransferArtifact>().All(artifact => artifact.Link == ArtifactLinkObservation.Removed));
        }
        Assert.AreEqual(stage is "prepare" or "overwrite" ? 1 : 2, io.Flushes);
        return io.Calls;
    }

    private static async Task AdvanceBeforeAsync(WindowsOverwriteAttempt attempt, string stage)
    {
        if (stage != "prepare")
        {
            _ = await attempt.PrepareAsync(CancellationToken.None);
        }
        if (stage is "append" or "cleanup")
        {
            await attempt.OverwritePreparedAsync(CancellationToken.None);
        }
        if (stage == "cleanup")
        {
            await attempt.AppendCompletionAsync(CancellationToken.None);
        }
    }

    private static Task InvokeAsync(WindowsOverwriteAttempt attempt, string stage)
    {
        return stage switch
        {
            "prepare" => attempt.PrepareAsync(CancellationToken.None),
            "overwrite" => attempt.OverwritePreparedAsync(CancellationToken.None),
            "append" => attempt.AppendCompletionAsync(CancellationToken.None),
            "cleanup" => attempt.CleanupAsync(CancellationToken.None),
            _ => throw new ArgumentOutOfRangeException(nameof(stage)),
        };
    }
}
