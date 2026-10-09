using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Drives;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>
/// Proves how the application session admits, freezes, and routes the Locations picker, and that a
/// selection reaches the pane only through the single navigation route, exactly once.
/// </summary>
[TestClass]
public sealed class CommanderSessionLocationsTests
{
    /// <summary>Proves an idle session loads both sections, then opens over the active pane.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenIdleLoadsThenOpensOverTheActivePaneAsync()
    {
        using Fixture fixture = await Fixture.ListedAsync();
        _ = await fixture.HandleAsync(UserIntent.ActivateOtherPane);

        Task<CommanderSnapshot> opening = fixture.HandleAsync(UserIntent.OpenLocations);

        Assert.IsFalse(opening.IsCompleted);
        LocationsLoading loading = Assert.IsInstanceOfType<LocationsLoading>(fixture.Session.Current.Scopes.Locations);
        Assert.AreSame(PaneSide.Right, loading.ActiveSide);
        fixture.Drives.Complete(LocationsSessionTests.DrivesOf(LocationsSessionTests.Drive("C:\\", DriveKind.Fixed, null)));
        fixture.WslRoots.Complete(LocationsSessionTests.RootsOf("Ubuntu"));
        CommanderSnapshot opened = await opening;
        LocationsOpen open = Assert.IsInstanceOfType<LocationsOpen>(opened.Scopes.Locations);
        Assert.AreSame(PaneSide.Right, open.ActiveSide);
        Assert.HasCount(2, open.Items);
        Assert.AreSame(open, fixture.Session.Current.Scopes.Locations);
    }

    /// <summary>Proves the open settings editor or bookmark manager keeps input and reads no catalog.</summary>
    [TestMethod]
    [DataRow("settings")]
    [DataRow("bookmarks")]
    public async Task HandleAsyncWhenSettingsModalIsOpenRefusesThePickerAsync(string modal)
    {
        using Fixture fixture = await Fixture.ListedAsync();
        _ = await fixture.HandleAsync(modal == "settings" ? UserIntent.OpenSettings : UserIntent.OpenBookmarks);

        CommanderSnapshot refused = await fixture.HandleAsync(UserIntent.OpenLocations);

        Assert.AreNotSame(SettingsEditorState.Closed, refused.Settings.Editor);
        fixture.AssertNotOpened(refused);
    }

    /// <summary>Proves an address edit, an open palette, and the window mode keep input.</summary>
    [TestMethod]
    [DataRow("address")]
    [DataRow("palette")]
    [DataRow("window")]
    public async Task HandleAsyncWhenAnotherScopeOwnsInputRefusesThePickerAsync(string scope)
    {
        using Fixture fixture = await Fixture.ListedAsync();
        UserIntent opener = scope == "address"
            ? UserIntent.FocusAddress
            : scope == "palette" ? UserIntent.OpenCommandPalette : UserIntent.OpenWindowAdjustment;
        CommanderSnapshot before = await fixture.HandleAsync(opener);

        CommanderSnapshot refused = await fixture.HandleAsync(UserIntent.OpenLocations);

        Assert.AreSame(before.Scopes.AddressEditor, refused.Scopes.AddressEditor);
        Assert.AreSame(before.Scopes.CommandPalette, refused.Scopes.CommandPalette);
        Assert.AreSame(before.Scopes.WindowAdjustment, refused.Scopes.WindowAdjustment);
        fixture.AssertNotOpened(refused);
    }

    /// <summary>Proves a file operation awaiting a decision refuses the picker.</summary>
    [TestMethod]
    [DataRow("name")]
    [DataRow("confirmation")]
    [DataRow("conflict")]
    public async Task HandleAsyncWhenOperationAwaitsADecisionRefusesThePickerAsync(string decision)
    {
        using Fixture fixture = await Fixture.ListedAsync();
        OperationActivity awaiting = await fixture.AwaitDecisionAsync(decision);

        CommanderSnapshot refused = await fixture.HandleAsync(UserIntent.OpenLocations);

        Assert.AreSame(awaiting, refused.Panes.Operation);
        fixture.AssertNotOpened(refused);
    }

