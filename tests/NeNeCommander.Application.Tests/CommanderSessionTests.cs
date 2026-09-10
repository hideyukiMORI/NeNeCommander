using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Commands;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves the application session coordinates settings modality without moving pane ownership.</summary>
[TestClass]
public sealed class CommanderSessionTests
{
    /// <summary>Proves an assigned fixed slot reads only the active pane through its existing port.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenBookmarkSlotIsAssignedNavigatesOnlyTheActivePaneAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "old.txt")));
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\bookmark", "new.txt")));
        using FileOperationGateway gateway = CreateGateway();
        BookmarkCatalog catalog = Catalog(
            Entry("Target", "C:\\bookmark", BookmarkShortcutSlot.One));
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()),
            SettingsReadOutcome.Read(UserSettings.Create(
                ColorScheme.NeNeDark,
                HiddenItemVisibility.Hidden,
                catalog)));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);

        CommanderSnapshot navigated = await session.HandleAsync(
            UserIntent.BookmarkSlotOne,
            observer,
            CancellationToken.None);

        Assert.HasCount(2, left.Requests);
        Assert.IsEmpty(right.Requests);
        Assert.AreEqual("C:\\bookmark", left.Requests[1].Location.CanonicalText);
        PaneContentListed content = Assert.IsInstanceOfType<PaneContentListed>(navigated.Panes.Left.Content);
        Assert.AreEqual("C:\\bookmark", content.Listing.Location.CanonicalText);
    }

    /// <summary>Proves an unassigned fixed slot is a metadata no-op with no filesystem read.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenBookmarkSlotIsUnassignedPerformsNoReadAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        using FileOperationGateway gateway = CreateGateway();
        BookmarkCatalog catalog = Catalog(
            Entry("Target", "C:\\target", BookmarkShortcutSlot.One));
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()),
            SettingsReadOutcome.Read(UserSettings.Create(
                ColorScheme.NeNeDark,
                HiddenItemVisibility.Hidden,
                catalog)));

        CommanderSnapshot unchanged = await session.HandleAsync(
            UserIntent.BookmarkSlotNine,
            new RecordingCommanderObserver(),
            CancellationToken.None);

        Assert.IsEmpty(left.Requests);
        Assert.IsEmpty(right.Requests);
        Assert.AreSame(PaneContent.Absent, unchanged.Panes.Left.Content);
    }

    /// <summary>Proves overlapping direct reads and unrelated intents share one navigation gate.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenDirectBookmarkReadIsPendingRejectsOverlappingWorkAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        TaskCompletionSource<DirectoryReadOutcome> read = left.EnqueuePending();
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSessionWithCatalog(
            left,
            right,
            gateway,
            Catalog(Entry("Target", "C:\\target", BookmarkShortcutSlot.One)));
        RecordingCommanderObserver observer = new();

        Task<CommanderSnapshot> navigation = session.HandleAsync(
            UserIntent.BookmarkSlotOne,
            observer,
            CancellationToken.None);
        CommanderSnapshot duplicate = await session.HandleAsync(
            UserIntent.BookmarkSlotOne,
            observer,
            CancellationToken.None);
        CommanderSnapshot settings = await session.HandleAsync(
            UserIntent.OpenSettings,
            observer,
            CancellationToken.None);
        CommanderSnapshot pane = await session.HandleAsync(
            UserIntent.Refresh,
            observer,
            CancellationToken.None);
        CommanderSnapshot explicitNavigation = await session.NavigateAsync(
            PaneSide.Right,
            ParsePath("C:\\other"),
            CancellationToken.None);

        Assert.HasCount(1, left.Requests);
        Assert.IsEmpty(right.Requests);
        Assert.AreSame(SettingsEditorState.Closed, duplicate.Settings.Editor);
        Assert.AreSame(SettingsEditorState.Closed, settings.Settings.Editor);
        Assert.AreSame(PaneContent.Absent, pane.Panes.Left.Content);
        Assert.AreSame(PaneContent.Absent, explicitNavigation.Panes.Right.Content);
        read.SetResult(DirectoryReadOutcome.Succeeded(Listing("C:\\target", "item.txt")));
        _ = await navigation;

        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\target", "next.txt")));
        _ = await session.HandleAsync(
            UserIntent.BookmarkSlotOne,
            observer,
            CancellationToken.None);

        Assert.HasCount(2, left.Requests);
    }

    /// <summary>Proves an unrelated left-pane read blocks a direct bookmark read.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenLeftPaneReadIsPendingRejectsDirectBookmarkAsync()
    {
        await AssertPendingPaneReadBlocksDirectBookmarkAsync(PaneSide.Left);
    }

    /// <summary>Proves an unrelated right-pane read blocks a direct bookmark read.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenRightPaneReadIsPendingRejectsDirectBookmarkAsync()
    {
        await AssertPendingPaneReadBlocksDirectBookmarkAsync(PaneSide.Right);
    }

    private static async Task AssertPendingPaneReadBlocksDirectBookmarkAsync(PaneSide side)
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort pendingPort = side == PaneSide.Left ? left : right;
        TaskCompletionSource<DirectoryReadOutcome> read = pendingPort.EnqueuePending();
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSessionWithCatalog(
            left,
            right,
            gateway,
            Catalog(Entry("Target", "C:\\target", BookmarkShortcutSlot.One)));
        Task<CommanderSnapshot> pending = session.NavigateAsync(
            side,
            ParsePath("C:\\pending"),
            CancellationToken.None);

        _ = await session.HandleAsync(
            UserIntent.BookmarkSlotOne,
            new RecordingCommanderObserver(),
            CancellationToken.None);

        Assert.HasCount(1, pendingPort.Requests);
        ScriptedDirectoryReadPort otherPort = side == PaneSide.Left ? right : left;
        Assert.IsEmpty(otherPort.Requests);
        read.SetResult(DirectoryReadOutcome.Succeeded(Listing("C:\\pending", "item.txt")));
        _ = await pending;
    }

    /// <summary>Proves successful manager navigation closes only after the existing read succeeds.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenManagerBookmarkSucceedsClosesAfterReadAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "old.txt")));
        BookmarkEntry entry = Entry("Target", "C:\\bookmark", null);
        BookmarkCatalog catalog = Catalog(entry);
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSessionWithCatalog(left, right, gateway, catalog);
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.OpenBookmarks, observer, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.BookmarkSlotOne, observer, CancellationToken.None);
        Assert.HasCount(1, left.Requests);
        TaskCompletionSource<DirectoryReadOutcome> read = left.EnqueuePending();
        Task<CommanderSnapshot> navigation = session.HandleAsync(
            UserIntent.NavigateBookmark(new BookmarkSelection(entry)),
            observer,
            CancellationToken.None);

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
        Assert.HasCount(2, left.Requests);
        read.SetResult(DirectoryReadOutcome.Succeeded(Listing("C:\\bookmark", "new.txt")));
        CommanderSnapshot navigated = await navigation;

        Assert.AreSame(SettingsEditorState.Closed, navigated.Settings.Editor);
        _ = Assert.IsInstanceOfType<BookmarksEditorClosed>(navigated.Settings.BookmarksEditor);
        Assert.AreEqual(
            "C:\\bookmark",
            Assert.IsInstanceOfType<PaneContentListed>(navigated.Panes.Left.Content)
                .Listing.Location.CanonicalText);

        _ = await session.HandleAsync(UserIntent.OpenBookmarks, observer, CancellationToken.None);
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\bookmark", "again.txt")));
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
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "old.txt")));
        left.Enqueue(DirectoryReadOutcome.Failed(FileOperationFailureKind.ProviderUnavailable));
        BookmarkEntry entry = Entry("Offline", "\\\\server\\share", null);
        BookmarkEntry other = Entry("Other", "C:\\other", null);
        BookmarkCatalog catalog = Assert.IsInstanceOfType<BookmarkCatalogAccepted>(
            BookmarkCatalog.Create([], [entry, other])).Catalog;
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            store,
            SettingsReadOutcome.Read(UserSettings.Create(
                ColorScheme.NeNeDark,
                HiddenItemVisibility.Hidden,
                catalog)));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.OpenBookmarks, observer, CancellationToken.None);

        CommanderSnapshot failed = await session.HandleAsync(
            UserIntent.NavigateBookmark(new BookmarkSelection(entry)),
            observer,
            CancellationToken.None);

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

        CommanderSnapshot craftedNavigation = await session.HandleAsync(
            UserIntent.NavigateBookmark(new BookmarkSelection(other)),
            observer,
            CancellationToken.None);

        Assert.AreSame(navigationFailure, craftedNavigation.Settings.BookmarksEditor);
        Assert.HasCount(2, left.Requests);

        CommanderSnapshot craftedDelete = await session.HandleAsync(
            UserIntent.ManageBookmarks(BookmarkEditorAction.DeleteBookmark(new BookmarkSelection(entry))),
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
        BookmarkEntry entry = Entry("Target", "C:\\target", null);
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSessionWithCatalog(left, right, gateway, Catalog(entry));
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

    /// <summary>Proves browse ignores unrelated commands and Escape closes the bookmark modal.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenBookmarksBrowseOwnsInputAcceptsOnlyItsClosedActionsAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()));
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

    /// <summary>Proves registration defaults do not invent names without one valid pane leaf.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenCurrentPaneHasNoValidLeafKeepsRegistrationDefaultsEmptyAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession absent = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()));
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
        root.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\", "item.txt")));
        using FileOperationGateway rootGateway = CreateGateway();
        CommanderSession atRoot = CreateSession(
            root,
            ScriptedDirectoryReadPort.Create(),
            rootGateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()));
        _ = await atRoot.NavigateAsync(PaneSide.Left, ParsePath("C:\\"), CancellationToken.None);
        _ = await atRoot.HandleAsync(UserIntent.OpenBookmarks, observer, CancellationToken.None);
        _ = await atRoot.HandleAsync(
            UserIntent.ManageBookmarks(BookmarkEditorAction.BeginAddBookmark),
            observer,
            CancellationToken.None);
        BookmarkDrafting rootDraft = Assert.IsInstanceOfType<BookmarkDrafting>(
            atRoot.Current.Settings.BookmarksEditor);
        Assert.AreEqual(string.Empty, rootDraft.Draft.Name);
        Assert.AreEqual("C:\\", rootDraft.Draft.Path);
    }

    /// <summary>Proves a stale displayed key cannot be rebound to a replacement path.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenManagerSelectionIsStaleRejectsWithoutReadAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        BookmarkEntry oldEntry = Entry("Target", "C:\\old", null);
        BookmarkEntry currentEntry = Entry("Target", "C:\\new", null);
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSessionWithCatalog(
            left,
            right,
            gateway,
            Catalog(currentEntry));
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

    /// <summary>Proves bookmark Save uses pane-derived defaults and the sole settings queue.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenBookmarkDraftIsSavedQueuesOneCompleteCatalogAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\Current\\Folder", "old.txt")));
        using FileOperationGateway gateway = CreateGateway();
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        TaskCompletionSource<SettingsWriteOutcome> write = store.PlanWrite();
        CommanderSession session = CreateSession(left, right, gateway, store);
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(
            PaneSide.Left,
            ParsePath("C:\\Current\\Folder"),
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

        Assert.HasCount(1, store.Writes);
        Assert.HasCount(1, saved.Settings.Settings.Bookmarks.Bookmarks);
        Assert.AreEqual("Folder", saved.Settings.Settings.Bookmarks.Bookmarks[0].Name.Value);
        Assert.AreSame(
            BookmarkShortcutSlot.Two,
            saved.Settings.Settings.Bookmarks.Bookmarks[0].ShortcutSlot);
        _ = Assert.IsInstanceOfType<SettingsPersistencePending>(saved.Settings.Persistence);
        write.SetResult(SettingsWriteOutcome.Succeeded());
        await session.StopAsync();
    }

    /// <summary>Proves keyboard and native-focus entry capture the intended listed pane.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenAddressEditingStartsCapturesAndActivatesRequestedPaneAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "left.txt")));
        right.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\right", "right.txt")));
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        _ = await session.NavigateAsync(PaneSide.Right, ParsePath("C:\\right"), CancellationToken.None);

        CommanderSnapshot leftEditing = await session.HandleAsync(
            UserIntent.FocusAddress,
            observer,
            CancellationToken.None);
        CommanderSnapshot repeatedShortcut = await session.HandleAsync(
            UserIntent.FocusAddress,
            observer,
            CancellationToken.None);
        CommanderSnapshot repeatedFocus = await session.HandleAsync(
            UserIntent.BeginAddressEdit(PaneSide.Left),
            observer,
            CancellationToken.None);
        CommanderSnapshot ignoredMovement = await session.HandleAsync(
            UserIntent.MoveNext,
            observer,
            CancellationToken.None);
        CommanderSnapshot rightEditing = await session.HandleAsync(
            UserIntent.BeginAddressEdit(PaneSide.Right),
            observer,
            CancellationToken.None);

        AddressEditing leftState = Assert.IsInstanceOfType<AddressEditing>(leftEditing.AddressEditor);
        Assert.AreSame(leftState, repeatedShortcut.AddressEditor);
        Assert.AreSame(leftState, repeatedFocus.AddressEditor);
        Assert.AreSame(leftState, ignoredMovement.AddressEditor);
        Assert.AreSame(PaneSide.Left, leftState.Side);
        Assert.AreEqual("C:\\left", leftState.OriginalLocation.CanonicalText);
        AddressEditing rightState = Assert.IsInstanceOfType<AddressEditing>(rightEditing.AddressEditor);
        Assert.AreSame(PaneSide.Right, rightState.Side);
        Assert.AreEqual("C:\\right", rightState.OriginalLocation.CanonicalText);
        Assert.AreSame(PaneSide.Right, rightEditing.Panes.ActiveSide);
    }

    /// <summary>Proves invalid raw input remains exact, performs no read, and preserves selection.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenAddressIsInvalidKeepsRawEditorAndPaneContentAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "item.txt")));
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.ToggleSelection, observer, CancellationToken.None);
        AddressEditorState editing = (await session.HandleAsync(
            UserIntent.FocusAddress,
            observer,
            CancellationToken.None)).AddressEditor;
        PaneContentListed before = Assert.IsInstanceOfType<PaneContentListed>(session.Current.Panes.Left.Content);
        const string RawText = " C:\\invalid ";

        CommanderSnapshot rejected = await session.HandleAsync(
            UserIntent.SubmitAddress(editing, RawText),
            observer,
            CancellationToken.None);

        AddressInputRejected state = Assert.IsInstanceOfType<AddressInputRejected>(rejected.AddressEditor);
        Assert.AreEqual(RawText, state.RawText);
        Assert.AreSame(PathParseFailureKind.Relative, state.Failure);
        Assert.HasCount(1, left.Requests);
        PaneContentListed after = Assert.IsInstanceOfType<PaneContentListed>(rejected.Panes.Left.Content);
        Assert.AreSame(before, after);
        Assert.HasCount(1, after.State.Selection);
    }

    /// <summary>Proves a valid submission closes before reading through the captured pane route.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenAddressIsValidClosesBeforeExistingNavigationCompletesAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "old.txt")));
        TaskCompletionSource<DirectoryReadOutcome> targetRead = left.EnqueuePending();
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        AddressEditorState editing = (await session.HandleAsync(
            UserIntent.FocusAddress,
            observer,
            CancellationToken.None)).AddressEditor;

        Task<CommanderSnapshot> navigating = session.HandleAsync(
            UserIntent.SubmitAddress(editing, "c:/target"),
            observer,
            CancellationToken.None);

        AddressEditorClosed closed = Assert.IsInstanceOfType<AddressEditorClosed>(session.Current.AddressEditor);
        Assert.AreSame(PaneSide.Left, closed.FileListFocusSide);
        PaneLoading loading = Assert.IsInstanceOfType<PaneLoading>(session.Current.Panes.Left.Activity);
        Assert.AreEqual("C:\\target", loading.Target.CanonicalText);
        Assert.HasCount(2, left.Requests);
        Assert.AreEqual("C:\\target", left.Requests[1].Location.CanonicalText);
        targetRead.SetResult(DirectoryReadOutcome.Succeeded(Listing("C:\\target", "new.txt")));
        CommanderSnapshot navigated = await navigating;
        PaneContentListed content = Assert.IsInstanceOfType<PaneContentListed>(navigated.Panes.Left.Content);
        Assert.AreEqual("C:\\target", content.Listing.Location.CanonicalText);
    }

    /// <summary>Proves Escape closes address editing without reaching pane selection reduction.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenEscapeCancelsAddressPreservesSelectionAndRequestsListFocusAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "item.txt")));
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.ToggleSelection, observer, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.FocusAddress, observer, CancellationToken.None);

        CommanderSnapshot cancelled = await session.HandleAsync(
            UserIntent.Escape,
            observer,
            CancellationToken.None);

        AddressEditorClosed closed = Assert.IsInstanceOfType<AddressEditorClosed>(cancelled.AddressEditor);
        Assert.AreSame(PaneSide.Left, closed.FileListFocusSide);
        PaneContentListed content = Assert.IsInstanceOfType<PaneContentListed>(cancelled.Panes.Left.Content);
        Assert.AreEqual("C:\\left", content.Listing.Location.CanonicalText);
        Assert.HasCount(1, content.State.Selection);
    }

    /// <summary>Proves an old control's LostFocus cannot close a newer address editor.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenStaleAddressDepartureArrivesKeepsNewEditorAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "left.txt")));
        right.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\right", "right.txt")));
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        _ = await session.NavigateAsync(PaneSide.Right, ParsePath("C:\\right"), CancellationToken.None);
        AddressEditorState oldState = (await session.HandleAsync(
            UserIntent.FocusAddress,
            observer,
            CancellationToken.None)).AddressEditor;
        AddressEditorState newState = (await session.HandleAsync(
            UserIntent.BeginAddressEdit(PaneSide.Right),
            observer,
            CancellationToken.None)).AddressEditor;

        CommanderSnapshot afterDeparture = await session.HandleAsync(
            UserIntent.LeaveAddress(oldState),
            observer,
            CancellationToken.None);

        Assert.AreSame(newState, afterDeparture.AddressEditor);
        Assert.AreSame(PaneSide.Right, Assert.IsInstanceOfType<AddressEditing>(newState).Side);

        CommanderSnapshot departed = await session.HandleAsync(
            UserIntent.LeaveAddress(newState),
            observer,
            CancellationToken.None);
        Assert.AreSame(AddressEditorState.Closed, departed.AddressEditor);
    }

    /// <summary>Proves provider failure and cancellation retain the listed content and selection.</summary>
    [TestMethod]
    [DataRow("failed")]
    [DataRow("cancelled")]
    public async Task HandleAsyncWhenAddressReadDoesNotSucceedPreservesPaneAsync(string outcomeName)
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "item.txt")));
        bool cancelled = outcomeName == "cancelled";
        left.Enqueue(cancelled
            ? DirectoryReadOutcome.Cancelled()
            : DirectoryReadOutcome.Failed(FileOperationFailureKind.AccessDenied));
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.ToggleSelection, observer, CancellationToken.None);
        PaneContentListed before = Assert.IsInstanceOfType<PaneContentListed>(session.Current.Panes.Left.Content);
        AddressEditorState editing = (await session.HandleAsync(
            UserIntent.FocusAddress,
            observer,
            CancellationToken.None)).AddressEditor;

        CommanderSnapshot completed = await session.HandleAsync(
            UserIntent.SubmitAddress(editing, "C:\\target"),
            observer,
            CancellationToken.None);

        PaneContentListed after = Assert.IsInstanceOfType<PaneContentListed>(completed.Panes.Left.Content);
        Assert.AreSame(before, after);
        Assert.HasCount(1, after.State.Selection);
        if (cancelled)
        {
            _ = Assert.IsInstanceOfType<PaneReadCancelled>(completed.Panes.Left.Activity);
        }
        else
        {
            PaneReadFailed failure = Assert.IsInstanceOfType<PaneReadFailed>(completed.Panes.Left.Activity);
            Assert.AreSame(FileOperationFailureKind.AccessDenied, failure.Failure);
        }
        AddressEditorClosed closed = Assert.IsInstanceOfType<AddressEditorClosed>(completed.AddressEditor);
        Assert.AreSame(PaneSide.Left, closed.FileListFocusSide);
    }

    /// <summary>Proves any pane read in flight refuses address entry without changing activation.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaneReadIsRunningRefusesAddressEntryAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        right.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\right", "item.txt")));
        TaskCompletionSource<DirectoryReadOutcome> leftRead = left.EnqueuePending();
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Right, ParsePath("C:\\right"), CancellationToken.None);
        Task<CommanderSnapshot> loading = session.NavigateAsync(
            PaneSide.Left,
            ParsePath("C:\\loading"),
            CancellationToken.None);

        CommanderSnapshot refused = await session.HandleAsync(
            UserIntent.BeginAddressEdit(PaneSide.Right),
            observer,
            CancellationToken.None);

        Assert.AreSame(AddressEditorState.Closed, refused.AddressEditor);
        Assert.AreSame(PaneSide.Left, refused.Panes.ActiveSide);
        leftRead.SetResult(DirectoryReadOutcome.Cancelled());
        _ = await loading;
    }

    /// <summary>Proves an operation modal keeps ownership over a requested address editor.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenOperationModalOwnsInputRefusesAddressEntryAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "item.txt")));
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.Rename, observer, CancellationToken.None);

        CommanderSnapshot refused = await session.HandleAsync(
            UserIntent.FocusAddress,
            observer,
            CancellationToken.None);

        _ = Assert.IsInstanceOfType<OperationAwaitingName>(refused.Panes.Operation);
        Assert.AreSame(AddressEditorState.Closed, refused.AddressEditor);
    }

    /// <summary>Proves a submission qualified by an old editor cannot navigate either pane.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenStaleAddressSubmissionArrivesPerformsNoReadAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "left.txt")));
        right.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\right", "right.txt")));
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        _ = await session.NavigateAsync(PaneSide.Right, ParsePath("C:\\right"), CancellationToken.None);
        AddressEditorState oldState = (await session.HandleAsync(
            UserIntent.FocusAddress,
            observer,
            CancellationToken.None)).AddressEditor;
        AddressEditorState newState = (await session.HandleAsync(
            UserIntent.BeginAddressEdit(PaneSide.Right),
            observer,
            CancellationToken.None)).AddressEditor;

        CommanderSnapshot refused = await session.HandleAsync(
            UserIntent.SubmitAddress(oldState, "C:\\target"),
            observer,
            CancellationToken.None);

        Assert.AreSame(newState, refused.AddressEditor);
        Assert.HasCount(1, left.Requests);
        Assert.HasCount(1, right.Requests);
    }

    /// <summary>Proves correction after rejection keeps the captured side and uses normal navigation.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenRejectedAddressIsCorrectedNavigatesCapturedPaneAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "old.txt")));
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\corrected", "new.txt")));
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        AddressEditorState editing = (await session.HandleAsync(
            UserIntent.FocusAddress,
            observer,
            CancellationToken.None)).AddressEditor;
        AddressEditorState rejected = (await session.HandleAsync(
            UserIntent.SubmitAddress(editing, "relative"),
            observer,
            CancellationToken.None)).AddressEditor;
        CommanderSnapshot repeated = await session.HandleAsync(
            UserIntent.BeginAddressEdit(PaneSide.Left),
            observer,
            CancellationToken.None);

        CommanderSnapshot corrected = await session.HandleAsync(
            UserIntent.SubmitAddress(rejected, "C:\\corrected"),
            observer,
            CancellationToken.None);

        Assert.AreSame(rejected, repeated.AddressEditor);
        PaneContentListed content = Assert.IsInstanceOfType<PaneContentListed>(corrected.Panes.Left.Content);
        Assert.AreEqual("C:\\corrected", content.Listing.Location.CanonicalText);
        Assert.HasCount(2, left.Requests);
    }

    /// <summary>Proves an address editor cannot start before its pane has listed a location.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenActivePaneHasNoListingRefusesAddressEntryAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()));

        CommanderSnapshot refused = await session.HandleAsync(
            UserIntent.FocusAddress,
            new RecordingCommanderObserver(),
            CancellationToken.None);

        Assert.AreSame(AddressEditorState.Closed, refused.AddressEditor);
        Assert.IsEmpty(left.Requests);
        Assert.IsEmpty(right.Requests);
    }

    /// <summary>Proves opening settings freezes both pane navigation paths until Escape closes it.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenSettingsAreOpenFreezesPanesUntilEscapeAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSession(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent()));
        RecordingCommanderObserver observer = new();

        CommanderSnapshot opened = await session.HandleAsync(
            UserIntent.OpenSettings,
            observer,
            CancellationToken.None);
        CommanderSnapshot frozen = await session.NavigateAsync(
            PaneSide.Left,
            ParsePath("C:\\frozen"),
            CancellationToken.None);

        Assert.AreSame(SettingsEditorState.Open, opened.Settings.Editor);
        Assert.AreSame(SettingsEditorState.Open, frozen.Settings.Editor);
        Assert.IsEmpty(left.Requests);

        CommanderSnapshot closed = await session.HandleAsync(
            UserIntent.Escape,
            observer,
            CancellationToken.None);
        Assert.AreSame(SettingsEditorState.Closed, closed.Settings.Editor);
    }

    /// <summary>Proves a typed settings selection saves through the settings owner and not a pane transition.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenLaunchDefaultChangesKeepsCurrentPaneStateAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        using FileOperationGateway gateway = CreateGateway();
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        TaskCompletionSource<SettingsWriteOutcome> write = store.PlanWrite();
        CommanderSession session = CreateSession(left, right, gateway, store);
        RecordingCommanderObserver observer = new();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "item.txt")));
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.OpenSettings, observer, CancellationToken.None);

        Task<CommanderSnapshot> changing = session.HandleAsync(
            UserIntent.SelectLaunchHiddenItemVisibility(HiddenItemVisibility.Shown),
            observer,
            CancellationToken.None);

        Assert.AreSame(HiddenItemVisibility.Shown, session.Current.Settings.Settings.HiddenItemVisibility);
        PaneContentListed leftContent = Assert.IsInstanceOfType<PaneContentListed>(
            session.Current.Panes.Left.Content);
        Assert.AreSame(HiddenItemVisibility.Hidden, leftContent.State.HiddenItemVisibility);
        Assert.AreSame(PaneContent.Absent, session.Current.Panes.Right.Content);
        CommanderSnapshot changed = await changing;
        _ = Assert.IsInstanceOfType<SettingsPersistencePending>(changed.Settings.Persistence);
        write.SetResult(SettingsWriteOutcome.Succeeded());
        await session.StopAsync();
        _ = Assert.IsInstanceOfType<SettingsPersistenceSucceeded>(session.Current.Settings.Persistence);
    }

    /// <summary>Proves an existing name modal keeps ownership when the settings shortcut arrives.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenNameModalOwnsInputRefusesToOpenSettingsAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "item.txt")));
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        CommanderSnapshot awaitingName = await session.HandleAsync(
            UserIntent.Rename,
            observer,
            CancellationToken.None);

        CommanderSnapshot refused = await session.HandleAsync(
            UserIntent.OpenSettings,
            observer,
            CancellationToken.None);

        _ = Assert.IsInstanceOfType<OperationAwaitingName>(awaitingName.Panes.Operation);
        _ = Assert.IsInstanceOfType<OperationAwaitingName>(refused.Panes.Operation);
        Assert.AreSame(SettingsEditorState.Closed, refused.Settings.Editor);
    }

    /// <summary>Proves the transfer-conflict modal keeps ownership when the settings shortcut arrives.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenConflictModalOwnsInputRefusesToOpenSettingsAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        using FileOperationGateway gateway = new(port);
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()));
        RecordingCommanderObserver observer = new();
        DirectoryListing leftListing = Listing("C:\\left", "item.txt");
        DirectoryListing rightListing = Listing("C:\\right", "item.txt");
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing));
        _ = await session.NavigateAsync(PaneSide.Left, leftListing.Location, CancellationToken.None);
        _ = await session.NavigateAsync(PaneSide.Right, rightListing.Location, CancellationToken.None);
        FileInspectionSucceeded inspection = Assert.IsInstanceOfType<FileInspectionSucceeded>(
            Inspection(leftListing.Entries[0].Path));
        TransferConflict conflict = TransferConflict.Create(
            inspection.Snapshot,
            rightListing.Entries[0].Path,
            ParsePath("C:\\right\\item (2).txt"));
        port.EnqueueInspection(inspection);
        port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([conflict]));
        _ = await session.HandleAsync(UserIntent.Copy, observer, CancellationToken.None);

        CommanderSnapshot refused = await session.HandleAsync(
            UserIntent.OpenSettings,
            observer,
            CancellationToken.None);

        _ = Assert.IsInstanceOfType<OperationAwaitingConflict>(refused.Panes.Operation);
        Assert.AreSame(SettingsEditorState.Closed, refused.Settings.Editor);
    }

    /// <summary>Proves rapid selector intents return after enqueue and persist in their canonical order.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenSettingsSelectionsOverlapEnqueuesBothWithoutAwaitingIoAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        using FileOperationGateway gateway = CreateGateway();
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        TaskCompletionSource<SettingsWriteOutcome> firstWrite = store.PlanWrite();
        TaskCompletionSource<SettingsWriteOutcome> secondWrite = store.PlanWrite();
        CommanderSession session = CreateSession(left, right, gateway, store);
        RecordingCommanderObserver observer = new();

        _ = await session.HandleAsync(UserIntent.OpenSettings, observer, CancellationToken.None);
        CommanderSnapshot ignored = await session.HandleAsync(
            UserIntent.Refresh,
            observer,
            CancellationToken.None);
        Task<CommanderSnapshot> first = session.HandleAsync(
            UserIntent.SelectColorScheme(ColorScheme.Ubuntu),
            observer,
            CancellationToken.None);
        Task<CommanderSnapshot> second = session.HandleAsync(
            UserIntent.SelectColorScheme(ColorScheme.Dracula),
            observer,
            CancellationToken.None);

        Assert.IsTrue(first.IsCompletedSuccessfully);
        Assert.IsTrue(second.IsCompletedSuccessfully);
        Assert.AreSame(SettingsEditorState.Open, ignored.Settings.Editor);
        Assert.IsEmpty(observer.Settings);
        Assert.HasCount(1, store.Writes);
        Assert.AreSame(ColorScheme.Dracula, session.Current.Settings.Settings.ColorScheme);
        firstWrite.SetResult(SettingsWriteOutcome.Succeeded());
        await store.WaitForWriteCountAsync(2);
        Assert.AreSame(ColorScheme.Dracula, session.Current.Settings.Settings.ColorScheme);
        secondWrite.SetResult(SettingsWriteOutcome.Succeeded());
        await session.StopAsync();
        Assert.AreSame(ColorScheme.Ubuntu, store.Writes[0].ColorScheme);
        Assert.AreSame(ColorScheme.Dracula, store.Writes[1].ColorScheme);
    }

    /// <summary>Proves the palette exposes only the approved stable intent subset and state reasons.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaletteOpensCapturesStableCatalogAndAvailabilityAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "item.txt")));
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()));
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);

        CommanderSnapshot opened = await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            new RecordingCommanderObserver(),
            CancellationToken.None);

        CommandPaletteOpen palette = Assert.IsInstanceOfType<CommandPaletteOpen>(opened.CommandPalette);
        CollectionAssert.AreEqual(
            new UserIntent[]
            {
                UserIntent.OpenFocused,
                UserIntent.NavigateParent,
                UserIntent.NavigateBack,
                UserIntent.NavigateForward,
                UserIntent.Refresh,
                UserIntent.FocusAddress,
                UserIntent.Rename,
                UserIntent.Copy,
                UserIntent.Move,
                UserIntent.CreateDirectory,
                UserIntent.Delete,
                UserIntent.ToggleSelection,
                UserIntent.ToggleHiddenItems,
                UserIntent.ActivateOtherPane,
                UserIntent.OpenSettings,
            },
            palette.Candidates.Select(candidate => candidate.Intent).ToArray());
        Assert.AreSame(CommandAvailability.Available, Candidate(palette, UserIntent.OpenFocused).Availability);
        Assert.AreSame(
            CommandAvailability.Available,
            Candidate(palette, UserIntent.NavigateParent).Availability);
        Assert.AreEqual(
            CommandUnavailableReason.PassivePaneUnavailable,
            Assert.IsInstanceOfType<CommandUnavailable>(Candidate(palette, UserIntent.Copy).Availability).Reason);
        Assert.AreEqual(
            CommandUnavailableReason.BackUnavailable,
            Assert.IsInstanceOfType<CommandUnavailable>(Candidate(palette, UserIntent.NavigateBack).Availability).Reason);
        Assert.AreEqual(
            CommandUnavailableReason.ForwardUnavailable,
            Assert.IsInstanceOfType<CommandUnavailable>(Candidate(palette, UserIntent.NavigateForward).Availability).Reason);
        Assert.AreSame(opened.Panes.Left, palette.Left);
        Assert.AreSame(opened.Panes.Right, palette.Right);
        Assert.AreSame(PaneSide.Left, palette.ActiveSide);
    }

    /// <summary>Proves every no-source and root-navigation reason is captured without preflight.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaletteOpensOnEmptyRootCapturesUnavailableReasonsAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(EmptyListing("C:\\")));
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()));
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\"), CancellationToken.None);

        CommandPaletteOpen palette = Assert.IsInstanceOfType<CommandPaletteOpen>((await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            new RecordingCommanderObserver(),
            CancellationToken.None)).CommandPalette);

        AssertUnavailable(palette, UserIntent.OpenFocused, CommandUnavailableReason.FocusRequired);
        AssertUnavailable(palette, UserIntent.Rename, CommandUnavailableReason.FocusRequired);
        AssertUnavailable(palette, UserIntent.ToggleSelection, CommandUnavailableReason.FocusRequired);
        AssertUnavailable(palette, UserIntent.Copy, CommandUnavailableReason.SourceRequired);
        AssertUnavailable(palette, UserIntent.Move, CommandUnavailableReason.SourceRequired);
        AssertUnavailable(palette, UserIntent.Delete, CommandUnavailableReason.SourceRequired);
        AssertUnavailable(palette, UserIntent.NavigateParent, CommandUnavailableReason.ParentUnavailable);
    }

    /// <summary>Proves an open palette freezes pane entry points and valid Escape preserves state.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaletteCancelsPreservesPanesAndReturnsToCapturedSideAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "item.txt")));
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.ToggleSelection, observer, CancellationToken.None);
        PaneSnapshot before = session.Current.Panes.Left;
        _ = await session.HandleAsync(UserIntent.OpenCommandPalette, observer, CancellationToken.None);

        _ = await session.HandleAsync(UserIntent.ActivateOtherPane, observer, CancellationToken.None);
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\blocked"), CancellationToken.None);
        CommandPaletteOpen open = Assert.IsInstanceOfType<CommandPaletteOpen>(session.Current.CommandPalette);
        CommanderSnapshot cancelled = await session.HandleAsync(
            UserIntent.CancelCommandPalette(open),
            observer,
            CancellationToken.None);

        Assert.AreSame(before, cancelled.Panes.Left);
        Assert.AreSame(PaneSide.Left, cancelled.Panes.ActiveSide);
        Assert.HasCount(
            1,
            Assert.IsInstanceOfType<PaneContentListed>(cancelled.Panes.Left.Content).State.Selection);
        CommandPaletteClosed closed = Assert.IsInstanceOfType<CommandPaletteClosed>(cancelled.CommandPalette);
        Assert.AreSame(PaneSide.Left, closed.FileListFocusSide);
        Assert.HasCount(1, left.Requests);
    }

    /// <summary>Proves unavailable and non-catalog submissions cannot dispatch or close the palette.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaletteSubmissionIsUnavailableOrUnknownKeepsPaletteAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "item.txt")));
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        CommandPaletteOpen open = Assert.IsInstanceOfType<CommandPaletteOpen>((await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            observer,
            CancellationToken.None)).CommandPalette);

        CommanderSnapshot unavailable = await session.HandleAsync(
            UserIntent.SubmitCommand(open, UserIntent.Copy),
            observer,
            CancellationToken.None);
        CommanderSnapshot unknown = await session.HandleAsync(
            UserIntent.SubmitCommand(open, UserIntent.Confirm),
            observer,
            CancellationToken.None);

        Assert.AreSame(open, unavailable.CommandPalette);
        Assert.AreSame(open, unknown.CommandPalette);
        Assert.AreSame(OperationActivity.Idle, unknown.Panes.Operation);
    }

    /// <summary>Proves an available palette command closes and enters the existing modal route once.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaletteCommandIsAvailableDispatchesExistingIntentAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "item.txt")));
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        CommandPaletteOpen open = Assert.IsInstanceOfType<CommandPaletteOpen>((await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            observer,
            CancellationToken.None)).CommandPalette);

        CommanderSnapshot dispatched = await session.HandleAsync(
            UserIntent.SubmitCommand(open, UserIntent.Rename),
            observer,
            CancellationToken.None);

        Assert.AreSame(CommandPaletteState.Closed, dispatched.CommandPalette);
        OperationAwaitingName awaiting = Assert.IsInstanceOfType<OperationAwaitingName>(dispatched.Panes.Operation);
        Assert.AreSame(OperationKind.Rename, awaiting.Kind);
        Assert.AreEqual("C:\\left\\item.txt", awaiting.Subject.CanonicalText);
    }

    /// <summary>Proves settings and address commands transfer ownership to their existing editors.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaletteSubmitsEditorCommandsTransfersExistingOwnerAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "item.txt")));
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        CommandPaletteOpen settingsPalette = Assert.IsInstanceOfType<CommandPaletteOpen>((await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            observer,
            CancellationToken.None)).CommandPalette);

        CommanderSnapshot settings = await session.HandleAsync(
            UserIntent.SubmitCommand(settingsPalette, UserIntent.OpenSettings),
            observer,
            CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.Escape, observer, CancellationToken.None);
        CommandPaletteOpen addressPalette = Assert.IsInstanceOfType<CommandPaletteOpen>((await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            observer,
            CancellationToken.None)).CommandPalette);
        CommanderSnapshot address = await session.HandleAsync(
            UserIntent.SubmitCommand(addressPalette, UserIntent.FocusAddress),
            observer,
            CancellationToken.None);

        Assert.AreSame(CommandPaletteState.Closed, settings.CommandPalette);
        Assert.AreSame(SettingsEditorState.Open, settings.Settings.Editor);
        Assert.AreSame(CommandPaletteState.Closed, address.CommandPalette);
        AddressEditing editing = Assert.IsInstanceOfType<AddressEditing>(address.AddressEditor);
        Assert.AreSame(PaneSide.Left, editing.Side);
    }

    /// <summary>Proves pane activation executes once and leaves the new side authoritative.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaletteActivatesOtherPaneKeepsNewSideAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "left.txt")));
        right.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\right", "right.txt")));
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        _ = await session.NavigateAsync(PaneSide.Right, ParsePath("C:\\right"), CancellationToken.None);
        CommandPaletteOpen open = Assert.IsInstanceOfType<CommandPaletteOpen>((await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            observer,
            CancellationToken.None)).CommandPalette);

        CommanderSnapshot activated = await session.HandleAsync(
            UserIntent.SubmitCommand(open, UserIntent.ActivateOtherPane),
            observer,
            CancellationToken.None);

        Assert.AreSame(CommandPaletteState.Closed, activated.CommandPalette);
        Assert.AreSame(PaneSide.Right, activated.Panes.ActiveSide);
        Assert.AreSame(open.Left, activated.Panes.Left);
        Assert.AreSame(open.Right, activated.Panes.Right);
    }

    /// <summary>Proves palette Delete enters the existing confirmation with its captured source.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-008")]
    public async Task HandleAsyncWhenPaletteSubmitsDeletePreservesConfirmationTargetAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = Listing("C:\\left", "item.txt");
        left.Enqueue(DirectoryReadOutcome.Succeeded(listing));
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(PermanentInspection(listing.Entries[0].Path));
        using FileOperationGateway gateway = new(port);
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, listing.Location, CancellationToken.None);
        CommandPaletteOpen open = Assert.IsInstanceOfType<CommandPaletteOpen>((await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            observer,
            CancellationToken.None)).CommandPalette);

        CommanderSnapshot dispatched = await session.HandleAsync(
            UserIntent.SubmitCommand(open, UserIntent.Delete),
            observer,
            CancellationToken.None);

        Assert.AreSame(CommandPaletteState.Closed, dispatched.CommandPalette);
        OperationAwaitingConfirmation awaiting =
            Assert.IsInstanceOfType<OperationAwaitingConfirmation>(dispatched.Panes.Operation);
        Assert.HasCount(1, awaiting.Request.Sources);
        Assert.AreSame(listing.Entries[0].Path, awaiting.Request.Sources[0]);
        Assert.HasCount(1, port.Calls);
        Assert.AreEqual("Inspect:C:\\left\\item.txt", port.Calls[0]);
    }

    /// <summary>Proves an old Enter event cannot close or execute a newly opened palette.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenOldPaletteSubmissionArrivesKeepsCurrentPaletteAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "item.txt")));
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        CommandPaletteOpen old = Assert.IsInstanceOfType<CommandPaletteOpen>((await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            observer,
            CancellationToken.None)).CommandPalette);
        _ = await session.HandleAsync(UserIntent.CancelCommandPalette(old), observer, CancellationToken.None);
        CommandPaletteOpen current = Assert.IsInstanceOfType<CommandPaletteOpen>((await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            observer,
            CancellationToken.None)).CommandPalette);

        CommanderSnapshot staleCancel = await session.HandleAsync(
            UserIntent.CancelCommandPalette(old),
            observer,
            CancellationToken.None);
        CommanderSnapshot ignored = await session.HandleAsync(
            UserIntent.SubmitCommand(old, UserIntent.Rename),
            observer,
            CancellationToken.None);

        Assert.AreSame(current, staleCancel.CommandPalette);
        Assert.AreSame(current, ignored.CommandPalette);
        Assert.AreSame(OperationActivity.Idle, ignored.Panes.Operation);
    }

    /// <summary>Proves active-side and passive-snapshot changes fail closed without old-side focus.</summary>
    [TestMethod]
    [DataRow("active")]
    [DataRow("passive")]
    public async Task HandleAsyncWhenCapturedPaletteScopeChangesRejectsExecutionAsync(string changedPart)
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "left.txt")));
        right.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\right", "right.txt")));
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()),
            out DualPaneSession panes);
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        _ = await session.NavigateAsync(PaneSide.Right, ParsePath("C:\\right"), CancellationToken.None);
        CommandPaletteOpen open = Assert.IsInstanceOfType<CommandPaletteOpen>((await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            observer,
            CancellationToken.None)).CommandPalette);
        _ = await panes.HandleAsync(UserIntent.ActivateOtherPane, observer, CancellationToken.None);
        if (changedPart == "passive")
        {
            _ = await panes.HandleAsync(UserIntent.ToggleHiddenItems, observer, CancellationToken.None);
            _ = await panes.HandleAsync(UserIntent.ActivateOtherPane, observer, CancellationToken.None);
        }

        CommanderSnapshot rejected = await session.HandleAsync(
            UserIntent.SubmitCommand(open, UserIntent.Copy),
            observer,
            CancellationToken.None);

        Assert.AreSame(CommandPaletteState.Closed, rejected.CommandPalette);
        Assert.AreSame(changedPart == "active" ? PaneSide.Right : PaneSide.Left, rejected.Panes.ActiveSide);
        Assert.AreSame(OperationActivity.Idle, rejected.Panes.Operation);
    }

    /// <summary>Proves stale Escape closes without overriding a newer modal's focus ownership.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaletteScopeGainsModalEscapeDoesNotRestoreOldPaneFocusAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "item.txt")));
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()),
            out DualPaneSession panes);
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        CommandPaletteOpen open = Assert.IsInstanceOfType<CommandPaletteOpen>((await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            observer,
            CancellationToken.None)).CommandPalette);
        _ = await panes.HandleAsync(UserIntent.Rename, observer, CancellationToken.None);

        CommanderSnapshot closed = await session.HandleAsync(
            UserIntent.CancelCommandPalette(open),
            observer,
            CancellationToken.None);

        Assert.AreSame(CommandPaletteState.Closed, closed.CommandPalette);
        _ = Assert.IsInstanceOfType<OperationAwaitingName>(closed.Panes.Operation);
    }

    /// <summary>Proves pane reads and file launches in flight refuse palette capture.</summary>
    [TestMethod]
    [DataRow("loading")]
    [DataRow("launching")]
    public async Task HandleAsyncWhenPaneExternalWorkRunsRefusesPaletteAsync(string activity)
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        ScriptedFileLauncher launcher = new();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "item.txt")));
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()),
            launcher);
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        Task<CommanderSnapshot> pending;
        if (activity == "loading")
        {
            TaskCompletionSource<DirectoryReadOutcome> completion = left.EnqueuePending();
            pending = session.NavigateAsync(PaneSide.Left, ParsePath("C:\\target"), CancellationToken.None);
            CommanderSnapshot refused = await session.HandleAsync(
                UserIntent.OpenCommandPalette,
                observer,
                CancellationToken.None);
            Assert.AreSame(CommandPaletteState.Closed, refused.CommandPalette);
            _ = Assert.IsInstanceOfType<PaneLoading>(refused.Panes.Left.Activity);
            completion.SetResult(DirectoryReadOutcome.Cancelled());
        }
        else
        {
            TaskCompletionSource<NeNeCommander.Application.Launching.FileLaunchOutcome> completion =
                launcher.EnqueuePending();
            pending = session.HandleAsync(UserIntent.OpenFocused, observer, CancellationToken.None);
            CommanderSnapshot refused = await session.HandleAsync(
                UserIntent.OpenCommandPalette,
                observer,
                CancellationToken.None);
            Assert.AreSame(CommandPaletteState.Closed, refused.CommandPalette);
            _ = Assert.IsInstanceOfType<PaneLaunching>(refused.Panes.Left.Activity);
            completion.SetResult(NeNeCommander.Application.Launching.FileLaunchOutcome.Accepted());
        }
        _ = await pending;
    }

    /// <summary>Proves external work in the right pane independently blocks palette capture.</summary>
    [TestMethod]
    [DataRow("loading")]
    [DataRow("launching")]
    public async Task HandleAsyncWhenRightPaneExternalWorkRunsRefusesPaletteAsync(string activity)
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        ScriptedFileLauncher leftLauncher = new();
        ScriptedFileLauncher rightLauncher = new();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "left.txt")));
        right.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\right", "right.txt")));
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()),
            leftLauncher,
            rightLauncher,
            out _);
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        _ = await session.NavigateAsync(PaneSide.Right, ParsePath("C:\\right"), CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.ActivateOtherPane, observer, CancellationToken.None);
        Task<CommanderSnapshot> pending;
        if (activity == "loading")
        {
            TaskCompletionSource<DirectoryReadOutcome> completion = right.EnqueuePending();
            pending = session.NavigateAsync(PaneSide.Right, ParsePath("C:\\target"), CancellationToken.None);
            CommanderSnapshot refused = await session.HandleAsync(
                UserIntent.OpenCommandPalette,
                observer,
                CancellationToken.None);
            Assert.AreSame(CommandPaletteState.Closed, refused.CommandPalette);
            _ = Assert.IsInstanceOfType<PaneLoading>(refused.Panes.Right.Activity);
            completion.SetResult(DirectoryReadOutcome.Cancelled());
        }
        else
        {
            TaskCompletionSource<NeNeCommander.Application.Launching.FileLaunchOutcome> completion =
                rightLauncher.EnqueuePending();
            pending = session.HandleAsync(UserIntent.OpenFocused, observer, CancellationToken.None);
            CommanderSnapshot refused = await session.HandleAsync(
                UserIntent.OpenCommandPalette,
                observer,
                CancellationToken.None);
            Assert.AreSame(CommandPaletteState.Closed, refused.CommandPalette);
            _ = Assert.IsInstanceOfType<PaneLaunching>(refused.Panes.Right.Activity);
            completion.SetResult(NeNeCommander.Application.Launching.FileLaunchOutcome.Accepted());
        }
        _ = await pending;
    }

    /// <summary>Proves an open palette owns input and ignores the manager and every fixed slot.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaletteOwnsInputIgnoresBookmarkIntentsAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "item.txt")));
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSessionWithCatalog(
            left,
            right,
            gateway,
            Catalog(Entry("Target", "C:\\bookmark", BookmarkShortcutSlot.One)));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        CommanderSnapshot opened = await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            observer,
            CancellationToken.None);
        CommandPaletteOpen palette = Assert.IsInstanceOfType<CommandPaletteOpen>(opened.CommandPalette);

        CommanderSnapshot managerIgnored = await session.HandleAsync(
            UserIntent.OpenBookmarks,
            observer,
            CancellationToken.None);
        CommanderSnapshot slotIgnored = await session.HandleAsync(
            UserIntent.BookmarkSlotOne,
            observer,
            CancellationToken.None);

        Assert.AreSame(palette, managerIgnored.CommandPalette);
        Assert.AreSame(palette, slotIgnored.CommandPalette);
        Assert.AreSame(SettingsEditorState.Closed, slotIgnored.Settings.Editor);
        Assert.HasCount(1, left.Requests);
        Assert.IsEmpty(right.Requests);
    }

    /// <summary>Proves an open Bookmarks modal ignores palette, address, and settings entry.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenBookmarksModalOwnsInputIgnoresPaletteAddressAndSettingsAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "item.txt")));
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.OpenBookmarks, observer, CancellationToken.None);

        CommanderSnapshot paletteIgnored = await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            observer,
            CancellationToken.None);
        CommanderSnapshot shortcutAddressIgnored = await session.HandleAsync(
            UserIntent.FocusAddress,
            observer,
            CancellationToken.None);
        CommanderSnapshot nativeAddressIgnored = await session.HandleAsync(
            UserIntent.BeginAddressEdit(PaneSide.Left),
            observer,
            CancellationToken.None);
        CommanderSnapshot settingsIgnored = await session.HandleAsync(
            UserIntent.OpenSettings,
            observer,
            CancellationToken.None);

        Assert.AreSame(CommandPaletteState.Closed, paletteIgnored.CommandPalette);
        Assert.AreSame(AddressEditorState.Closed, shortcutAddressIgnored.AddressEditor);
        Assert.AreSame(AddressEditorState.Closed, nativeAddressIgnored.AddressEditor);
        Assert.AreSame(SettingsEditorState.Bookmarks, settingsIgnored.Settings.Editor);
        Assert.HasCount(1, left.Requests);
    }

    /// <summary>Proves an active address editor ignores the manager and every fixed slot.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenAddressEditorOwnsInputIgnoresBookmarkIntentsAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "item.txt")));
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSessionWithCatalog(
            left,
            right,
            gateway,
            Catalog(Entry("Target", "C:\\bookmark", BookmarkShortcutSlot.One)));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        CommanderSnapshot editing = await session.HandleAsync(
            UserIntent.FocusAddress,
            observer,
            CancellationToken.None);
        AddressEditing editor = Assert.IsInstanceOfType<AddressEditing>(editing.AddressEditor);

        CommanderSnapshot managerIgnored = await session.HandleAsync(
            UserIntent.OpenBookmarks,
            observer,
            CancellationToken.None);
        CommanderSnapshot slotIgnored = await session.HandleAsync(
            UserIntent.BookmarkSlotOne,
            observer,
            CancellationToken.None);

        Assert.AreSame(editor, managerIgnored.AddressEditor);
        Assert.AreSame(editor, slotIgnored.AddressEditor);
        Assert.AreEqual("C:\\left", editor.OriginalLocation.CanonicalText);
        Assert.AreSame(SettingsEditorState.Closed, slotIgnored.Settings.Editor);
        Assert.HasCount(1, left.Requests);
    }

    /// <summary>Proves a pending operation modal freezes the bookmark manager and every fixed slot.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenOperationModalOwnsInputFreezesBookmarkInteractionAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "item.txt")));
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSessionWithCatalog(
            left,
            right,
            gateway,
            Catalog(Entry("Target", "C:\\bookmark", BookmarkShortcutSlot.One)));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        CommanderSnapshot awaitingName = await session.HandleAsync(
            UserIntent.Rename,
            observer,
            CancellationToken.None);

        CommanderSnapshot managerRefused = await session.HandleAsync(
            UserIntent.OpenBookmarks,
            observer,
            CancellationToken.None);
        CommanderSnapshot slotRefused = await session.HandleAsync(
            UserIntent.BookmarkSlotOne,
            observer,
            CancellationToken.None);

        _ = Assert.IsInstanceOfType<OperationAwaitingName>(awaitingName.Panes.Operation);
        _ = Assert.IsInstanceOfType<OperationAwaitingName>(slotRefused.Panes.Operation);
        Assert.AreSame(SettingsEditorState.Closed, managerRefused.Settings.Editor);
        Assert.AreSame(SettingsEditorState.Closed, slotRefused.Settings.Editor);
        Assert.HasCount(1, left.Requests);
        Assert.IsEmpty(right.Requests);
    }

    /// <summary>Proves a pending Shell handoff freezes the bookmark manager and every fixed slot.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaneIsLaunchingFreezesBookmarkManagerAndSlotsAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        ScriptedFileLauncher launcher = new();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "item.txt")));
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()),
            launcher,
            new ScriptedFileLauncher(),
            SettingsReadOutcome.Read(UserSettings.Create(
                ColorScheme.NeNeDark,
                HiddenItemVisibility.Hidden,
                Catalog(Entry("Target", "C:\\bookmark", BookmarkShortcutSlot.One)))),
            out _);
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        TaskCompletionSource<NeNeCommander.Application.Launching.FileLaunchOutcome> completion =
            launcher.EnqueuePending();
        Task<CommanderSnapshot> launching = session.HandleAsync(
            UserIntent.OpenFocused,
            observer,
            CancellationToken.None);

        CommanderSnapshot managerRefused = await session.HandleAsync(
            UserIntent.OpenBookmarks,
            observer,
            CancellationToken.None);
        CommanderSnapshot slotRefused = await session.HandleAsync(
            UserIntent.BookmarkSlotOne,
            observer,
            CancellationToken.None);

        _ = Assert.IsInstanceOfType<PaneLaunching>(managerRefused.Panes.Left.Activity);
        Assert.AreSame(SettingsEditorState.Closed, managerRefused.Settings.Editor);
        Assert.AreSame(SettingsEditorState.Closed, slotRefused.Settings.Editor);
        Assert.HasCount(1, left.Requests);
        completion.SetResult(NeNeCommander.Application.Launching.FileLaunchOutcome.Accepted());
        _ = await launching;
    }

    /// <summary>Proves a direct bookmark read appends one history location and truncates Forward.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenDirectBookmarkSucceedsAppendsOneHistoryLocationAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "old.txt")));
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\next", "next.txt")));
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "old.txt")));
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\bookmark", "new.txt")));
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSessionWithCatalog(
            left,
            right,
            gateway,
            Catalog(Entry("Target", "C:\\bookmark", BookmarkShortcutSlot.One)));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\next"), CancellationToken.None);
        CommanderSnapshot back = await session.HandleAsync(
            UserIntent.NavigateBack,
            observer,
            CancellationToken.None);
        PaneNavigationHistory beforeBookmark = HistoryOf(back);
        Assert.HasCount(2, beforeBookmark.Locations);
        Assert.AreEqual(0, beforeBookmark.CurrentIndex);
        Assert.IsNotNull(beforeBookmark.ForwardTarget);

        CommanderSnapshot navigated = await session.HandleAsync(
            UserIntent.BookmarkSlotOne,
            observer,
            CancellationToken.None);

        PaneNavigationHistory history = HistoryOf(navigated);
        Assert.HasCount(2, history.Locations);
        Assert.AreEqual(1, history.CurrentIndex);
        Assert.AreEqual("C:\\left", history.Locations[0].CanonicalText);
        Assert.AreEqual("C:\\bookmark", history.Locations[1].CanonicalText);
        Assert.IsNull(history.ForwardTarget);
    }

    /// <summary>Proves only a successful manager navigation changes the pane location history.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenManagerBookmarkNavigatesRecordsOnlySuccessInHistoryAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        BookmarkEntry entry = Entry("Target", "C:\\bookmark", null);
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "old.txt")));
        left.Enqueue(DirectoryReadOutcome.Failed(FileOperationFailureKind.ProviderUnavailable));
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\bookmark", "new.txt")));
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSessionWithCatalog(left, right, gateway, Catalog(entry));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.OpenBookmarks, observer, CancellationToken.None);

        CommanderSnapshot failed = await session.HandleAsync(
            UserIntent.NavigateBookmark(new BookmarkSelection(entry)),
            observer,
            CancellationToken.None);
        PaneNavigationHistory afterFailure = HistoryOf(failed);
        CommanderSnapshot navigated = await session.HandleAsync(
            UserIntent.NavigateBookmark(new BookmarkSelection(entry)),
            observer,
            CancellationToken.None);

        Assert.HasCount(1, afterFailure.Locations);
        Assert.AreEqual(0, afterFailure.CurrentIndex);
        Assert.AreEqual("C:\\left", afterFailure.Locations[0].CanonicalText);
        PaneNavigationHistory history = HistoryOf(navigated);
        Assert.HasCount(2, history.Locations);
        Assert.AreEqual(1, history.CurrentIndex);
        Assert.AreEqual("C:\\bookmark", history.Locations[1].CanonicalText);
    }

    /// <summary>Proves a palette submission naming a bookmark command is rejected without dispatch.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaletteSubmissionNamesBookmarkCommandRejectsWithoutDispatchAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "item.txt")));
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSessionWithCatalog(
            left,
            right,
            gateway,
            Catalog(Entry("Target", "C:\\bookmark", BookmarkShortcutSlot.One)));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        CommanderSnapshot opened = await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            observer,
            CancellationToken.None);
        CommandPaletteOpen palette = Assert.IsInstanceOfType<CommandPaletteOpen>(opened.CommandPalette);

        CommanderSnapshot managerRejected = await session.HandleAsync(
            UserIntent.SubmitCommand(palette, UserIntent.OpenBookmarks),
            observer,
            CancellationToken.None);
        CommanderSnapshot slotRejected = await session.HandleAsync(
            UserIntent.SubmitCommand(palette, UserIntent.BookmarkSlotOne),
            observer,
            CancellationToken.None);

        Assert.AreSame(palette, managerRejected.CommandPalette);
        Assert.AreSame(palette, slotRejected.CommandPalette);
        Assert.AreSame(SettingsEditorState.Closed, slotRejected.Settings.Editor);
        Assert.HasCount(15, CommandCatalog.Commands);
        Assert.IsFalse(CommandCatalog.Commands.Contains(UserIntent.OpenBookmarks));
        Assert.IsFalse(CommandCatalog.Commands.Contains(UserIntent.BookmarkSlotOne));
        Assert.HasCount(1, left.Requests);
    }

    private static PaneNavigationHistory HistoryOf(CommanderSnapshot snapshot)
    {
        return Assert.IsInstanceOfType<PaneContentListed>(snapshot.Panes.Left.Content)
            .State.NavigationHistory;
    }

    private static CommanderSession CreateSession(
        ScriptedDirectoryReadPort left,
        ScriptedDirectoryReadPort right,
        FileOperationGateway gateway,
        ISettingsStore store)
    {
        return CreateSession(left, right, gateway, store, new ScriptedFileLauncher());
    }

    private static CommanderSession CreateSession(
        ScriptedDirectoryReadPort left,
        ScriptedDirectoryReadPort right,
        FileOperationGateway gateway,
        ISettingsStore store,
        ScriptedFileLauncher leftLauncher)
    {
        return CreateSession(left, right, gateway, store, leftLauncher, out _);
    }

    private static CommanderSession CreateSession(
        ScriptedDirectoryReadPort left,
        ScriptedDirectoryReadPort right,
        FileOperationGateway gateway,
        ISettingsStore store,
        out DualPaneSession panes)
    {
        return CreateSession(left, right, gateway, store, new ScriptedFileLauncher(), out panes);
    }

    private static CommanderSession CreateSession(
        ScriptedDirectoryReadPort left,
        ScriptedDirectoryReadPort right,
        FileOperationGateway gateway,
        ISettingsStore store,
        ScriptedFileLauncher leftLauncher,
        out DualPaneSession panes)
    {
        return CreateSession(
            left,
            right,
            gateway,
            store,
            leftLauncher,
            new ScriptedFileLauncher(),
            SettingsReadOutcome.Absent(),
            out panes);
    }

    private static CommanderSession CreateSession(
        ScriptedDirectoryReadPort left,
        ScriptedDirectoryReadPort right,
        FileOperationGateway gateway,
        ISettingsStore store,
        ScriptedFileLauncher leftLauncher,
        ScriptedFileLauncher rightLauncher,
        out DualPaneSession panes)
    {
        return CreateSession(
            left,
            right,
            gateway,
            store,
            leftLauncher,
            rightLauncher,
            SettingsReadOutcome.Absent(),
            out panes);
    }

    private static CommanderSession CreateSession(
        ScriptedDirectoryReadPort left,
        ScriptedDirectoryReadPort right,
        FileOperationGateway gateway,
        ISettingsStore store,
        SettingsReadOutcome initialOutcome)
    {
        return CreateSession(
            left,
            right,
            gateway,
            store,
            new ScriptedFileLauncher(),
            new ScriptedFileLauncher(),
            initialOutcome,
            out _);
    }

    private static CommanderSession CreateSessionWithCatalog(
        ScriptedDirectoryReadPort left,
        ScriptedDirectoryReadPort right,
        FileOperationGateway gateway,
        BookmarkCatalog catalog)
    {
        return CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()),
            SettingsReadOutcome.Read(UserSettings.Create(
                ColorScheme.NeNeDark,
                HiddenItemVisibility.Hidden,
                catalog)));
    }

    private static CommanderSession CreateSession(
        ScriptedDirectoryReadPort left,
        ScriptedDirectoryReadPort right,
        FileOperationGateway gateway,
        ISettingsStore store,
        ScriptedFileLauncher leftLauncher,
        ScriptedFileLauncher rightLauncher,
        SettingsReadOutcome initialOutcome,
        out DualPaneSession panes)
    {
        PaneSession leftPane = new(
            left,
            leftLauncher,
            Capacity(),
            DirectoryListing.EntryBoundaryLimit,
            HiddenItemVisibility.Hidden);
        PaneSession rightPane = new(
            right,
            rightLauncher,
            Capacity(),
            DirectoryListing.EntryBoundaryLimit,
            HiddenItemVisibility.Hidden);
        panes = new DualPaneSession(leftPane, rightPane, gateway);
        return new CommanderSession(
            panes,
            new SettingsSession(store, initialOutcome, static _ => { }));
    }

    private static CommandCandidate Candidate(CommandPaletteOpen palette, UserIntent intent)
    {
        return palette.Candidates.Single(candidate => candidate.Intent == intent);
    }

    private static void AssertUnavailable(
        CommandPaletteOpen palette,
        UserIntent intent,
        CommandUnavailableReason expected)
    {
        CommandUnavailable unavailable = Assert.IsInstanceOfType<CommandUnavailable>(
            Candidate(palette, intent).Availability);
        Assert.AreSame(expected, unavailable.Reason);
    }

    private static FileOperationGateway CreateGateway()
    {
        return new FileOperationGateway(ScriptedFileOperationPort.Create(null, null));
    }

    private static VisiblePageCapacity Capacity()
    {
        return Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(VisiblePageCapacity.Create(4)).Capacity;
    }

    private static DirectoryListing Listing(string location, string name)
    {
        FileSystemPath parsedLocation = ParsePath(location);
        DirectoryEntry entry = DirectoryEntry.Create(
            ParsePath(location + "\\" + name),
            name,
            DirectoryEntryKind.File,
            EntryVisibility.Normal);
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(
            DirectoryListing.Create(
                parsedLocation,
                [entry],
                DirectoryListingCompleteness.Complete,
            0)).Listing;
    }

    private static DirectoryListing EmptyListing(string location)
    {
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(DirectoryListing.Create(
            ParsePath(location),
            [],
            DirectoryListingCompleteness.Complete,
            0)).Listing;
    }

    private static FileInspectionOutcome Inspection(FileSystemPath path)
    {
        FileIdentityAccepted identity = Assert.IsInstanceOfType<FileIdentityAccepted>(
            FileIdentity.Parse("identity:" + path.CanonicalText));
        return FileInspectionOutcome.Succeeded(
            FileEntrySnapshot.Create(path, identity.Identity, DeletionCapability.Recycle));
    }

    private static FileInspectionOutcome PermanentInspection(FileSystemPath path)
    {
        FileIdentityAccepted identity = Assert.IsInstanceOfType<FileIdentityAccepted>(
            FileIdentity.Parse("identity:" + path.CanonicalText));
        return FileInspectionOutcome.Succeeded(
            FileEntrySnapshot.Create(path, identity.Identity, DeletionCapability.PermanentOnly));
    }

    private static FileSystemPath ParsePath(string text)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path;
    }

    private static BookmarkEntry Entry(
        string name,
        string path,
        BookmarkShortcutSlot? slot)
    {
        BookmarkDisplayName displayName = Assert.IsInstanceOfType<BookmarkDisplayNameAccepted>(
            BookmarkDisplayName.Parse(name)).Name;
        BookmarkPath bookmarkPath = Assert.IsInstanceOfType<BookmarkPathAccepted>(
            BookmarkPath.Parse(path)).Path;
        return BookmarkEntry.Create(displayName, bookmarkPath, null, slot);
    }

    private static BookmarkCatalog Catalog(BookmarkEntry entry)
    {
        return Assert.IsInstanceOfType<BookmarkCatalogAccepted>(
            BookmarkCatalog.Create([], [entry])).Catalog;
    }
}
