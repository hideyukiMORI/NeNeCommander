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

/// <summary>Proves bookmark editing derives defaults and writes through the settings owner.</summary>
[TestClass]
public sealed class CommanderSessionBookmarkRegistrationTests
{
    /// <summary>Proves browse ignores unrelated commands and Escape closes the bookmark modal.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenBookmarksBrowseOwnsInputAcceptsOnlyItsClosedActionsAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession();
        RecordingCommanderObserver observer = new();
        _ = await session.HandleAsync(UserIntent.OpenBookmarks, observer, CancellationToken.None);

        CommanderSnapshot unchanged = await session.HandleAsync(
            UserIntent.Refresh,
            observer,
            CancellationToken.None);
        CommanderSnapshot closed = await session.HandleAsync(
            UserIntent.Escape,
            observer,
            CancellationToken.None);

        _ = Assert.IsInstanceOfType<BookmarksBrowsing>(unchanged.Settings.BookmarksEditor);
        Assert.AreSame(SettingsEditorState.Closed, closed.Settings.Editor);
        Assert.IsEmpty(left.Requests);
        Assert.IsEmpty(right.Requests);
    }

    /// <summary>Proves an absent location stays empty and a drive root uses its canonical path as its name.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenCurrentPaneHasNoValidLeafKeepsRegistrationDefaultsEmptyAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession absent = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession();
        RecordingCommanderObserver observer = new();
        _ = await absent.HandleAsync(UserIntent.OpenBookmarks, observer, CancellationToken.None);
        _ = await absent.HandleAsync(
            UserIntent.ManageBookmarks(BookmarkEditorAction.BeginAddBookmark),
            observer,
            CancellationToken.None);
        BookmarkDrafting absentDraft = Assert.IsInstanceOfType<BookmarkDrafting>(
            absent.Current.Settings.BookmarksEditor);
        Assert.AreEqual(string.Empty, absentDraft.Draft.Name);
        Assert.AreEqual(string.Empty, absentDraft.Draft.Path);

        ScriptedDirectoryReadPort root = ScriptedDirectoryReadPort.Create();
        root.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\", "item.txt"), null));
        using FileOperationGateway rootGateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession atRoot = new CommanderSessionTestComposition(root, ScriptedDirectoryReadPort.Create(), rootGateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession();
        _ = await atRoot.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\"), CancellationToken.None);
        _ = await atRoot.HandleAsync(UserIntent.OpenBookmarks, observer, CancellationToken.None);
        _ = await atRoot.HandleAsync(
            UserIntent.ManageBookmarks(BookmarkEditorAction.BeginAddBookmark),
            observer,
            CancellationToken.None);
        BookmarkDrafting rootDraft = Assert.IsInstanceOfType<BookmarkDrafting>(
            atRoot.Current.Settings.BookmarksEditor);
        Assert.AreEqual("C:\\", rootDraft.Draft.Name);
        Assert.AreEqual("C:\\", rootDraft.Draft.Path);
    }

    /// <summary>Proves the display-name boundary preserves the full folder path without saving settings.</summary>
    [TestMethod]
    [DataRow(128, 128)]
    [DataRow(129, 0)]
    public async Task HandleAsyncWhenFolderNameCrossesBookmarkLimitDerivesOnlyValidNameAsync(
        int leafLength,
        int expectedNameLength)
    {
        string path = "C:\\" + new string('a', leafLength);
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.EmptyListing(path), null));
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(
            left, ScriptedDirectoryReadPort.Create(), gateway, store).CreateSession();
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath(path), CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.OpenBookmarks, observer, CancellationToken.None);

        _ = await session.HandleAsync(
            UserIntent.ManageBookmarks(BookmarkEditorAction.BeginAddBookmark),
            observer,
            CancellationToken.None);

        BookmarkDrafting draft = Assert.IsInstanceOfType<BookmarkDrafting>(session.Current.Settings.BookmarksEditor);
        Assert.AreEqual(new string('a', expectedNameLength), draft.Draft.Name);
        Assert.AreEqual(path, draft.Draft.Path);
        Assert.AreSame(PaneSide.Left, session.Current.Panes.ActiveSide);
        Assert.HasCount(1, left.Requests);
        Assert.IsEmpty(store.Writes);
    }

    /// <summary>Proves a UNC share root offers its share name, the last segment before the trailing separator.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenCurrentPaneIsUncShareRootDefaultsToTheShareNameAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("\\\\server\\share\\", "item.txt"), null));
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, ScriptedDirectoryReadPort.Create(), gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession();
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("\\\\server\\share\\"), CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.OpenBookmarks, observer, CancellationToken.None);

        _ = await session.HandleAsync(
            UserIntent.ManageBookmarks(BookmarkEditorAction.BeginAddBookmark),
            observer,
            CancellationToken.None);

        BookmarkDrafting draft = Assert.IsInstanceOfType<BookmarkDrafting>(session.Current.Settings.BookmarksEditor);
        Assert.AreEqual("share", draft.Draft.Name);
        Assert.AreEqual("\\\\server\\share\\", draft.Draft.Path);
    }

    /// <summary>
    /// Proves the bookmark deletion question writes nothing and the confirmation queues exactly one
    /// complete catalog without the entry (ADR-0080).
    /// </summary>
    [TestMethod]
    public async Task HandleAsyncWhenBookmarkDeletionIsConfirmedQueuesOneCatalogOnlyAfterTheQuestionAsync()
    {
        BookmarkEntry entry = CommanderSessionTestValues.Entry("Target", "C:\\target", null);
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        TaskCompletionSource<SettingsWriteOutcome> write = store.PlanWrite();
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, store).CreateSession(SettingsReadOutcome.Read(UserSettings.Create(
            DisplayPreferences.Create(ColorScheme.NeNeDark, HiddenItemVisibility.Hidden, KeyHintVisibility.Shown), CommanderSessionTestValues.Catalog(entry), PaneSortPreferences.Default)));
        RecordingCommanderObserver observer = new();
        _ = await session.HandleAsync(UserIntent.OpenBookmarks, observer, CancellationToken.None);
        BookmarkCatalog before = session.Current.Settings.Settings.Bookmarks;

        CommanderSnapshot asking = await session.HandleAsync(
            UserIntent.ManageBookmarks(BookmarkEditorAction.BeginDeleteBookmark(new BookmarkSelection(entry))),
            observer,
            CancellationToken.None);

        _ = Assert.IsInstanceOfType<BookmarkDeleteConfirmation>(asking.Settings.BookmarksEditor);
        Assert.AreSame(before, asking.Settings.Settings.Bookmarks);
        Assert.IsEmpty(store.Writes);

        CommanderSnapshot deleted = await session.HandleAsync(
            UserIntent.ManageBookmarks(BookmarkEditorAction.ConfirmDeleteBookmark),
            observer,
            CancellationToken.None);

        Assert.HasCount(1, store.Writes);
        Assert.IsEmpty(store.Writes[0].Bookmarks.Bookmarks);
        Assert.IsEmpty(deleted.Settings.Settings.Bookmarks.Bookmarks);
        _ = Assert.IsInstanceOfType<BookmarksBrowsing>(deleted.Settings.BookmarksEditor);
        write.SetResult(SettingsWriteOutcome.Succeeded());
    }

    /// <summary>Proves bookmark Save uses pane-derived defaults and the sole settings queue.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenBookmarkDraftIsSavedQueuesOneCompleteCatalogAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\Current\\Folder", "old.txt"), null));
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        TaskCompletionSource<SettingsWriteOutcome> write = store.PlanWrite();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, store).CreateSession();
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(
            PaneSide.Left,
            CommanderSessionTestValues.ParsePath("C:\\Current\\Folder"),
            CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.OpenBookmarks, observer, CancellationToken.None);

        _ = await session.HandleAsync(
            UserIntent.ManageBookmarks(BookmarkEditorAction.BeginAddBookmark),
            observer,
            CancellationToken.None);
        BookmarkDrafting draft = Assert.IsInstanceOfType<BookmarkDrafting>(
            session.Current.Settings.BookmarksEditor);
        Assert.AreEqual("Folder", draft.Draft.Name);
        Assert.AreEqual("C:\\Current\\Folder", draft.Draft.Path);

        _ = await session.HandleAsync(
            UserIntent.ManageBookmarks(
                BookmarkEditorAction.UpdateBookmark(
                    new BookmarkDraft(
                        draft.Draft.Name,
                        draft.Draft.Path,
                        BookmarkCategoryFilter.Uncategorized,
                        BookmarkShortcutSlot.Two))),
            observer,
            CancellationToken.None);
        CommanderSnapshot saved = await session.HandleAsync(
            UserIntent.ManageBookmarks(BookmarkEditorAction.Save),
            observer,
            CancellationToken.None);

        AssertSavedBookmarkDraft(saved, store);
        write.SetResult(SettingsWriteOutcome.Succeeded());
        await session.StopAsync();
    }

    private static void AssertSavedBookmarkDraft(CommanderSnapshot saved, ScriptedSettingsStore store)
    {
        Assert.HasCount(1, store.Writes);
        Assert.HasCount(1, saved.Settings.Settings.Bookmarks.Bookmarks);
        Assert.AreEqual("Folder", saved.Settings.Settings.Bookmarks.Bookmarks[0].Name.Value);
        Assert.AreSame(
            BookmarkShortcutSlot.Two,
            saved.Settings.Settings.Bookmarks.Bookmarks[0].ShortcutSlot);
        _ = Assert.IsInstanceOfType<SettingsPersistencePending>(saved.Settings.Persistence);
    }
}
