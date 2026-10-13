using System;
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

/// <summary>Proves saved orders seed pane state and accepted commands use the existing settings queue.</summary>
[TestClass]
public sealed class PaneSortPersistenceTests
{
    /// <summary>Proves every typed persisted order reaches the first successful listing on either side.</summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    [DataRow(6)]
    [DataRow(7)]
    public async Task NavigateAsyncWhenSavedOrdersExistRestoresBothWithoutWritingAsync(int choice)
    {
        PaneSortOrder order = Order(choice);
        using Fixture fixture = new(PaneSortPreferences.Create(order, Order(7 - choice)));

        await fixture.ListBothAsync();

        Assert.AreEqual(order, State(fixture.Session.Current.Panes.Left).SortOrder);
        Assert.AreEqual(Order(7 - choice), State(fixture.Session.Current.Panes.Right).SortOrder);
        Assert.IsEmpty(fixture.Store.Writes);
    }

    /// <summary>Proves first focus follows the restored extension projection, rather than listing name order.</summary>
    [TestMethod]
    public async Task NavigateAsyncWhenInitialReadFailsOrCancelsRetainsSavedOrderAndFirstFocusAsync()
    {
        using Fixture fixture = new(PaneSortPreferences.Create(Order(2), PaneSortOrder.Default));
        fixture.Left.Enqueue(DirectoryReadOutcome.Failed(FileOperationFailureKind.AccessDenied));
        fixture.Left.Enqueue(DirectoryReadOutcome.Cancelled());
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left")));

        _ = await fixture.Session.NavigateAsync(PaneSide.Left, Path("C:\\left"), CancellationToken.None);
        _ = await fixture.Session.NavigateAsync(PaneSide.Left, Path("C:\\left"), CancellationToken.None);
        CommanderSnapshot listed = await fixture.Session.NavigateAsync(PaneSide.Left, Path("C:\\left"), CancellationToken.None);

        PaneState state = State(listed.Panes.Left);
        Assert.AreEqual(Order(2), state.SortOrder);
        Assert.AreEqual("C:\\left\\z.aaa", state.FocusItem?.CanonicalText);
        Assert.IsEmpty(fixture.Store.Writes);
    }

    /// <summary>Proves an abandoned initial read leaves the persisted startup order available.</summary>
    [TestMethod]
    public async Task NavigateAsyncWhenInitialReadIsAbandonedRestoresSavedOrderOnRetryAsync()
    {
        using Fixture fixture = new(PaneSortPreferences.Create(Order(2), Order(7)));
        TaskCompletionSource<DirectoryReadOutcome> first = fixture.Left.EnqueuePending();
        Task<CommanderSnapshot> abandoned = fixture.Session.NavigateAsync(PaneSide.Left, Path("C:\\left"), CancellationToken.None);
        _ = await fixture.HandleAsync(UserIntent.Escape);
        _ = await abandoned;
        first.SetResult(DirectoryReadOutcome.Cancelled());
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left")));

        _ = await fixture.Session.NavigateAsync(PaneSide.Left, Path("C:\\left"), CancellationToken.None);

        Assert.AreEqual(Order(2), State(fixture.Session.Current.Panes.Left).SortOrder);
        Assert.AreEqual("C:\\left\\z.aaa", State(fixture.Session.Current.Panes.Left).FocusItem?.CanonicalText);
        Assert.IsEmpty(fixture.Store.Writes);
    }

    /// <summary>Proves queued writes keep both choices, and Tab never persists a difference between sides.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenBothPanesSortWritesAcceptedOrdersInSequenceAsync()
    {
        using Fixture fixture = new(PaneSortPreferences.Default);
        await fixture.ListBothAsync();
        TaskCompletionSource<SettingsWriteOutcome> first = fixture.Store.PlanWrite();
        TaskCompletionSource<SettingsWriteOutcome> second = fixture.Store.PlanWrite();

        _ = await fixture.HandleAsync(UserIntent.SortByExtension);
        _ = await fixture.HandleAsync(UserIntent.ActivateOtherPane);
        _ = await fixture.HandleAsync(UserIntent.SortByModified);

        Assert.HasCount(1, fixture.Store.Writes);
        Assert.AreEqual(Order(2), fixture.Session.Current.Settings.Settings.PaneSortOrders.Left);
        Assert.AreEqual(Order(6), fixture.Session.Current.Settings.Settings.PaneSortOrders.Right);
        first.SetResult(SettingsWriteOutcome.Succeeded());
        await fixture.Store.WaitForWriteCountAsync(2);
        Assert.AreEqual(Order(2), fixture.Store.Writes[0].PaneSortOrders.Left);
        Assert.AreEqual(PaneSortOrder.Default, fixture.Store.Writes[0].PaneSortOrders.Right);
        Assert.AreEqual(Order(2), fixture.Store.Writes[1].PaneSortOrders.Left);
        Assert.AreEqual(Order(6), fixture.Store.Writes[1].PaneSortOrders.Right);
        second.SetResult(SettingsWriteOutcome.Succeeded());
        await fixture.Session.StopAsync();
    }