    /// <summary>Proves a running file operation refuses the picker and completes untouched.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenOperationIsRunningRefusesThePickerAsync()
    {
        DirectoryListing leftListing = Listing("C:\\left", "item.txt");
        BlockingInspectionPort port = BlockingInspectionPort.Create(Inspection(leftListing.Entries[0].Path));
        using Fixture fixture = await Fixture.ListedAsync(port);
        Task<CommanderSnapshot> move = fixture.HandleAsync(UserIntent.Move);

        CommanderSnapshot refused = await fixture.HandleAsync(UserIntent.OpenLocations);

        _ = Assert.IsInstanceOfType<OperationRunning>(refused.Panes.Operation);
        fixture.AssertNotOpened(refused);
        _ = await fixture.CompleteBlockedOperationAsync(port, move);
    }

    /// <summary>Proves a pane read in flight on either side refuses the picker.</summary>
    [TestMethod]
    [DataRow("left")]
    [DataRow("right")]
    public async Task HandleAsyncWhenPaneReadIsInFlightRefusesThePickerAsync(string sideName)
    {
        using Fixture fixture = await Fixture.ListedAsync();
        PaneSide side = sideName == "left" ? PaneSide.Left : PaneSide.Right;
        TaskCompletionSource<DirectoryReadOutcome> read = fixture.PortOf(side).EnqueuePending();
        Task<CommanderSnapshot> pending = fixture.Session.NavigateAsync(side, ParsePath("C:\\target"), CancellationToken.None);

        CommanderSnapshot refused = await fixture.HandleAsync(UserIntent.OpenLocations);

        _ = Assert.IsInstanceOfType<PaneLoading>(refused.Panes.Of(side).Activity);
        fixture.AssertNotOpened(refused);
        read.SetResult(DirectoryReadOutcome.Cancelled());
        _ = await pending;
    }

    /// <summary>Proves the palette dispatches the catalog command after closing itself.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaletteSubmitsOpenLocationsOpensThePickerAsync()
    {
        using Fixture fixture = await Fixture.ListedWithCatalogsAsync();
        CommandPaletteOpen palette = Assert.IsInstanceOfType<CommandPaletteOpen>(
            (await fixture.HandleAsync(UserIntent.OpenCommandPalette)).Scopes.CommandPalette);

        CommanderSnapshot opened = await fixture.HandleAsync(UserIntent.SubmitCommand(palette, UserIntent.OpenLocations));

        Assert.AreSame(CommandPaletteState.Closed, opened.Scopes.CommandPalette);
        _ = Assert.IsInstanceOfType<LocationsOpen>(opened.Scopes.Locations);
    }

    /// <summary>
    /// Proves the loading and the open picker freeze every pane, settings, bookmark, address, palette,
    /// window, and operation intent, and refuse external navigation without a read.
    /// </summary>
    [TestMethod]
    [DataRow("loading")]
    [DataRow("open")]
    public async Task HandleAsyncWhenPickerOwnsInputFreezesEveryOtherIntentAsync(string phase)
    {
        using Fixture fixture = phase == "open" ? await Fixture.ListedWithCatalogsAsync() : await Fixture.ListedAsync();
        Task<CommanderSnapshot> opening = fixture.HandleAsync(UserIntent.OpenLocations);
        CommanderSnapshot before = phase == "open" ? await opening : fixture.Session.Current;
        UserIntent[] intents =
        [
            UserIntent.ActivateOtherPane,
            UserIntent.ToggleHiddenItems,
            UserIntent.Refresh,
            UserIntent.OpenFocused,
            UserIntent.NavigateParent,
            UserIntent.Copy,
            UserIntent.Delete,
            UserIntent.Rename,
            UserIntent.SortByName,
            UserIntent.OpenSettings,
            UserIntent.OpenBookmarks,
            UserIntent.OpenCommandPalette,
            UserIntent.OpenWindowAdjustment,
            UserIntent.OpenLocations,
            UserIntent.FocusAddress,
            UserIntent.BeginAddressEdit(PaneSide.Left),
            UserIntent.BookmarkSlotOne,
            UserIntent.Escape,
            UserIntent.Confirm,
        ];

        foreach (UserIntent intent in intents)
        {
            CommanderSnapshot after = await fixture.HandleAsync(intent);
            Fixture.AssertUnchanged(before, after);
        }
        CommanderSnapshot navigated = await fixture.Session.NavigateAsync(
            PaneSide.Left,
            ParsePath("C:\\elsewhere"),
            CancellationToken.None);

        Fixture.AssertUnchanged(before, navigated);
        Assert.HasCount(1, fixture.Left.Requests);
        Assert.HasCount(1, fixture.Right.Requests);
        if (phase == "loading")
        {
            fixture.Drives.Complete(LocationsSessionTests.DrivesOf());
            fixture.WslRoots.Complete(LocationsSessionTests.RootsOf());
            _ = await opening;
        }
    }

