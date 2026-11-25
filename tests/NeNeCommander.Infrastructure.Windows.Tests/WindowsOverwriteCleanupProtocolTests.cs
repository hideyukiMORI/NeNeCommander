using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves cleanup consumes only successful completion and cannot reenter, replay or resume cancellation.</summary>
[TestClass]
public sealed class WindowsOverwriteCleanupProtocolTests
{
    /// <summary>Proves cancellation retains acknowledged removals without starting the next owned-link effect.</summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task CleanupAsyncWhenCancelledKeepsPartialAcknowledgementsAndForbidsReplayAsync(int cancelAfter)
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        using CancellationTokenSource cancellation = new();
        int calls = 0;
        bool Close(IntPtr handle)
        {
            bool closed = WindowsTransferArtifactRemoval.Native.Close(handle);
            if (++calls == cancelAfter)
            {
                cancellation.Cancel();
            }
            return closed;
        }
        WindowsTransferArtifactRemoval removal = new(WindowsTransferArtifactRemoval.Native.RequestDisposition, Close);
        using WindowsOverwriteAttempt attempt = files.Attempt(new DeterministicIdentifierSource(), WindowsTransferContentIo.Native, WindowsTransferArtifactQueries.Native, removal);
        _ = await WindowsOverwriteCleanupFixture.CompleteAsync(attempt);
        if (cancelAfter == 0)
        {
            cancellation.Cancel();
        }

        _ = await Assert.ThrowsAsync<OperationCanceledException>(() => attempt.CleanupAsync(cancellation.Token));
        Assert.AreEqual(cancelAfter, calls);
        WindowsOverwriteCleanupTests.RequirePartial(attempt, cancelAfter, ArtifactLinkObservation.Removed);
        Assert.AreEqual("source", files.Read(files.Pair.Source.Path));
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.CleanupAsync(CancellationToken.None));
    }

    /// <summary>Proves a second caller cannot enter while the first caller awaits a held read.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-014")]
    public async Task CleanupAsyncWhenVerificationIsAwaitingRejectsConcurrentEntryAsync()
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsOverwriteCleanupFixture calls = new();
        using WindowsOverwriteAttempt attempt = files.Attempt(new DeterministicIdentifierSource(), calls.Io, WindowsTransferArtifactQueries.Native);
        calls.Attempt = attempt;
        _ = await WindowsOverwriteCleanupFixture.CompleteAsync(attempt);
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
        calls.Read = async (handle, bytes, offset, token) =>
        {
            _ = entered.TrySetResult();
            await resume.Task;
            return await RandomAccess.ReadAsync(handle, bytes, offset, token);
        };
        Task first = attempt.CleanupAsync(CancellationToken.None);
        await entered.Task;
        try
        {
            _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.CleanupAsync(CancellationToken.None));
        }
        finally
        {
            resume.SetResult();
            await first;
        }
        WindowsOverwriteCleanupTests.RequirePartial(attempt, 3, ArtifactLinkObservation.Removed);
    }

    /// <summary>Proves new, merely prepared or merely verified attempts do not authorize artifact removal.</summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    public async Task CleanupAsyncWhenCompletionHasNotReturnedRefusesAsync(int stage)
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        int calls = 0;
        WindowsTransferArtifactRemoval removal = new(_ => { calls++; return 0; }, _ => throw new AssertFailedException("Removal was not admitted."));
        using WindowsOverwriteAttempt attempt = files.Attempt(new DeterministicIdentifierSource(), WindowsTransferContentIo.Native, WindowsTransferArtifactQueries.Native, removal);
        if (stage >= 1)
        {
            _ = await attempt.PrepareAsync(CancellationToken.None);
        }
        if (stage >= 2)
        {
            await attempt.OverwritePreparedAsync(CancellationToken.None);
        }

        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.CleanupAsync(CancellationToken.None));
        Assert.AreEqual(0, calls);
        Assert.AreEqual("source", files.Read(files.Pair.Source.Path));
        Assert.AreEqual(stage == 2 ? "source" : "target", files.Read(files.Pair.Target.Path));
    }
}