    /// <summary>Proves failure keeps the changed pane and typed preference, with the existing warning channel.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenSortSaveFailsKeepsPaneOrderAndFailureWarningAsync()
    {
        using Fixture fixture = new(PaneSortPreferences.Default);
        await fixture.ListBothAsync();
        TaskCompletionSource<SettingsWriteOutcome> write = fixture.Store.PlanWrite();

        CommanderSnapshot pending = await fixture.HandleAsync(UserIntent.SortBySize);
        write.SetResult(SettingsWriteOutcome.Rejected(
            SettingsWriteFailureKind.IoFailure, SettingsDirectoryEffect.NotAttempted, SettingsWriteEffect.None));
        await fixture.Session.StopAsync();

        _ = Assert.IsInstanceOfType<SettingsPersistencePending>(pending.Settings.Persistence);
        _ = Assert.IsInstanceOfType<SettingsPersistenceFailed>(fixture.Session.Current.Settings.Persistence);
        Assert.AreEqual(Order(4), State(fixture.Session.Current.Panes.Left).SortOrder);
        Assert.AreEqual(Order(4), fixture.Session.Current.Settings.Settings.PaneSortOrders.Left);
    }

    /// <summary>Proves unlisted, loading, and modal rejection never reach storage.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenSortIsRejectedDoesNotWriteAsync()
    {
        using Fixture fixture = new(PaneSortPreferences.Default);
        _ = await fixture.HandleAsync(UserIntent.SortByName);
        TaskCompletionSource<DirectoryReadOutcome> read = fixture.Left.EnqueuePending();
        Task<CommanderSnapshot> navigation = fixture.Session.NavigateAsync(PaneSide.Left, Path("C:\\left"), CancellationToken.None);
        _ = await fixture.HandleAsync(UserIntent.SortByExtension);
        read.SetResult(DirectoryReadOutcome.Succeeded(Listing("C:\\left")));
        _ = await navigation;
        _ = await fixture.HandleAsync(UserIntent.OpenSettings);
        _ = await fixture.HandleAsync(UserIntent.SortBySize);

        Assert.IsEmpty(fixture.Store.Writes);
        Assert.AreEqual(PaneSortOrder.Default, State(fixture.Session.Current.Panes.Left).SortOrder);
    }

