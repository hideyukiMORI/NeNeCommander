using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves the settings session owns modal state and ordered save-on-change writes.</summary>
[TestClass]
public sealed class SettingsSessionTests
{
    /// <summary>Proves the successful write factory returns its closed success type.</summary>
    [TestMethod]
    public void SettingsWriteOutcomeSucceededWhenCalledReturnsSucceededOutcome()
    {
        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(SettingsWriteOutcome.Succeeded());
    }

    /// <summary>Proves a rejected startup document keeps defaults and a typed persistent warning.</summary>
    [TestMethod]
    public void ConstructWhenStartupDocumentWasRejectedKeepsDefaultAndRejection()
    {
        ScriptedSettingsStore store = new(
            SettingsReadOutcome.Rejected(SettingsReadFailureKind.Malformed));

        SettingsSnapshot snapshot = new SettingsSession(
            store,
            SettingsReadOutcome.Rejected(SettingsReadFailureKind.Malformed),
            static _ => { }).Current;

        Assert.AreSame(UserSettings.Default, snapshot.Settings);
        Assert.AreSame(SettingsEditorState.Closed, snapshot.Editor);
        Assert.AreSame(
            SettingsReadFailureKind.Malformed,
            Assert.IsInstanceOfType<SettingsPersistenceStartupRejected>(snapshot.Persistence).Failure);
    }

    /// <summary>Proves closing the editor never rolls back a selection already queued for save.</summary>
    [TestMethod]
    public async Task CloseWhenSelectionIsPendingKeepsTheSelectedSessionValueAsync()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        TaskCompletionSource<SettingsWriteOutcome> write = store.PlanWrite();
        SettingsSession session = new(store, SettingsReadOutcome.Absent(), static _ => { });
        RecordingCommanderObserver observer = new();
        _ = session.Open();

        Task pending = session.SelectColorSchemeAsync(
            ColorScheme.Dracula,
            observer,
            CancellationToken.None);
        SettingsSnapshot closed = session.Close();

