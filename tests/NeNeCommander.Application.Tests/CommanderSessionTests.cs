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
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()));

        CommanderSnapshot unchanged = await session.HandleAsync(
            UserIntent.BookmarkSlotNine,
            new RecordingCommanderObserver(),
            CancellationToken.None);

        Assert.IsEmpty(left.Requests);
        Assert.IsEmpty(right.Requests);
        Assert.AreSame(PaneContent.Absent, unchanged.Panes.Left.Content);
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
        TaskCompletionSource<DirectoryReadOutcome> read = left.EnqueuePending();
        Task<CommanderSnapshot> navigation = session.HandleAsync(
            UserIntent.NavigateBookmark(new BookmarkSelection(entry)),
            observer,
            CancellationToken.None);

        Assert.AreSame(SettingsEditorState.Bookmarks, session.Current.Settings.Editor);
        Assert.HasCount(2, left.Requests);
        read.SetResult(DirectoryReadOutcome.Succeeded(Listing("C:\\bookmark", "new.txt")));
        CommanderSnapshot navigated = await navigation;

        Assert.AreSame(SettingsEditorState.Closed, navigated.Settings.Editor);
        Assert.AreEqual(
            "C:\\bookmark",
            Assert.IsInstanceOfType<PaneContentListed>(navigated.Panes.Left.Content)
                .Listing.Location.CanonicalText);
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
        BookmarkCatalog catalog = Catalog(entry);
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSessionWithCatalog(left, right, gateway, catalog);
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

    private static CommanderSession CreateSession(
        ScriptedDirectoryReadPort left,
        ScriptedDirectoryReadPort right,
        FileOperationGateway gateway,
        ISettingsStore store)
    {
        return CreateSession(left, right, gateway, store, SettingsReadOutcome.Absent());
    }

    private static CommanderSession CreateSession(
        ScriptedDirectoryReadPort left,
        ScriptedDirectoryReadPort right,
        FileOperationGateway gateway,
        ISettingsStore store,
        SettingsReadOutcome initialOutcome)
    {
        PaneSession leftPane = new(
            left,
            Capacity(),
            DirectoryListing.EntryBoundaryLimit,
            HiddenItemVisibility.Hidden);
        PaneSession rightPane = new(
            right,
            Capacity(),
            DirectoryListing.EntryBoundaryLimit,
            HiddenItemVisibility.Hidden);
        return new CommanderSession(
            new DualPaneSession(leftPane, rightPane, gateway),
            new SettingsSession(store, initialOutcome, static _ => { }));
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

    private static FileInspectionOutcome Inspection(FileSystemPath path)
    {
        FileIdentityAccepted identity = Assert.IsInstanceOfType<FileIdentityAccepted>(
            FileIdentity.Parse("identity:" + path.CanonicalText));
        return FileInspectionOutcome.Succeeded(
            FileEntrySnapshot.Create(path, identity.Identity, DeletionCapability.Recycle));
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