    /// <summary>Proves other preference and catalog revisions retain both sort values, in either direction.</summary>
    [TestMethod]
    public async Task SelectAsyncWhenSortAndOtherSettingsOverlapPreservesCompleteValuesAsync()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        BookmarkCatalog catalog = Catalog();
        UserSettings initial = UserSettings.Create(ColorScheme.NeNeDark, HiddenItemVisibility.Hidden, catalog, PaneSortPreferences.Default);
        SettingsSession settings = new(store, SettingsReadOutcome.Read(initial), static exception => throw new AssertFailedException("Unexpected persistence fault.", exception));
        RecordingCommanderObserver observer = new();
        TaskCompletionSource<SettingsWriteOutcome>[] writes = [store.PlanWrite(), store.PlanWrite(), store.PlanWrite(), store.PlanWrite(), store.PlanWrite()];
        Task[] pending =
        [
            settings.SavePaneSortOrderAsync(PaneSide.Left, Order(3), observer, CancellationToken.None),
            settings.SelectColorSchemeAsync(ColorScheme.Dracula, observer, CancellationToken.None),
            settings.SelectLaunchHiddenItemVisibilityAsync(HiddenItemVisibility.Shown, observer, CancellationToken.None),
            settings.SaveBookmarkCatalogAsync(catalog, observer, CancellationToken.None),
            settings.SavePaneSortOrderAsync(PaneSide.Right, Order(7), observer, CancellationToken.None),
        ];
        Task stopped = settings.StopAsync();
        Assert.IsFalse(stopped.IsCompleted);
        for (int index = 0; index < writes.Length; index++)
        {
            await store.WaitForWriteCountAsync(index + 1);
            Assert.AreEqual(Order(3), store.Writes[index].PaneSortOrders.Left);
            Assert.AreSame(catalog, store.Writes[index].Bookmarks);
            writes[index].SetResult(SettingsWriteOutcome.Succeeded());
            await pending[index];
        }
        await stopped;
        UserSettings last = store.Writes[4];
        Assert.AreSame(catalog, last.Bookmarks);
        Assert.AreEqual(Order(7), last.PaneSortOrders.Right);
        Assert.AreSame(ColorScheme.Dracula, last.ColorScheme);
        Assert.AreSame(HiddenItemVisibility.Shown, last.HiddenItemVisibility);
    }

    private static BookmarkCatalog Catalog()
    {
        BookmarkCategoryName category = Assert.IsInstanceOfType<BookmarkCategoryNameAccepted>(BookmarkCategoryName.Parse("Work")).Name;
        BookmarkDisplayName name = Assert.IsInstanceOfType<BookmarkDisplayNameAccepted>(BookmarkDisplayName.Parse("Saved")).Name;
        BookmarkPath path = Assert.IsInstanceOfType<BookmarkPathAccepted>(BookmarkPath.Parse("C:\\saved")).Path;
        BookmarkEntry entry = BookmarkEntry.Create(name, path, category, BookmarkShortcutSlot.One);
        return Assert.IsInstanceOfType<BookmarkCatalogAccepted>(BookmarkCatalog.Create([category], [entry])).Catalog;
    }

    private static PaneSortOrder Order(int choice)
    {
        SortKey[] keys = [SortKey.Name, SortKey.Extension, SortKey.Size, SortKey.Modified];
        return PaneSortOrder.Create(keys[choice / 2], choice % 2 == 0 ? SortDirection.Ascending : SortDirection.Descending);
    }

    private static PaneState State(PaneSnapshot snapshot)
    {
        return Assert.IsInstanceOfType<PaneContentListed>(snapshot.Content).State;
    }

    private static FileSystemPath Path(string text)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path;
    }

    private static DirectoryListing Listing(string location)
    {
        FileSystemPath path = Path(location);
        DirectoryEntry[] entries =
        [
            DirectoryEntry.Create(Path(location + "\\a.txt"), "a.txt", DirectoryEntryKind.File, EntryMetadata.Unmeasured(EntryVisibility.Normal)),
            DirectoryEntry.Create(Path(location + "\\z.aaa"), "z.aaa", DirectoryEntryKind.File, EntryMetadata.Unmeasured(EntryVisibility.Normal)),
        ];
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(DirectoryListing.Create(path, entries, DirectoryListingCompleteness.Complete, 0)).Listing;
    }

    private sealed class Fixture : IDisposable
    {
        private readonly FileOperationGateway _gateway;
        private readonly RecordingCommanderObserver _observer = new();
        internal ScriptedDirectoryReadPort Left { get; } = ScriptedDirectoryReadPort.Create();
        internal ScriptedDirectoryReadPort Right { get; } = ScriptedDirectoryReadPort.Create();
        internal ScriptedSettingsStore Store { get; } = new(SettingsReadOutcome.Absent());
        internal CommanderSession Session { get; }

        internal Fixture(PaneSortPreferences preferences)
        {
            _gateway = new FileOperationGateway(ScriptedFileOperationPort.Create(null, null));
            VisiblePageCapacity capacity = Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(VisiblePageCapacity.Create(4)).Capacity;
            PaneSession left = new(Left, new ScriptedFileLauncher(), PaneSessionConfiguration.Create(capacity, DirectoryListing.EntryBoundaryLimit, HiddenItemVisibility.Hidden, preferences.Left));
            PaneSession right = new(Right, new ScriptedFileLauncher(), PaneSessionConfiguration.Create(capacity, DirectoryListing.EntryBoundaryLimit, HiddenItemVisibility.Hidden, preferences.Right));
            UserSettings initial = UserSettings.Create(ColorScheme.NeNeDark, HiddenItemVisibility.Hidden, BookmarkCatalog.Empty, preferences);
            Session = new CommanderSession(new DualPaneSession(left, right, _gateway),
                new SettingsSession(Store, SettingsReadOutcome.Read(initial), static exception => throw new AssertFailedException("Unexpected persistence fault.", exception)),
                new TransientScopeOwners(new AddressEditorSession(), new CommandPaletteSession(), new WindowAdjustmentSession(),
                    new LocationsSession(new ScriptedDriveCatalog(), new ScriptedWslDistributionCatalog())));
        }

        internal async Task ListBothAsync()
        {
            Left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left")));
            Right.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\right")));
            _ = await Session.NavigateAsync(PaneSide.Left, Path("C:\\left"), CancellationToken.None);
            _ = await Session.NavigateAsync(PaneSide.Right, Path("C:\\right"), CancellationToken.None);
        }

        internal Task<CommanderSnapshot> HandleAsync(UserIntent intent)
        {
            return Session.HandleAsync(intent, _observer, CancellationToken.None);
        }

        public void Dispose()
        {
            _gateway.Dispose();
        }
    }
}
