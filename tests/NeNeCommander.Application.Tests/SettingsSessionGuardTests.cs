using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>
/// Proves the settings owner validates every internal entry before it routes, queues or checks
/// shutdown, closes both of its modals together, and reports each completed write.
/// </summary>
[TestClass]
public sealed class SettingsSessionGuardTests
{
    /// <summary>Proves closing the settings modal also closes the open bookmark editor.</summary>
    [TestMethod]
    public void CloseWhenBookmarksEditorIsOpenClosesTheBookmarksEditor()
    {
        SettingsSession session = new(new ScriptedSettingsStore(SettingsReadOutcome.Absent()), SettingsReadOutcome.Absent(), static _ => { });
        SettingsSnapshot opened = session.OpenBookmarks();

        SettingsSnapshot closed = session.Close();

        Assert.AreNotSame(BookmarksEditorState.Closed, opened.BookmarksEditor);
        Assert.AreSame(SettingsEditorState.Closed, closed.Editor);
        Assert.AreSame(BookmarksEditorState.Closed, closed.BookmarksEditor);
    }

    /// <summary>Proves a successful write reports the settled state to the observer that queued it.</summary>
    [TestMethod]
    public async Task SelectColorSchemeWhenWriteSucceedsReportsTheSucceededStateAsync()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        TaskCompletionSource<SettingsWriteOutcome> write = store.PlanWrite();
        SettingsSession session = new(store, SettingsReadOutcome.Absent(), static _ => { });
        RecordingCommanderObserver observer = new();

        Task pending = session.SelectColorSchemeAsync(ColorScheme.Monokai, observer, CancellationToken.None);
        write.SetResult(SettingsWriteOutcome.Succeeded());
        await pending;

        Assert.HasCount(1, observer.Settings);
        Assert.AreSame(ColorScheme.Monokai, observer.Settings[0].Settings.ColorScheme);
        _ = Assert.IsInstanceOfType<SettingsPersistenceSucceeded>(observer.Settings[0].Persistence);
    }

    /// <summary>Proves the editor route rejects an absent intent or observer before it closes or ignores input.</summary>
    [TestMethod]
    public void HandleEditorIntentWhenArgumentIsNullThrowsBeforeRouting()
    {
        SettingsSession session = new(new ScriptedSettingsStore(SettingsReadOutcome.Absent()), SettingsReadOutcome.Absent(), static _ => { });
        _ = session.Open();

        TransferConflictTestContext.AssertNullParameter(
            () => _ = session.HandleEditorIntentAsync(null!, new RecordingCommanderObserver(), CancellationToken.None),
            "intent");
        TransferConflictTestContext.AssertNullParameter(
            () => _ = session.HandleEditorIntentAsync(UserIntent.Escape, null!, CancellationToken.None),
            "observer");

        Assert.AreSame(SettingsEditorState.Open, session.Current.Editor);
    }

    /// <summary>Proves a sort-order save rejects each absent argument synchronously and queues no write.</summary>
    [TestMethod]
    public void SavePaneSortOrderWhenArgumentIsNullThrowsBeforeQueueing()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        SettingsSession session = new(store, SettingsReadOutcome.Absent(), static _ => { });
        RecordingCommanderObserver observer = new();

        TransferConflictTestContext.AssertNullParameter(
            () => _ = session.SavePaneSortOrderAsync(null!, PaneSortOrder.Default, observer, CancellationToken.None),
            "side");
        TransferConflictTestContext.AssertNullParameter(
            () => _ = session.SavePaneSortOrderAsync(PaneSide.Left, null!, observer, CancellationToken.None),
            "order");
        TransferConflictTestContext.AssertNullParameter(
            () => _ = session.SavePaneSortOrderAsync(PaneSide.Right, PaneSortOrder.Default, null!, CancellationToken.None),
            "observer");

        Assert.IsEmpty(store.Writes);
        Assert.AreSame(UserSettings.Default, session.Current.Settings);
    }

    /// <summary>Proves a catalog save names its absent catalog instead of the settings factory's parameter.</summary>
    [TestMethod]
    public void SaveBookmarkCatalogWhenCatalogIsNullNamesCatalog()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        SettingsSession session = new(store, SettingsReadOutcome.Absent(), static _ => { });

        TransferConflictTestContext.AssertNullParameter(
            () => _ = session.SaveBookmarkCatalogAsync(null!, new RecordingCommanderObserver(), CancellationToken.None),
            "catalog");

        Assert.IsEmpty(store.Writes);
    }

    /// <summary>Proves a stopped owner still reports an absent bookmark action or default as an argument defect.</summary>
    [TestMethod]
    public async Task ApplyBookmarkEditorActionAfterStopWhenArgumentIsNullThrowsArgumentNullAsync()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        SettingsSession session = new(store, SettingsReadOutcome.Absent(), static _ => { });
        _ = session.OpenBookmarks();
        await session.StopAsync();

        TransferConflictTestContext.AssertNullParameter(
            () => _ = session.ApplyBookmarkEditorAction(
                null!,
                new BookmarkRegistrationDefaults(string.Empty, string.Empty),
                new RecordingCommanderObserver(),
                CancellationToken.None),
            "action");
        TransferConflictTestContext.AssertNullParameter(
            () => _ = session.ApplyBookmarkEditorAction(
                BookmarkEditorAction.Cancel,
                null!,
                new RecordingCommanderObserver(),
                CancellationToken.None),
            "defaults");

        Assert.AreSame(SettingsEditorState.Bookmarks, session.Current.Editor);
        Assert.IsEmpty(store.Writes);
    }
}
