using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves bookmark navigation retains its selection and closes only after the owned read.</summary>
[TestClass]
public sealed class CommanderSessionBookmarkNavigationTests
{
    /// <summary>Proves successful manager navigation closes only after the existing read succeeds.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenManagerBookmarkSucceedsClosesAfterReadAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "old.txt"), null));
        BookmarkEntry entry = CommanderSessionTestValues.Entry("Target", "C:\\bookmark", null);
        BookmarkCatalog catalog = CommanderSessionTestValues.Catalog(entry);
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = CommanderSessionTestValues.CreateSessionWithCatalog(left, right, gateway, catalog);
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.OpenBookmarks, observer, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.BookmarkSlotOne, observer, CancellationToken.None);
        Assert.HasCount(1, left.Requests);
        TaskCompletionSource<DirectoryReadOutcome> read = left.EnqueuePending();
        Task<CommanderSnapshot> navigation = session.HandleAsync(
            UserIntent.NavigateBookmark(new BookmarkSelection(entry)),
            observer,
            CancellationToken.None);

        await AssertPendingBookmarkNavigationOwnsInputAsync(session, observer, entry);
        Assert.HasCount(2, left.Requests);
        read.SetResult(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\bookmark", "new.txt"), null));
        CommanderSnapshot navigated = await navigation;

        Assert.AreSame(SettingsEditorState.Closed, navigated.Settings.Editor);
        _ = Assert.IsInstanceOfType<BookmarksEditorClosed>(navigated.Settings.BookmarksEditor);
        Assert.AreEqual(
            "C:\\bookmark",
            Assert.IsInstanceOfType<PaneContentListed>(navigated.Panes.Left.Content)
                .Listing.Location.CanonicalText);

        _ = await session.HandleAsync(UserIntent.OpenBookmarks, observer, CancellationToken.None);
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\bookmark", "again.txt"), null));
        _ = await session.HandleAsync(
            UserIntent.NavigateBookmark(new BookmarkSelection(entry)),
            observer,
            CancellationToken.None);

        Assert.HasCount(3, left.Requests);
    }

    /// <summary>Proves failed manager navigation retains modal, selection metadata, and old listing.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenManagerBookmarkFailsKeepsManagerAndOldListingAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "old.txt"), null));
        left.Enqueue(DirectoryReadOutcome.Failed(FileOperationFailureKind.ProviderUnavailable));
        BookmarkEntry entry = CommanderSessionTestValues.Entry("Offline", "\\\\server\\share", null);
        BookmarkEntry other = CommanderSessionTestValues.Entry("Other", "C:\\other", null);
        BookmarkCatalog catalog = Assert.IsInstanceOfType<BookmarkCatalogAccepted>(
            BookmarkCatalog.Create([], [entry, other])).Catalog;
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, store).CreateSession(SettingsReadOutcome.Read(UserSettings.Create(
                DisplayPreferences.Create(ColorScheme.NeNeDark, HiddenItemVisibility.Hidden, KeyHintVisibility.Shown, InterfaceLanguage.System),
                catalog, PaneSortPreferences.Default, null)));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.OpenBookmarks, observer, CancellationToken.None);

        CommanderSnapshot failed = await session.HandleAsync(
            UserIntent.NavigateBookmark(new BookmarkSelection(entry)),
            observer,
            CancellationToken.None);

        BookmarkNavigationFailed navigationFailure = AssertRetainedBookmarkNavigationFailure(failed, catalog, entry);
        CommanderSnapshot craftedNavigation = await session.HandleAsync(
            UserIntent.NavigateBookmark(new BookmarkSelection(other)),
            observer,
            CancellationToken.None);

        Assert.AreSame(navigationFailure, craftedNavigation.Settings.BookmarksEditor);
        Assert.HasCount(2, left.Requests);

        CommanderSnapshot craftedDelete = await session.HandleAsync(
            UserIntent.ManageBookmarks(BookmarkEditorAction.BeginDeleteBookmark(new BookmarkSelection(entry))),
            observer,
            CancellationToken.None);

        Assert.AreSame(navigationFailure, craftedDelete.Settings.BookmarksEditor);
        Assert.AreSame(catalog, craftedDelete.Settings.Settings.Bookmarks);
        Assert.IsEmpty(store.Writes);
    }

    /// <summary>Proves a typed cancelled read remains distinguishable and retryable in the manager.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenManagerBookmarkReadIsCancelledKeepsTheTypedReasonAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Cancelled());
        BookmarkEntry entry = CommanderSessionTestValues.Entry("Target", "C:\\target", null);
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = CommanderSessionTestValues.CreateSessionWithCatalog(left, right, gateway, CommanderSessionTestValues.Catalog(entry));
        RecordingCommanderObserver observer = new();
        _ = await session.HandleAsync(UserIntent.OpenBookmarks, observer, CancellationToken.None);

        CommanderSnapshot cancelled = await session.HandleAsync(
            UserIntent.NavigateBookmark(new BookmarkSelection(entry)),
            observer,
            CancellationToken.None);

        BookmarkNavigationFailed failed = Assert.IsInstanceOfType<BookmarkNavigationFailed>(
            cancelled.Settings.BookmarksEditor);
        _ = Assert.IsInstanceOfType<PaneReadCancelled>(failed.Reason);
        Assert.AreSame(SettingsEditorState.Bookmarks, cancelled.Settings.Editor);
        Assert.HasCount(1, left.Requests);
    }

    /// <summary>
    /// Proves a read that ends by throwing cancellation still settles the manager as a cancelled
    /// navigation to the selected target before the cancellation reaches the caller.
    /// </summary>
    [TestMethod]
    public async Task HandleAsyncWhenManagerBookmarkReadThrowsCancellationRecordsCancelledTargetAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        _ = left.EnqueuePending().TrySetCanceled();
        BookmarkEntry entry = CommanderSessionTestValues.Entry("Target", "C:\\target", null);
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = CommanderSessionTestValues.CreateSessionWithCatalog(left, right, gateway, CommanderSessionTestValues.Catalog(entry));
        RecordingCommanderObserver observer = new();
        _ = await session.HandleAsync(UserIntent.OpenBookmarks, observer, CancellationToken.None);

        _ = await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => session.HandleAsync(
            UserIntent.NavigateBookmark(new BookmarkSelection(entry)),
            observer,
            CancellationToken.None));

        BookmarkNavigationFailed failed = Assert.IsInstanceOfType<BookmarkNavigationFailed>(
            session.Current.Settings.BookmarksEditor);
        PaneReadCancelled reason = Assert.IsInstanceOfType<PaneReadCancelled>(failed.Reason);
        Assert.AreEqual("C:\\target", reason.Target.CanonicalText);
        Assert.AreEqual(entry, failed.Selection.Entry);
    }

    /// <summary>Proves a stale displayed key cannot be rebound to a replacement path.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenManagerSelectionIsStaleRejectsWithoutReadAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        BookmarkEntry oldEntry = CommanderSessionTestValues.Entry("Target", "C:\\old", null);
        BookmarkEntry currentEntry = CommanderSessionTestValues.Entry("Target", "C:\\new", null);
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = CommanderSessionTestValues.CreateSessionWithCatalog(
            left,
            right,
            gateway,
            CommanderSessionTestValues.Catalog(currentEntry));
        RecordingCommanderObserver observer = new();
        _ = await session.HandleAsync(UserIntent.OpenBookmarks, observer, CancellationToken.None);

        CommanderSnapshot rejected = await session.HandleAsync(
            UserIntent.NavigateBookmark(new BookmarkSelection(oldEntry)),
            observer,
            CancellationToken.None);

        Assert.AreSame(SettingsEditorState.Bookmarks, rejected.Settings.Editor);
        Assert.IsEmpty(left.Requests);
        Assert.IsEmpty(right.Requests);
        Assert.AreEqual("C:\\new", rejected.Settings.Settings.Bookmarks.Bookmarks[0].Path.Value.CanonicalText);
        _ = Assert.IsInstanceOfType<BookmarksBrowsing>(rejected.Settings.BookmarksEditor);
    }

    /// <summary>Proves only a successful manager navigation changes the pane location history.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenManagerBookmarkNavigatesRecordsOnlySuccessInHistoryAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        BookmarkEntry entry = CommanderSessionTestValues.Entry("Target", "C:\\bookmark", null);
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "old.txt"), null));
        left.Enqueue(DirectoryReadOutcome.Failed(FileOperationFailureKind.ProviderUnavailable));
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\bookmark", "new.txt"), null));
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = CommanderSessionTestValues.CreateSessionWithCatalog(left, right, gateway, CommanderSessionTestValues.Catalog(entry));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.OpenBookmarks, observer, CancellationToken.None);

        CommanderSnapshot failed = await session.HandleAsync(
            UserIntent.NavigateBookmark(new BookmarkSelection(entry)),
            observer,
            CancellationToken.None);
        PaneNavigationHistory afterFailure = CommanderSessionTestValues.HistoryOf(failed);
        CommanderSnapshot navigated = await session.HandleAsync(
            UserIntent.NavigateBookmark(new BookmarkSelection(entry)),
            observer,
            CancellationToken.None);

        Assert.HasCount(1, afterFailure.Locations);
        Assert.AreEqual(0, afterFailure.CurrentIndex);
        Assert.AreEqual("C:\\left", afterFailure.Locations[0].CanonicalText);
        PaneNavigationHistory history = CommanderSessionTestValues.HistoryOf(navigated);
        Assert.HasCount(2, history.Locations);
        Assert.AreEqual(1, history.CurrentIndex);
        Assert.AreEqual("C:\\bookmark", history.Locations[1].CanonicalText);
    }

    private static async Task AssertPendingBookmarkNavigationOwnsInputAsync(CommanderSession session, RecordingCommanderObserver observer, BookmarkEntry entry)
    {
        Assert.AreSame(SettingsEditorState.Bookmarks, session.Current.Settings.Editor);
        _ = Assert.IsInstanceOfType<BookmarkNavigationPending>(
            session.Current.Settings.BookmarksEditor);
        CommanderSnapshot duplicate = await session.HandleAsync(
            UserIntent.NavigateBookmark(new BookmarkSelection(entry)),
            observer,
            CancellationToken.None);
        CommanderSnapshot escapeIgnored = await session.HandleAsync(
            UserIntent.Escape,
            observer,
            CancellationToken.None);
        CommanderSnapshot saveIgnored = await session.HandleAsync(
            UserIntent.ManageBookmarks(BookmarkEditorAction.Save),
            observer,
            CancellationToken.None);
        CommanderSnapshot settingsIgnored = await session.HandleAsync(
            UserIntent.OpenSettings,
            observer,
            CancellationToken.None);
        CommanderSnapshot paneIntentIgnored = await session.HandleAsync(
            UserIntent.Refresh,
            observer,
            CancellationToken.None);
        _ = Assert.IsInstanceOfType<BookmarkNavigationPending>(
            duplicate.Settings.BookmarksEditor);
        _ = Assert.IsInstanceOfType<BookmarkNavigationPending>(
            escapeIgnored.Settings.BookmarksEditor);
        _ = Assert.IsInstanceOfType<BookmarkNavigationPending>(
            saveIgnored.Settings.BookmarksEditor);
        _ = Assert.IsInstanceOfType<BookmarkNavigationPending>(
            settingsIgnored.Settings.BookmarksEditor);
        _ = Assert.IsInstanceOfType<BookmarkNavigationPending>(
            paneIntentIgnored.Settings.BookmarksEditor);
    }

    private static BookmarkNavigationFailed AssertRetainedBookmarkNavigationFailure(CommanderSnapshot failed, BookmarkCatalog catalog, BookmarkEntry entry)
    {
        Assert.AreSame(SettingsEditorState.Bookmarks, failed.Settings.Editor);
        Assert.AreSame(catalog, failed.Settings.Settings.Bookmarks);
        Assert.AreEqual(
            "C:\\left",
            Assert.IsInstanceOfType<PaneContentListed>(failed.Panes.Left.Content)
                .Listing.Location.CanonicalText);
        _ = Assert.IsInstanceOfType<PaneReadFailed>(failed.Panes.Left.Activity);
        BookmarkNavigationFailed navigationFailure =
            Assert.IsInstanceOfType<BookmarkNavigationFailed>(failed.Settings.BookmarksEditor);
        Assert.AreEqual(entry, navigationFailure.Selection.Entry);
        PaneReadFailed reason = Assert.IsInstanceOfType<PaneReadFailed>(navigationFailure.Reason);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, reason.Failure);
        return navigationFailure;
    }
}