    /// <summary>Proves focus moves stay inside the picker and never move the pane focus.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenFocusMovesMovesOnlyThePickerFocusAsync()
    {
        using Fixture fixture = await Fixture.ListedWithCatalogsAsync();
        CommanderSnapshot opened = await fixture.HandleAsync(UserIntent.OpenLocations);
        LocationsOpen open = Assert.IsInstanceOfType<LocationsOpen>(opened.Scopes.Locations);

        CommanderSnapshot moved = await fixture.HandleAsync(UserIntent.MoveNext);

        LocationsOpen next = Assert.IsInstanceOfType<LocationsOpen>(moved.Scopes.Locations);
        Assert.AreSame(open.Items[1], next.FocusItem);
        Assert.AreSame(opened.Panes.Left, moved.Panes.Left);
        Assert.AreSame(opened.Panes.Right, moved.Panes.Right);
    }

    /// <summary>
    /// Proves a selection closes the picker before the read starts, reads the selected root on the
    /// pane active at open exactly once, and lists it on success.
    /// </summary>
    [TestMethod]
    public async Task HandleAsyncWhenSelectionIsAcceptedNavigatesTheActivePaneOnceAsync()
    {
        using Fixture fixture = await Fixture.ListedWithCatalogsAsync();
        _ = await fixture.HandleAsync(UserIntent.ActivateOtherPane);
        LocationsOpen open = Assert.IsInstanceOfType<LocationsOpen>(
            (await fixture.HandleAsync(UserIntent.OpenLocations)).Scopes.Locations);
        TaskCompletionSource<DirectoryReadOutcome> read = fixture.Right.EnqueuePending();

        Task<CommanderSnapshot> selecting = fixture.HandleAsync(UserIntent.SelectLocation(open, open.Items[1]));

        Assert.AreSame(LocationsState.Closed, fixture.Session.Current.Scopes.Locations);
        _ = Assert.IsInstanceOfType<PaneLoading>(fixture.Session.Current.Panes.Right.Activity);
        Assert.HasCount(2, fixture.Right.Requests);
        Assert.HasCount(1, fixture.Left.Requests);
        Assert.AreSame(open.Items[1].Location, fixture.Right.Requests[1].Location);
        read.SetResult(DirectoryReadOutcome.Succeeded(Listing("\\\\wsl.localhost\\Ubuntu", "home")));
        CommanderSnapshot selected = await selecting;
        PaneContentListed listed = Assert.IsInstanceOfType<PaneContentListed>(selected.Panes.Right.Content);
        Assert.AreEqual("\\\\wsl.localhost\\Ubuntu\\", listed.Listing.Location.CanonicalText);
        Assert.AreSame(LocationsState.Closed, selected.Scopes.Locations);
        Assert.HasCount(2, fixture.Right.Requests);
    }

    /// <summary>Proves a failed read of the selected root appears only through the pane, with no retry.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenSelectedRootFailsToReadShowsThePaneFailureAsync()
    {
        using Fixture fixture = await Fixture.ListedWithCatalogsAsync();
        LocationsOpen open = Assert.IsInstanceOfType<LocationsOpen>(
            (await fixture.HandleAsync(UserIntent.OpenLocations)).Scopes.Locations);
        fixture.Left.Enqueue(DirectoryReadOutcome.Failed(FileOperationFailureKind.ProviderUnavailable));

        CommanderSnapshot failed = await fixture.HandleAsync(UserIntent.SelectLocation(open, open.Items[0]));

        _ = Assert.IsInstanceOfType<PaneReadFailed>(failed.Panes.Left.Activity);
        Assert.AreSame(LocationsState.Closed, failed.Scopes.Locations);
        Assert.HasCount(2, fixture.Left.Requests);
    }

