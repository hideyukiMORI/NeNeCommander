using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves settings shutdown drains owned writes and defect observers before closing admission.</summary>
[TestClass]
public sealed class SettingsSessionShutdownTests
{
    /// <summary>Proves shutdown waits for the last queued settings write.</summary>
    [TestMethod]
    public async Task StopAsyncWhenWriteIsPendingWaitsForCompletionAsync()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        TaskCompletionSource<SettingsWriteOutcome> write = store.PlanWrite();
        SettingsSession session = new(store, SettingsReadOutcome.Absent(), static _ => { });
        RecordingCommanderObserver observer = new();
        _ = session.SelectColorSchemeAsync(ColorScheme.SolarizedDark, observer, CancellationToken.None);

        Task stopping = session.StopAsync();

        Assert.IsFalse(stopping.IsCompleted);
        write.SetResult(SettingsWriteOutcome.Succeeded());
        await stopping;
        _ = Assert.IsInstanceOfType<SettingsPersistenceSucceeded>(session.Current.Persistence);
    }

    /// <summary>Proves shutdown includes a later intent queued while it awaits an earlier write.</summary>
    [TestMethod]
    public async Task StopAsyncWhenAnotherIntentQueuesDuringShutdownAwaitsTheNewTailAsync()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        TaskCompletionSource<SettingsWriteOutcome> firstWrite = store.PlanWrite();
        TaskCompletionSource<SettingsWriteOutcome> secondWrite = store.PlanWrite();
        SettingsSession session = new(store, SettingsReadOutcome.Absent(), static _ => { });
        RecordingCommanderObserver observer = new();
        _ = session.SelectColorSchemeAsync(ColorScheme.Ubuntu, observer, CancellationToken.None);
        Task stopping = session.StopAsync();
        _ = session.SelectColorSchemeAsync(ColorScheme.Dracula, observer, CancellationToken.None);

        firstWrite.SetResult(SettingsWriteOutcome.Succeeded());
        await store.WaitForWriteCountAsync(2);
        Assert.IsFalse(stopping.IsCompleted);
        secondWrite.SetResult(SettingsWriteOutcome.Succeeded());
        await stopping;

        Assert.AreSame(ColorScheme.Dracula, session.Current.Settings.Display.ColorScheme);
    }

    /// <summary>Proves shutdown observes a faulted tail and still completes without an unobserved task.</summary>
    [TestMethod]
    public async Task StopAsyncWhenOwnedWriteFaultsObservesDefectAndCompletesAsync()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        TaskCompletionSource<SettingsWriteOutcome> write = store.PlanWrite();
        InvalidOperationException defect = new("Injected shutdown defect.");
        TaskCompletionSource<Exception> observed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        SettingsSession session = new(store, SettingsReadOutcome.Absent(), observed.SetResult);
        _ = session.SelectColorSchemeAsync(
            ColorScheme.Dracula,
            new RecordingCommanderObserver(),
            CancellationToken.None);
        Task stopping = session.StopAsync();

        write.SetException(defect);
        await stopping;

        Assert.AreSame(defect, await observed.Task);
    }

    /// <summary>Proves shutdown cannot finish before the queue's defect observer finishes.</summary>
    [TestMethod]
    public async Task StopAsyncWhenDefectObservationIsPendingAwaitsTheObserverAsync()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        TaskCompletionSource<SettingsWriteOutcome> write = store.PlanWrite();
        TaskCompletionSource observerEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseObserver = new(TaskCreationOptions.RunContinuationsAsynchronously);
        SettingsSession session = new(store, SettingsReadOutcome.Absent(), exception =>
        {
            _ = exception;
            observerEntered.SetResult();
            SpinWait.SpinUntil(() => releaseObserver.Task.IsCompleted);
        });
        _ = session.SelectColorSchemeAsync(
            ColorScheme.Dracula,
            new RecordingCommanderObserver(),
            CancellationToken.None);
        Task stopping = session.StopAsync();

        write.SetException(new InvalidOperationException("Injected shutdown defect."));
        await observerEntered.Task;

        Assert.IsFalse(stopping.IsCompleted);
        releaseObserver.SetResult();
        await stopping;
    }

    /// <summary>Proves a throwing host observer escapes its raw callback and the queue still closes.</summary>
    [TestMethod]
    public async Task StopAsyncWhenDefectObserverThrowsSurfacesOnceAndCompletesQueueAsync()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        TaskCompletionSource<SettingsWriteOutcome> write = store.PlanWrite();
        InvalidOperationException primary = new("Injected write defect.");
        InvalidOperationException observerFailure = new("Injected host observer failure.");
        Exception? observed = null;
        int observationCount = 0;
        ControlledSynchronizationContext context = new();
        SettingsSession session = new(store, SettingsReadOutcome.Absent(), exception =>
        {
            observed = exception;
            observationCount++;
            throw observerFailure;
        });
        SynchronizationContext? original = SynchronizationContext.Current;
        Task selected;
        try
        {
            SynchronizationContext.SetSynchronizationContext(context);
            selected = session.SelectColorSchemeAsync(
                ColorScheme.Dracula,
                new RecordingCommanderObserver(),
                CancellationToken.None);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(original);
        }

        write.SetException(primary);
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => selected);
        Task stopping = session.StopAsync();
        for (int attempt = 0; attempt < 20 && context.PendingCount == 0 && !stopping.IsCompleted; attempt++)
        {
            await Task.Yield();
        }
        int pendingBeforeExecution = context.PendingCount;
        InvalidOperationException? escaped = context.TryExecuteOne();
        await stopping;

        Assert.AreEqual(1, pendingBeforeExecution);
        Assert.AreSame(observerFailure, escaped);
        Assert.AreSame(primary, observed);
        Assert.AreEqual(1, observationCount);
    }

    /// <summary>Proves a discarded cancelled write is owned, rendered, and awaited without a defect.</summary>
    [TestMethod]
    public async Task StopAsyncWhenQueuedWriteIsCancelledObservesTypedCancellationAsync()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        List<Exception> defects = [];
        RecordingCommanderObserver observer = new();
        SettingsSession session = new(store, SettingsReadOutcome.Absent(), defects.Add);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        _ = session.SelectColorSchemeAsync(ColorScheme.Monokai, observer, cancellation.Token);
        await session.StopAsync();

        _ = Assert.IsInstanceOfType<SettingsPersistenceCancelled>(session.Current.Persistence);
        Assert.HasCount(1, observer.Settings);
        Assert.IsEmpty(defects);
        Assert.IsEmpty(store.Writes);
    }

    /// <summary>Proves a stopped settings owner cannot start unowned persistence work.</summary>
    [TestMethod]
    public async Task SelectAfterStopRejectsBeforeStateOrIoChangesAsync()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        RecordingCommanderObserver observer = new();
        SettingsSession session = new(store, SettingsReadOutcome.Absent(), static _ => { });
        await session.StopAsync();
        SettingsSnapshot before = session.Current;

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
            session.SelectColorSchemeAsync(
                ColorScheme.Dracula,
                observer,
                CancellationToken.None));

        Assert.AreSame(before.Settings, session.Current.Settings);
        Assert.AreSame(before.Persistence, session.Current.Persistence);
        Assert.IsEmpty(store.Writes);
        Assert.IsEmpty(observer.Settings);
        await session.StopAsync();
    }

    /// <summary>Proves stopped settings ownership rejects bookmark actions before editor or I/O changes.</summary>
    [TestMethod]
    public async Task ApplyBookmarkEditorActionAfterStopRejectsBeforeStateOrIoChangesAsync()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        SettingsSession session = new(store, SettingsReadOutcome.Absent(), static _ => { });
        _ = session.OpenBookmarks();
        SettingsSnapshot before = session.Current;
        await session.StopAsync();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
            session.ApplyBookmarkEditorAction(
                BookmarkEditorAction.BeginAddBookmark,
                new BookmarkRegistrationDefaults("Folder", "C:\\Folder"),
                new RecordingCommanderObserver(),
                CancellationToken.None));

        Assert.AreSame(before.Settings, session.Current.Settings);
        Assert.AreEqual(before.BookmarksEditor, session.Current.BookmarksEditor);
        Assert.IsEmpty(store.Writes);
    }

    private sealed class ControlledSynchronizationContext : SynchronizationContext
    {
        private readonly Lock _sync = new();
        private readonly Queue<(SendOrPostCallback Callback, object? State)> _pending = [];

        internal int PendingCount
        {
            get
            {
                lock (_sync)
                {
                    return _pending.Count;
                }
            }
        }

        public override void Post(SendOrPostCallback callback, object? state)
        {
            lock (_sync)
            {
                _pending.Enqueue((callback, state));
            }
        }

        internal InvalidOperationException? TryExecuteOne()
        {
            (SendOrPostCallback Callback, object? State) work;
            lock (_sync)
            {
                if (_pending.Count == 0)
                {
                    return null;
                }
                work = _pending.Dequeue();
            }
            try
            {
                work.Callback(work.State);
                return null;
            }
            catch (InvalidOperationException exception)
            {
                return exception;
            }
        }
    }
}
