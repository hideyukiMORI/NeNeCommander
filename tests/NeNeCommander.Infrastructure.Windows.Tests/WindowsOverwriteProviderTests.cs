using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Exercises the existing provider entry points with exact overwrite consent and mandatory verification.</summary>
[TestClass]
public sealed class WindowsOverwriteProviderTests
{
    /// <summary>The adapter queues admission and waits for the inner asynchronous operation before completing.</summary>
    [TestMethod]
    public async Task CopyAsyncWhenQueuedWaitsForBothSchedulerAndInnerWrite()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("desired", "original");
        FileEntrySnapshot source = await OverwriteSnapshotPort.CaptureAsync(fixture);
        fixture.Pair.Dispose();
        QueuedScheduler scheduler = new();
        DeterministicIdentifierSource identifiers = new();
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        WindowsTransferContentIo io = new(RandomAccess.ReadAsync, async (handle, bytes, offset, token) =>
        {
            _ = entered.TrySetResult();
            await release.Task;
            await RandomAccess.WriteAsync(handle, bytes, offset, token);
        }, RandomAccess.SetLength, RandomAccess.FlushToDisk);
        WindowsLocalFileOperationAdapter adapter = Adapter(new(scheduler, scheduler), identifiers, io);
        Task<ProviderStepOutcome> pending = adapter.CopyAsync(source, fixture.Root.Path, CancellationToken.None);
        bool queued = !pending.IsCompleted && identifiers.IssuedCount == 0;
        scheduler.ExecuteAll();
        _ = await Task.WhenAny(entered.Task, pending);
        bool awaitingInner = entered.Task.IsCompleted && !pending.IsCompleted;
        release.SetResult();

        ProviderStepOutcome outcome = await pending;

        Assert.IsTrue(queued);
        Assert.IsTrue(awaitingInner);
        Assert.AreEqual(ProviderStepCompletion.Succeeded, outcome.Completion);
        Assert.AreEqual(1, identifiers.IssuedCount);
    }

    /// <summary>The canonical verifier produces strict evidence, and only the separate deletion step removes source.</summary>
    [TestMethod]
    public async Task VerifyCopyAsyncWhenOverwriteCompletesCarriesStrictRequirementsIntoDeletion()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("desired", "original");
        FileEntrySnapshot source = await OverwriteSnapshotPort.CaptureAsync(fixture);
        fixture.Pair.Dispose();
        WindowsLocalFileOperationAdapter adapter = new(new(), new DeterministicIdentifierSource());

        ProviderStepOutcome copy = await adapter.CopyAsync(source, fixture.Root.Path, CancellationToken.None);
        Assert.AreEqual(ProviderStepCompletion.Succeeded, copy.Completion);
        Assert.IsTrue(File.Exists(source.Path.CanonicalText));
        CopyVerified verified = Assert.IsInstanceOfType<CopyVerified>(await adapter.VerifyCopyAsync(source, fixture.Root.Path, CancellationToken.None));
        VerifiedCopyEntry.VerifiedFile file = Assert.IsInstanceOfType<VerifiedCopyEntry.VerifiedFile>(verified.Receipt.Root);
        Assert.AreSame(FileVerificationRequirements.OverwriteContents, file.Requirements);
        Assert.AreEqual(source.ConflictChoice!.Target.OverwriteEvidence!.Target.Observation.Entry.ObjectIdentity, file.Target.ObjectIdentity);
        ProviderStepOutcome deleted = await adapter.DeleteAsync(source, DeletionExecutionMode.VerifiedCopy(verified.Receipt), CancellationToken.None);

        Assert.AreEqual(ProviderStepCompletion.Succeeded, deleted.Completion);
        Assert.IsFalse(File.Exists(source.Path.CanonicalText));
        Assert.AreEqual("desired", fixture.Read(file.Target.Path));
    }

    /// <summary>Overwrite consent never admits the provider's ordinary atomic move entry point.</summary>
    [TestMethod]
    public async Task MoveAsyncWhenOverwriteIsConfirmedRefusesAtomicMoveWithoutEffects()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("desired", "original");
        FileEntrySnapshot source = await OverwriteSnapshotPort.CaptureAsync(fixture);
        fixture.Pair.Dispose();
        WindowsLocalFileOperationAdapter adapter = new(new(), new DeterministicIdentifierSource());

        AtomicMoveCapabilityOutcome capability = await adapter.GetAtomicMoveCapabilityAsync(source, fixture.Root.Path, CancellationToken.None);
        ProviderStepOutcome move = await adapter.MoveAsync(source, fixture.Root.Path, CancellationToken.None);

        Assert.AreSame(AtomicMoveCapabilityOutcome.Unsupported, capability);
        Assert.IsNotNull(move.Failure);
        Assert.HasCount(0, move.Receipt.Effects);
        Assert.AreEqual("desired", fixture.Read(source.Path));
        Assert.AreEqual("original", fixture.Read(source.TransferTarget!));
    }

    /// <summary>A new named stream cannot be hidden by passing only default-stream content verification.</summary>
    [TestMethod]
    [DataRow("source")]
    [DataRow("target")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    public async Task VerifyCopyAsyncWhenStreamIsAddedRefusesStrictEvidence(string side)
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("desired", "original");
        FileEntrySnapshot source = await OverwriteSnapshotPort.CaptureAsync(fixture);
        fixture.Pair.Dispose();
        WindowsLocalFileOperationAdapter adapter = new(new(), new DeterministicIdentifierSource());
        ProviderStepOutcome copy = await adapter.CopyAsync(source, fixture.Root.Path, CancellationToken.None);
        Assert.AreEqual(ProviderStepCompletion.Succeeded, copy.Completion);
        _ = fixture.Root.WriteFile(side + ":extra", string.Empty);

        CopyVerificationOutcome verification = await adapter.VerifyCopyAsync(source, fixture.Root.Path, CancellationToken.None);

        Assert.IsNotNull(verification.Failure);
        Assert.AreEqual("desired", fixture.Read(source.Path));
    }

    private static WindowsLocalFileOperationAdapter Adapter(WindowsLocalIoExecutionBoundary boundary,
        DeterministicIdentifierSource identifiers, WindowsTransferContentIo io)
    {
        WindowsDriveBoundary drives = new();
        return new WindowsLocalFileOperationAdapter(boundary, new WindowsLocalAdapterDependencies(
            new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }), new WindowsWslFileSystem(drives), drives,
            new WindowsOverwriteDependencies(identifiers, io, WindowsTransferArtifactQueries.Native, WindowsTransferArtifactRemoval.Native)));
    }

    private sealed class QueuedScheduler : IWindowsLocalIoScheduler
    {
        private readonly Queue<Action> _pending = new();

        internal void ExecuteAll()
        {
            while (_pending.Count > 0)
            {
                _pending.Dequeue()();
            }
        }

        public Task<TResult> ScheduleAsync<TResult>(Func<TResult> operation)
        {
            TaskCompletionSource<TResult> completion = new();
            _pending.Enqueue(() => completion.SetResult(operation()));
            return completion.Task;
        }
    }
}