    /// <summary>Proves a qualified cancellation closes the picker without reading.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPickerIsCancelledClosesWithoutReadAsync()
    {
        using Fixture fixture = await Fixture.ListedWithCatalogsAsync();
        CommanderSnapshot opened = await fixture.HandleAsync(UserIntent.OpenLocations);
        LocationsOpen open = Assert.IsInstanceOfType<LocationsOpen>(opened.Scopes.Locations);

        CommanderSnapshot cancelled = await fixture.HandleAsync(UserIntent.CancelLocations(open));

        Assert.AreSame(LocationsState.Closed, cancelled.Scopes.Locations);
        Assert.AreSame(opened.Panes.Left, cancelled.Panes.Left);
        Assert.HasCount(1, fixture.Left.Requests);
    }

    private static DirectoryListing Listing(string location, string name)
    {
        DirectoryEntry entry = DirectoryEntry.Create(
            ParsePath(location + "\\" + name),
            name,
            DirectoryEntryKind.File,
            EntryVisibility.Normal);
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(DirectoryListing.Create(
            ParsePath(location),
            [entry],
            DirectoryListingCompleteness.Complete,
            0)).Listing;
    }

    private static FileInspectionOutcome Inspection(FileSystemPath path)
    {
        return InspectionWith(path, DeletionCapability.Recycle);
    }

    private static FileInspectionOutcome InspectionWith(FileSystemPath path, DeletionCapability capability)
    {
        FileIdentityAccepted identity = Assert.IsInstanceOfType<FileIdentityAccepted>(
            FileIdentity.Parse("identity:" + path.CanonicalText));
        return FileInspectionOutcome.Succeeded(FileEntrySnapshot.Create(path, identity.Identity, capability));
    }