        Assert.AreSame(ColorScheme.Dracula, closed.Settings.ColorScheme);
        Assert.AreSame(SettingsEditorState.Closed, closed.Editor);
        _ = Assert.IsInstanceOfType<SettingsPersistencePending>(closed.Persistence);
        write.SetResult(SettingsWriteOutcome.Succeeded());
        await pending;
        _ = Assert.IsInstanceOfType<SettingsPersistenceSucceeded>(session.Current.Persistence);
    }

    /// <summary>Proves the launch-hidden choice changes settings without mutating another field.</summary>
    [TestMethod]
    public async Task SelectLaunchHiddenVisibilityWhenChosenWritesCompleteSettingsAsync()
    {
        UserSettings initial = UserSettings.Create(
            ColorScheme.Monokai,
            HiddenItemVisibility.Hidden,
            BookmarkCatalog.Empty);
        ScriptedSettingsStore store = new(SettingsReadOutcome.Read(initial));
        TaskCompletionSource<SettingsWriteOutcome> write = store.PlanWrite();
        SettingsSession session = new(store, SettingsReadOutcome.Read(initial), static _ => { });
        RecordingCommanderObserver observer = new();

        Task pending = session.SelectLaunchHiddenItemVisibilityAsync(
            HiddenItemVisibility.Shown,
            observer,
            CancellationToken.None);

        Assert.AreSame(ColorScheme.Monokai, session.Current.Settings.ColorScheme);
        Assert.AreSame(HiddenItemVisibility.Shown, session.Current.Settings.HiddenItemVisibility);
        Assert.HasCount(1, store.Writes);
        Assert.AreSame(HiddenItemVisibility.Shown, store.Writes[0].HiddenItemVisibility);
        write.SetResult(SettingsWriteOutcome.Succeeded());
        await pending;
    }

    /// <summary>Proves writes start in intent order and an old completion cannot reverse newer state.</summary>
    [TestMethod]
    public async Task SelectWhenChangesOverlapWritesInOrderWithoutOldCompletionReversalAsync()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        TaskCompletionSource<SettingsWriteOutcome> firstWrite = store.PlanWrite();
        TaskCompletionSource<SettingsWriteOutcome> secondWrite = store.PlanWrite();
        SettingsSession session = new(store, SettingsReadOutcome.Absent(), static _ => { });
        RecordingCommanderObserver observer = new();

        Task first = session.SelectColorSchemeAsync(ColorScheme.Ubuntu, observer, CancellationToken.None);
        Task second = session.SelectColorSchemeAsync(ColorScheme.NeNeLight, observer, CancellationToken.None);

        Assert.HasCount(1, store.Writes);
        Assert.AreSame(ColorScheme.NeNeLight, session.Current.Settings.ColorScheme);
        firstWrite.SetResult(SettingsWriteOutcome.Succeeded());
        await first;
        await store.WaitForWriteCountAsync(2);
        Assert.AreSame(ColorScheme.NeNeLight, session.Current.Settings.ColorScheme);
        _ = Assert.IsInstanceOfType<SettingsPersistencePending>(session.Current.Persistence);
        secondWrite.SetResult(SettingsWriteOutcome.Rejected(
            SettingsWriteFailureKind.IoFailure,
            SettingsDirectoryEffect.NotAttempted,
            SettingsWriteEffect.None));
        await second;

        Assert.AreSame(ColorScheme.Ubuntu, store.Writes[0].ColorScheme);
        Assert.AreSame(ColorScheme.NeNeLight, store.Writes[1].ColorScheme);
        Assert.AreSame(ColorScheme.NeNeLight, session.Current.Settings.ColorScheme);
        SettingsPersistenceFailed failed = Assert.IsInstanceOfType<SettingsPersistenceFailed>(
            session.Current.Persistence);
        Assert.AreSame(SettingsWriteFailureKind.IoFailure, failed.Rejection.Failure);
    }

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

        Assert.AreSame(ColorScheme.Dracula, session.Current.Settings.ColorScheme);
    }

    /// <summary>Proves an unexpected store defect is observed once and cannot strand the next intent.</summary>
    [TestMethod]
    public async Task SelectWhenPriorWriteFaultsObservesDefectAndContinuesLatestIntentAsync()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        TaskCompletionSource<SettingsWriteOutcome> firstWrite = store.PlanWrite();
        TaskCompletionSource<SettingsWriteOutcome> secondWrite = store.PlanWrite();
        InvalidOperationException defect = new("Injected write defect.");
        List<Exception> observed = [];
        TaskCompletionSource observedSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        SettingsSession session = new(store, SettingsReadOutcome.Absent(), exception =>
        {
            observed.Add(exception);
            observedSignal.SetResult();
        });
        RecordingCommanderObserver observer = new();

        Task first = session.SelectColorSchemeAsync(ColorScheme.Ubuntu, observer, CancellationToken.None);
        Task second = session.SelectColorSchemeAsync(ColorScheme.NeNeLight, observer, CancellationToken.None);
        firstWrite.SetException(defect);

        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => first);
        await observedSignal.Task;
        await store.WaitForWriteCountAsync(2);
        secondWrite.SetResult(SettingsWriteOutcome.Succeeded());
        await second;

        Assert.HasCount(1, observed);
        Assert.AreSame(defect, observed[0]);
        Assert.AreSame(ColorScheme.NeNeLight, session.Current.Settings.ColorScheme);
        _ = Assert.IsInstanceOfType<SettingsPersistenceSucceeded>(session.Current.Persistence);
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

    /// <summary>Proves catalog and preference revisions share one complete ordered queue.</summary>
    [TestMethod]
    public async Task SaveBookmarkCatalogWhenPreferenceFollowsPreservesBothInEveryRevisionAsync()
    {
        BookmarkCatalog initialCatalog = Catalog("Initial", "C:\\initial");
        BookmarkCatalog replacementCatalog = Catalog("Replacement", "C:\\replacement");
        UserSettings initial = UserSettings.Create(
            ColorScheme.Ubuntu,
            HiddenItemVisibility.Shown,
            initialCatalog);
        ScriptedSettingsStore store = new(SettingsReadOutcome.Read(initial));
        TaskCompletionSource<SettingsWriteOutcome> catalogWrite = store.PlanWrite();
        TaskCompletionSource<SettingsWriteOutcome> preferenceWrite = store.PlanWrite();
        SettingsSession session = new(store, SettingsReadOutcome.Read(initial), static _ => { });
        RecordingCommanderObserver observer = new();

        Task first = session.SaveBookmarkCatalogAsync(
            replacementCatalog,
            observer,
            CancellationToken.None);
        Task second = session.SelectColorSchemeAsync(
            ColorScheme.Dracula,
            observer,
            CancellationToken.None);

        Assert.HasCount(1, store.Writes);
        Assert.AreSame(ColorScheme.Ubuntu, store.Writes[0].ColorScheme);
        Assert.AreSame(HiddenItemVisibility.Shown, store.Writes[0].HiddenItemVisibility);
        Assert.AreSame(replacementCatalog, store.Writes[0].Bookmarks);
        Assert.AreSame(ColorScheme.Dracula, session.Current.Settings.ColorScheme);
        Assert.AreSame(replacementCatalog, session.Current.Settings.Bookmarks);
        catalogWrite.SetResult(SettingsWriteOutcome.Succeeded());
        await first;
        await store.WaitForWriteCountAsync(2);
        Assert.AreSame(ColorScheme.Dracula, store.Writes[1].ColorScheme);
        Assert.AreSame(replacementCatalog, store.Writes[1].Bookmarks);
        preferenceWrite.SetResult(SettingsWriteOutcome.Succeeded());
        await second;
    }

    /// <summary>Proves a preference revision followed by a catalog save does not restore old metadata.</summary>
    [TestMethod]
    public async Task SaveBookmarkCatalogWhenPreferencePrecedesPreservesTheNewPreferenceAsync()
    {
        BookmarkCatalog replacementCatalog = Catalog("Replacement", "C:\\replacement");
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        TaskCompletionSource<SettingsWriteOutcome> preferenceWrite = store.PlanWrite();
        TaskCompletionSource<SettingsWriteOutcome> catalogWrite = store.PlanWrite();
        SettingsSession session = new(store, SettingsReadOutcome.Absent(), static _ => { });
        RecordingCommanderObserver observer = new();

        Task first = session.SelectLaunchHiddenItemVisibilityAsync(
            HiddenItemVisibility.Shown,
            observer,
            CancellationToken.None);
        Task second = session.SaveBookmarkCatalogAsync(
            replacementCatalog,
            observer,
            CancellationToken.None);

        Assert.HasCount(1, store.Writes);
        Assert.AreSame(BookmarkCatalog.Empty, store.Writes[0].Bookmarks);
        Assert.AreSame(HiddenItemVisibility.Shown, session.Current.Settings.HiddenItemVisibility);
        Assert.AreSame(replacementCatalog, session.Current.Settings.Bookmarks);
        preferenceWrite.SetResult(SettingsWriteOutcome.Succeeded());
        await first;
        await store.WaitForWriteCountAsync(2);
        Assert.AreSame(HiddenItemVisibility.Shown, store.Writes[1].HiddenItemVisibility);
        Assert.AreSame(replacementCatalog, store.Writes[1].Bookmarks);
        catalogWrite.SetResult(SettingsWriteOutcome.Succeeded());
        await second;
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

    private static BookmarkCatalog Catalog(string name, string path)
    {
        BookmarkDisplayName displayName = Assert.IsInstanceOfType<BookmarkDisplayNameAccepted>(
            BookmarkDisplayName.Parse(name)).Name;
        BookmarkPath bookmarkPath = Assert.IsInstanceOfType<BookmarkPathAccepted>(
            BookmarkPath.Parse(path)).Path;
        BookmarkEntry entry = BookmarkEntry.Create(displayName, bookmarkPath, null, null);
        return Assert.IsInstanceOfType<BookmarkCatalogAccepted>(
            BookmarkCatalog.Create([], [entry])).Catalog;
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
