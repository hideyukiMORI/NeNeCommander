using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves settings editor state and ordered complete-value save-on-change writes.</summary>
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
            BookmarkCatalog.Empty, PaneSortPreferences.Default);
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

    /// <summary>Proves catalog and preference revisions share one complete ordered queue.</summary>
    [TestMethod]
    public async Task SaveBookmarkCatalogWhenPreferenceFollowsPreservesBothInEveryRevisionAsync()
    {
        BookmarkCatalog initialCatalog = Catalog("Initial", "C:\\initial");
        BookmarkCatalog replacementCatalog = Catalog("Replacement", "C:\\replacement");
        UserSettings initial = UserSettings.Create(
            ColorScheme.Ubuntu,
            HiddenItemVisibility.Shown,
            initialCatalog, PaneSortPreferences.Default);
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

    /// <summary>Proves browse Cancel closes the sole modal without queuing persistence.</summary>
    [TestMethod]
    public async Task ApplyBookmarkEditorActionWhenBrowsingCancelClosesWithoutWriteAsync()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        SettingsSession session = new(store, SettingsReadOutcome.Absent(), static _ => { });
        _ = session.OpenBookmarks();

        await session.ApplyBookmarkEditorAction(
            BookmarkEditorAction.Cancel,
            new BookmarkRegistrationDefaults(string.Empty, string.Empty),
            new RecordingCommanderObserver(),
            CancellationToken.None);

        Assert.AreSame(SettingsEditorState.Closed, session.Current.Editor);
        Assert.AreSame(BookmarksEditorState.Closed, session.Current.BookmarksEditor);
        Assert.IsEmpty(store.Writes);
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
}