    private static FileSystemPath ParsePath(string text)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path;
    }

    private sealed class Fixture : System.IDisposable
    {
        private readonly FileOperationGateway _gateway;
        private readonly RecordingCommanderObserver _observer = new();

        private Fixture(IFileOperationPort port, ScriptedFileOperationPort scripted)
        {
            Port = scripted;
            _gateway = new FileOperationGateway(port);
            PaneSession left = new(Left, new ScriptedFileLauncher(), Capacity(), DirectoryListing.EntryBoundaryLimit, HiddenItemVisibility.Hidden);
            PaneSession right = new(Right, new ScriptedFileLauncher(), Capacity(), DirectoryListing.EntryBoundaryLimit, HiddenItemVisibility.Hidden);
            BookmarkDisplayName name = Assert.IsInstanceOfType<BookmarkDisplayNameAccepted>(
                BookmarkDisplayName.Parse("Target")).Name;
            BookmarkPath path = Assert.IsInstanceOfType<BookmarkPathAccepted>(
                BookmarkPath.Parse("C:\\bookmark")).Path;
            BookmarkCatalog catalog = Assert.IsInstanceOfType<BookmarkCatalogAccepted>(BookmarkCatalog.Create(
                [],
                [BookmarkEntry.Create(name, path, null, BookmarkShortcutSlot.One)])).Catalog;
            Session = new CommanderSession(
                new DualPaneSession(left, right, _gateway),
                new SettingsSession(
                    new ScriptedSettingsStore(SettingsReadOutcome.Absent()),
                    SettingsReadOutcome.Read(UserSettings.Create(
                        ColorScheme.NeNeDark,
                        HiddenItemVisibility.Hidden,
                        catalog)),
                    static _ => { }),
                new TransientScopeOwners(
                    new AddressEditorSession(),
                    new CommandPaletteSession(),
                    new WindowAdjustmentSession(),
                    new LocationsSession(Drives, WslRoots)));
        }

        internal ScriptedDirectoryReadPort Left { get; } = ScriptedDirectoryReadPort.Create();

        internal ScriptedDirectoryReadPort Right { get; } = ScriptedDirectoryReadPort.Create();

        internal ScriptedDriveCatalog Drives { get; } = new();

        internal ScriptedWslDistributionCatalog WslRoots { get; } = new();

        internal ScriptedFileOperationPort Port { get; }

        internal CommanderSession Session { get; }

        internal static async Task<Fixture> ListedAsync()
        {
            ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
            Fixture fixture = new(port, port);
            await fixture.ListBothAsync();
            return fixture;
        }

        internal static async Task<Fixture> ListedWithCatalogsAsync()
        {
            Fixture fixture = await ListedAsync();
            fixture.Drives.Complete(LocationsSessionTests.DrivesOf(LocationsSessionTests.Drive("D:\\", DriveKind.Fixed, "Data")));
            fixture.WslRoots.Complete(LocationsSessionTests.RootsOf("Ubuntu"));
            return fixture;
        }

        internal static async Task<Fixture> ListedAsync(BlockingInspectionPort port)
        {
            Fixture fixture = new(port, ScriptedFileOperationPort.Create(null, null));
            await fixture.ListBothAsync();
            return fixture;
        }

        internal Task<CommanderSnapshot> HandleAsync(UserIntent intent)
        {
            return Session.HandleAsync(intent, _observer, CancellationToken.None);
        }

        internal ScriptedDirectoryReadPort PortOf(PaneSide side)
        {
            return side == PaneSide.Left ? Left : Right;
        }

        internal void AssertNotOpened(CommanderSnapshot snapshot)
        {
            Assert.AreSame(LocationsState.Closed, snapshot.Scopes.Locations);
            Assert.AreSame(LocationsState.Closed, Session.Current.Scopes.Locations);
            Assert.AreEqual(0, Drives.CallCount);
            Assert.AreEqual(0, WslRoots.CallCount);
        }

        internal static void AssertUnchanged(CommanderSnapshot before, CommanderSnapshot after)
        {
            Assert.AreSame(before.Panes.Left, after.Panes.Left);
            Assert.AreSame(before.Panes.Right, after.Panes.Right);
            Assert.AreSame(before.Panes.ActiveSide, after.Panes.ActiveSide);
            Assert.AreSame(before.Panes.Operation, after.Panes.Operation);
            Assert.AreSame(before.Settings.Editor, after.Settings.Editor);
            Assert.AreSame(before.Scopes.AddressEditor, after.Scopes.AddressEditor);
            Assert.AreSame(before.Scopes.CommandPalette, after.Scopes.CommandPalette);
            Assert.AreSame(before.Scopes.WindowAdjustment, after.Scopes.WindowAdjustment);
            Assert.AreSame(before.Scopes.Locations, after.Scopes.Locations);
        }

        internal async Task<OperationActivity> AwaitDecisionAsync(string decision)
        {
            DirectoryListing leftListing = Listing("C:\\left", "item.txt");
            if (decision == "name")
            {
                return Assert.IsInstanceOfType<OperationAwaitingName>((await HandleAsync(UserIntent.Rename)).Panes.Operation);
            }
            if (decision == "confirmation")
            {
                Port.EnqueueInspection(InspectionWith(leftListing.Entries[0].Path, DeletionCapability.PermanentOnly));
                return Assert.IsInstanceOfType<OperationAwaitingConfirmation>((await HandleAsync(UserIntent.Delete)).Panes.Operation);
            }
            FileInspectionSucceeded inspection = Assert.IsInstanceOfType<FileInspectionSucceeded>(
                Inspection(leftListing.Entries[0].Path));
            TransferConflict conflict = TransferConflict.Create(
                inspection.Snapshot,
                ParsePath("C:\\right\\item.txt"),
                ParsePath("C:\\right\\item (2).txt"));
            Port.EnqueueInspection(inspection);
            Port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([conflict]));
            return Assert.IsInstanceOfType<OperationAwaitingConflict>((await HandleAsync(UserIntent.Copy)).Panes.Operation);
        }

        internal async Task<CommanderSnapshot> CompleteBlockedOperationAsync(
            BlockingInspectionPort port,
            Task<CommanderSnapshot> operation)
        {
            Left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "item.txt")));
            Right.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\right", "item.txt")));
            port.Release();
            return await operation;
        }

        public void Dispose()
        {
            _gateway.Dispose();
        }

        private async Task ListBothAsync()
        {
            Left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "item.txt")));
            Right.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\right", "item.txt")));
            _ = await Session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
            _ = await Session.NavigateAsync(PaneSide.Right, ParsePath("C:\\right"), CancellationToken.None);
        }

        private static VisiblePageCapacity Capacity()
        {
            return Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(VisiblePageCapacity.Create(4)).Capacity;
        }
    }
}
