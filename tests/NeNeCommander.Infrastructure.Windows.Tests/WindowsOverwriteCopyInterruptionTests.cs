using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves incomplete asynchronous writes and cancellation retain the same attempt's native observations.</summary>
[TestClass]
public sealed class WindowsOverwriteCopyInterruptionTests
{
    /// <summary>Native owners survive an incomplete inner write, and no result is returned before it finishes.</summary>
    [TestMethod]
    public async Task CopyAsyncWhenInnerWriteIsPendingKeepsHandlesUntilCompletion()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("desired", "original");
        FileEntrySnapshot source = WindowsOverwriteCopyTests.Snapshot(fixture);
        TransferOverwriteEvidence original = fixture.Pair.Evidence;
        fixture.Pair.Dispose();
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        WindowsTransferContentIo io = new(RandomAccess.ReadAsync, async (handle, bytes, offset, token) =>
        {
            _ = entered.TrySetResult();
            await release.Task;
            await RandomAccess.WriteAsync(handle, bytes, offset, token);
        }, RandomAccess.SetLength, RandomAccess.FlushToDisk);
        WindowsOverwriteCopy copy = WindowsOverwriteCopyTests.Create(new(), io, WindowsTransferArtifactRemoval.Native);
        Task<ProviderStepOutcome> operation = copy.CopyAsync(source, original, CancellationToken.None);
        _ = await Task.WhenAny(entered.Task, operation);
        bool wasPending = entered.Task.IsCompleted && !operation.IsCompleted;
        release.SetResult();

        ProviderStepOutcome outcome = await operation;

        Assert.IsTrue(wasPending);
        Assert.AreEqual(ProviderStepCompletion.Succeeded, outcome.Completion);
        Assert.AreEqual("desired", fixture.Read(original.Target.Observation.Entry.Path));
    }

    /// <summary>A partial target write is uncertain even when the native writer subsequently throws.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task CopyAsyncWhenTargetWriteThrowsRetainsChangedTargetAndArtifacts()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("desired", "original");
        FileEntrySnapshot source = WindowsOverwriteCopyTests.Snapshot(fixture);
        TransferOverwriteEvidence original = fixture.Pair.Evidence;
        fixture.Pair.Dispose();
        WindowsTransferContentIo io = TargetWriter(original, async (handle, bytes, offset, token) =>
        {
            await RandomAccess.WriteAsync(handle, bytes[..2], offset, token);
            throw new IOException("Injected failure after partial target write.");
        });
        WindowsOverwriteCopy copy = WindowsOverwriteCopyTests.Create(new(), io, WindowsTransferArtifactRemoval.Native);

        ProviderStepOutcome outcome = await copy.CopyAsync(source, original, CancellationToken.None);

        Assert.IsNotNull(outcome.Completion.Failure);
        AssertUncertain(outcome);
        Assert.AreEqual("deiginal", fixture.Read(original.Target.Observation.Entry.Path));
        Assert.AreEqual("desired", fixture.Read(source.Path));
    }

    /// <summary>Cancellation after a target effect returns the retained receipt rather than a cancelled task.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task CopyAsyncWhenTargetWriteCancelsRetainsChangedTargetAndSource()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("desired", "original");
        FileEntrySnapshot source = WindowsOverwriteCopyTests.Snapshot(fixture);
        TransferOverwriteEvidence original = fixture.Pair.Evidence;
        fixture.Pair.Dispose();
        using CancellationTokenSource cancellation = new();
        WindowsTransferContentIo io = TargetWriter(original, async (handle, bytes, offset, token) =>
        {
            await RandomAccess.WriteAsync(handle, bytes[..2], offset, token);
            cancellation.Cancel();
        });
        WindowsOverwriteCopy copy = WindowsOverwriteCopyTests.Create(new(), io, WindowsTransferArtifactRemoval.Native);

        ProviderStepOutcome outcome = await copy.CopyAsync(source, original, cancellation.Token);

        Assert.AreEqual(ProviderStepCompletion.Cancelled, outcome.Completion);
        AssertUncertain(outcome);
        Assert.AreEqual("deiginal", fixture.Read(original.Target.Observation.Entry.Path));
        Assert.AreEqual("desired", fixture.Read(source.Path));
    }

    /// <summary>Append flush failure keeps desired verification and all recovery objects but does not report success.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task CopyAsyncWhenCompletionFlushFailsRetainsDesiredTargetWithoutCleanup()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("desired", "original");
        FileEntrySnapshot source = WindowsOverwriteCopyTests.Snapshot(fixture);
        TransferOverwriteEvidence original = fixture.Pair.Evidence;
        fixture.Pair.Dispose();
        int flushes = 0;
        WindowsTransferContentIo io = new(RandomAccess.ReadAsync, RandomAccess.WriteAsync, RandomAccess.SetLength, handle =>
        {
            if (++flushes == 2)
            {
                throw new IOException("Injected completion flush failure.");
            }
            RandomAccess.FlushToDisk(handle);
        });
        WindowsOverwriteCopy copy = WindowsOverwriteCopyTests.Create(new(), io, WindowsTransferArtifactRemoval.Native);

        ProviderStepOutcome outcome = await copy.CopyAsync(source, original, CancellationToken.None);

        Assert.IsNotNull(outcome.Completion.Failure);
        _ = Assert.IsInstanceOfType<DesiredTargetContentVerified>(outcome.Receipt.Target);
        Assert.HasCount(3, outcome.Receipt.Artifacts);
        Assert.IsTrue(outcome.Receipt.Artifacts.Cast<IdentifiedTransferArtifact>().All(artifact => artifact.Link == ArtifactLinkObservation.Retained));
        Assert.AreEqual("desired", fixture.Read(source.Path));
    }

    private static WindowsTransferContentIo TargetWriter(TransferOverwriteEvidence original,
        Func<SafeFileHandle, ReadOnlyMemory<byte>, long, CancellationToken, ValueTask> write)
    {
        return new WindowsTransferContentIo(RandomAccess.ReadAsync, (handle, bytes, offset, token) =>
            WindowsTransferArtifactQueries.Native.ReadIdentity(handle) == original.Target.Observation.Entry.ObjectIdentity
                ? write(handle, bytes, offset, token) : RandomAccess.WriteAsync(handle, bytes, offset, token),
            RandomAccess.SetLength, RandomAccess.FlushToDisk);
    }

    private static void AssertUncertain(ProviderStepOutcome outcome)
    {
        TargetMayHaveChanged target = Assert.IsInstanceOfType<TargetMayHaveChanged>(outcome.Receipt.Target);
        Assert.AreEqual(TransferPathObservation.LastKnownPath, target.Target.PathObservation);
        Assert.HasCount(0, outcome.Receipt.Effects);
        Assert.HasCount(3, outcome.Receipt.Artifacts);
        Assert.IsTrue(outcome.Receipt.Artifacts.Cast<IdentifiedTransferArtifact>().All(artifact => artifact.Link == ArtifactLinkObservation.Retained));
    }
}
