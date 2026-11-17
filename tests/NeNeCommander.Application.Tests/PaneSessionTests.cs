using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Drives;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves pane reads, directory navigation, and in-flight read ownership use the canonical route.</summary>
[TestClass]
public sealed class PaneSessionTests
{
    /// <summary>Proves a read fact follows retained content and is replaced only by another successful read.</summary>
    [TestMethod]
    public async Task NavigateWhenDriveFactChangesRetainsAndReplacesObservedKind()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = PaneSessionTestContext.Listing("C:\\root",
            ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, DriveKind.Network));
        port.Enqueue(DirectoryReadOutcome.Failed(FileOperationFailureKind.AccessDenied));
        port.Enqueue(DirectoryReadOutcome.Cancelled());
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, DriveKind.Fixed));
        PaneSession session = PaneSessionTestContext.CreateSession(port);

        PaneSnapshot listed = await session.NavigateAsync(listing.Location, CancellationToken.None);
        PaneSnapshot focused = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);
        PaneSnapshot selected = await session.HandleAsync(UserIntent.ToggleSelection, CancellationToken.None);
        PaneSnapshot sorted = await session.HandleAsync(UserIntent.SortByExtension, CancellationToken.None);
        PaneSnapshot failed = await session.NavigateAsync(PaneSessionTestContext.ParsePath("C:\\denied"), CancellationToken.None);
        PaneSnapshot cancelled = await session.NavigateAsync(PaneSessionTestContext.ParsePath("C:\\cancelled"), CancellationToken.None);
        PaneSnapshot refreshed = await session.RefreshAsync(CancellationToken.None);

        Assert.AreSame(DriveKind.Network, Assert.IsInstanceOfType<PaneContentListed>(listed.Content).DriveKind);
        Assert.AreSame(DriveKind.Network, Assert.IsInstanceOfType<PaneContentListed>(focused.Content).DriveKind);
        Assert.AreNotSame(
            Assert.IsInstanceOfType<PaneContentListed>(listed.Content).State.FocusItem,
            Assert.IsInstanceOfType<PaneContentListed>(focused.Content).State.FocusItem);
        Assert.AreSame(DriveKind.Network, Assert.IsInstanceOfType<PaneContentListed>(selected.Content).DriveKind);
        Assert.AreSame(DriveKind.Network, Assert.IsInstanceOfType<PaneContentListed>(sorted.Content).DriveKind);
        Assert.AreSame(DriveKind.Network, Assert.IsInstanceOfType<PaneContentListed>(failed.Content).DriveKind);
        Assert.AreSame(DriveKind.Network, Assert.IsInstanceOfType<PaneContentListed>(cancelled.Content).DriveKind);
        Assert.AreSame(DriveKind.Fixed, Assert.IsInstanceOfType<PaneContentListed>(refreshed.Content).DriveKind);
    }

    /// <summary>Proves a successful read lists the location with the first entry focused.</summary>
    [TestMethod]
    public async Task NavigateAsyncWhenReadSucceedsListsLocationWithFirstEntryFocused()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = PaneSessionTestContext.Listing("C:\\root", ("docs", DirectoryEntryKind.Directory), ("a.txt", DirectoryEntryKind.File));
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        PaneSession session = PaneSessionTestContext.CreateSession(port);

        PaneSnapshot snapshot = await session.NavigateAsync(PaneSessionTestContext.ParsePath("C:\\root"), CancellationToken.None);

        PaneContentListed listed = Assert.IsInstanceOfType<PaneContentListed>(snapshot.Content);
        Assert.AreSame(listing, listed.Listing);
        Assert.AreSame(listing.Entries[0].Path, listed.State.FocusItem);
        Assert.IsEmpty(listed.State.Selection);
        Assert.AreSame(PaneActivity.Idle, snapshot.Activity);
        Assert.AreSame(snapshot, session.Current);
        Assert.HasCount(1, port.Requests);
        Assert.AreEqual(DirectoryListing.EntryBoundaryLimit, port.Requests[0].EntryBoundary);
    }

    /// <summary>Proves a failed read keeps the previous content and reports the typed failure.</summary>
    [TestMethod]
    public async Task NavigateAsyncWhenReadFailsKeepsContentAndReportsFailure()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = PaneSessionTestContext.Listing("C:\\root", ("a.txt", DirectoryEntryKind.File));
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        port.Enqueue(DirectoryReadOutcome.Failed(FileOperationFailureKind.AccessDenied));
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        FileSystemPath denied = PaneSessionTestContext.ParsePath("C:\\root\\denied");

        _ = await session.NavigateAsync(PaneSessionTestContext.ParsePath("C:\\root"), CancellationToken.None);
        PaneSnapshot snapshot = await session.NavigateAsync(denied, CancellationToken.None);

        Assert.AreSame(listing, Assert.IsInstanceOfType<PaneContentListed>(snapshot.Content).Listing);
        PaneReadFailed failed = Assert.IsInstanceOfType<PaneReadFailed>(snapshot.Activity);
        Assert.AreSame(denied, failed.Target);
        Assert.AreSame(FileOperationFailureKind.AccessDenied, failed.Failure);
    }

    /// <summary>Proves a cancelled read is a typed activity, not an error.</summary>
    [TestMethod]
    public async Task NavigateAsyncWhenReadIsCancelledReportsCancelledActivity()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Cancelled());
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        FileSystemPath target = PaneSessionTestContext.ParsePath("C:\\root");

        PaneSnapshot snapshot = await session.NavigateAsync(target, CancellationToken.None);

        Assert.AreSame(PaneContent.Absent, snapshot.Content);
        Assert.AreSame(target, Assert.IsInstanceOfType<PaneReadCancelled>(snapshot.Activity).Target);
    }

    /// <summary>Proves an unregistered outcome variant is a defect, not a silent state.</summary>
    [TestMethod]
    public async Task NavigateAsyncWhenOutcomeVariantIsUnsupportedThrows()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(new UnsupportedDirectoryReadOutcome());
        PaneSession session = PaneSessionTestContext.CreateSession(port);

        InvalidOperationException failure = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            async () => await session.NavigateAsync(PaneSessionTestContext.ParsePath("C:\\root"), CancellationToken.None));

        Assert.AreEqual("The directory read outcome variant is not navigable.", failure.Message);
    }

    /// <summary>Proves intents are ignored before any listing exists.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenNothingIsListedReturnsInitialSnapshotWithoutRead()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        PaneSession session = PaneSessionTestContext.CreateSession(port);

        PaneSnapshot snapshot = await session.HandleAsync(UserIntent.NavigateParent, CancellationToken.None);

        Assert.AreSame(PaneSnapshot.Initial, snapshot);
        Assert.IsEmpty(port.Requests);
    }

    /// <summary>Proves movement intents pass through the reducer and irrelevant intents keep the snapshot.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenMovementIntentArrivesAppliesReducer()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = PaneSessionTestContext.Listing("C:\\root", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        PaneSnapshot listedSnapshot = await session.NavigateAsync(PaneSessionTestContext.ParsePath("C:\\root"), CancellationToken.None);

        PaneSnapshot moved = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);
        PaneSnapshot unchanged = await session.HandleAsync(UserIntent.Copy, CancellationToken.None);

        Assert.AreSame(listing.Entries[1].Path, Assert.IsInstanceOfType<PaneContentListed>(moved.Content).State.FocusItem);
        Assert.AreNotSame(listedSnapshot, moved);
        Assert.AreSame(moved, unchanged);
        Assert.HasCount(1, port.Requests);
    }

    /// <summary>Proves opening a focused directory reads it and lists it.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenOpenFocusedOnDirectoryNavigatesIntoIt()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing root = PaneSessionTestContext.Listing("C:\\root", ("docs", DirectoryEntryKind.Directory));
        DirectoryListing docs = PaneSessionTestContext.Listing("C:\\root\\docs", ("readme.md", DirectoryEntryKind.File));
        port.Enqueue(DirectoryReadOutcome.Succeeded(root, null));
        port.Enqueue(DirectoryReadOutcome.Succeeded(docs, null));
        ScriptedFileLauncher launcher = new();
        PaneSession session = PaneSessionTestContext.CreateSession(port, launcher);
        _ = await session.NavigateAsync(PaneSessionTestContext.ParsePath("C:\\root"), CancellationToken.None);

        PaneSnapshot snapshot = await session.HandleAsync(UserIntent.OpenFocused, CancellationToken.None);

        Assert.AreSame(root.Entries[0].Path, port.Requests[1].Location);
        PaneContentListed listed = Assert.IsInstanceOfType<PaneContentListed>(snapshot.Content);
        Assert.AreSame(docs, listed.Listing);
        Assert.AreSame(docs.Entries[0].Path, listed.State.FocusItem);
        Assert.IsEmpty(launcher.Targets);
    }

    /// <summary>Proves opening an empty listing starts neither a read nor a file handoff.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenOpenFocusedOnEmptyListingDoesNothing()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(PaneSessionTestContext.Listing("C:\\empty"), null));
        ScriptedFileLauncher launcher = new();
        PaneSession session = PaneSessionTestContext.CreateSession(port, launcher);

        PaneSnapshot emptyListed = await session.NavigateAsync(PaneSessionTestContext.ParsePath("C:\\empty"), CancellationToken.None);
        PaneSnapshot afterEmpty = await session.HandleAsync(UserIntent.OpenFocused, CancellationToken.None);

        Assert.AreSame(emptyListed, afterEmpty);
        Assert.HasCount(1, port.Requests);
        Assert.IsEmpty(launcher.Targets);
    }

    /// <summary>Proves navigating to the parent reads it and focuses the origin directory.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenNavigateParentFromNestedReadsParentAndFocusesOrigin()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing docs = PaneSessionTestContext.Listing("C:\\root\\docs", ("readme.md", DirectoryEntryKind.File));
        DirectoryListing root = PaneSessionTestContext.Listing("C:\\root", ("archive", DirectoryEntryKind.Directory), ("DOCS", DirectoryEntryKind.Directory));
        port.Enqueue(DirectoryReadOutcome.Succeeded(docs, null));
        port.Enqueue(DirectoryReadOutcome.Succeeded(root, null));
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        _ = await session.NavigateAsync(PaneSessionTestContext.ParsePath("C:\\root\\docs"), CancellationToken.None);

        PaneSnapshot snapshot = await session.HandleAsync(UserIntent.NavigateParent, CancellationToken.None);

        Assert.AreEqual("C:\\root", port.Requests[1].Location.CanonicalText);
        PaneContentListed listed = Assert.IsInstanceOfType<PaneContentListed>(snapshot.Content);
        Assert.AreSame(root.Entries[1].Path, listed.State.FocusItem);
    }

    /// <summary>Proves refresh re-reads the same location, keeps focus, and clears selection.</summary>
    [TestMethod]
    public async Task RefreshAsyncWhenListedReReadsLocationKeepingFocusAndClearingSelection()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing first = PaneSessionTestContext.Listing("C:\\root", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
        DirectoryListing second = PaneSessionTestContext.Listing("C:\\root", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File), ("c.txt", DirectoryEntryKind.File));
        port.Enqueue(DirectoryReadOutcome.Succeeded(first, null));
        port.Enqueue(DirectoryReadOutcome.Succeeded(second, null));
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        PaneSnapshot initial = await session.RefreshAsync(CancellationToken.None);
        _ = await session.NavigateAsync(PaneSessionTestContext.ParsePath("C:\\root"), CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.ToggleSelection, CancellationToken.None);

        PaneSnapshot refreshed = await session.HandleAsync(UserIntent.Refresh, CancellationToken.None);

        Assert.AreSame(PaneSnapshot.Initial, initial);
        Assert.AreEqual("C:\\root", port.Requests[1].Location.CanonicalText);
        PaneContentListed listed = Assert.IsInstanceOfType<PaneContentListed>(refreshed.Content);
        Assert.AreSame(second, listed.Listing);
        Assert.AreSame(second.Entries[1].Path, listed.State.FocusItem);
        Assert.IsEmpty(listed.State.Selection);
    }

    /// <summary>Proves the parent intent at a provider root starts no read.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenNavigateParentAtRootDoesNotRead()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(PaneSessionTestContext.Listing("C:\\", ("Users", DirectoryEntryKind.Directory)), null));
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        PaneSnapshot listed = await session.NavigateAsync(PaneSessionTestContext.ParsePath("C:\\"), CancellationToken.None);

        PaneSnapshot snapshot = await session.HandleAsync(UserIntent.NavigateParent, CancellationToken.None);

        Assert.AreSame(listed, snapshot);
        Assert.HasCount(1, port.Requests);
    }

    /// <summary>Proves intents are frozen while a read is in flight.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-016")]
    public async Task HandleAsyncWhenReadIsInFlightFreezesIntents()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing root = PaneSessionTestContext.Listing("C:\\root", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
        port.Enqueue(DirectoryReadOutcome.Succeeded(root, null));
        TaskCompletionSource<DirectoryReadOutcome> pending = port.EnqueuePending();
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        _ = await session.NavigateAsync(PaneSessionTestContext.ParsePath("C:\\root"), CancellationToken.None);
        FileSystemPath target = PaneSessionTestContext.ParsePath("C:\\other");

        Task<PaneSnapshot> navigation = session.NavigateAsync(target, CancellationToken.None);
        PaneSnapshot frozen = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);
        pending.SetResult(DirectoryReadOutcome.Succeeded(PaneSessionTestContext.Listing("C:\\other", ("c.txt", DirectoryEntryKind.File)), null));
        PaneSnapshot completed = await navigation;

        Assert.AreSame(target, Assert.IsInstanceOfType<PaneLoading>(frozen.Activity).Target);
        Assert.AreSame(root.Entries[0].Path, Assert.IsInstanceOfType<PaneContentListed>(frozen.Content).State.FocusItem);
        Assert.AreEqual("C:\\other", Assert.IsInstanceOfType<PaneContentListed>(completed.Content).Listing.Location.CanonicalText);
        Assert.HasCount(2, port.Requests);
    }

    /// <summary>Proves both refresh forms do nothing while a read is in flight, so a refresh never races the navigation it would duplicate.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-016")]
    public async Task RefreshAsyncWhenReadIsInFlightDoesNothing()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing root = PaneSessionTestContext.Listing("C:\\root", ("a.txt", DirectoryEntryKind.File));
        port.Enqueue(DirectoryReadOutcome.Succeeded(root, null));
        TaskCompletionSource<DirectoryReadOutcome> pending = port.EnqueuePending();
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        _ = await session.NavigateAsync(PaneSessionTestContext.ParsePath("C:\\root"), CancellationToken.None);

        Task<PaneSnapshot> navigation = session.NavigateAsync(PaneSessionTestContext.ParsePath("C:\\other"), CancellationToken.None);
        PaneSnapshot plain = await session.RefreshAsync(CancellationToken.None);
        PaneSnapshot focusing = await session.RefreshFocusingAsync(root.Entries[0].Path, CancellationToken.None);
        pending.SetResult(DirectoryReadOutcome.Succeeded(PaneSessionTestContext.Listing("C:\\other", ("c.txt", DirectoryEntryKind.File)), null));
        _ = await navigation;

        _ = Assert.IsInstanceOfType<PaneLoading>(plain.Activity);
        _ = Assert.IsInstanceOfType<PaneLoading>(focusing.Activity);
        Assert.HasCount(2, port.Requests);
    }

    /// <summary>Proves a read superseded by a newer navigation is discarded when it completes late.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-016")]
    public async Task NavigateAsyncWhenSupersededDiscardsStaleResult()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        TaskCompletionSource<DirectoryReadOutcome> first = port.EnqueuePending();
        TaskCompletionSource<DirectoryReadOutcome> second = port.EnqueuePending();
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        DirectoryListing stale = PaneSessionTestContext.Listing("C:\\stale", ("s.txt", DirectoryEntryKind.File));
        DirectoryListing fresh = PaneSessionTestContext.Listing("C:\\fresh", ("f.txt", DirectoryEntryKind.File));

        Task<PaneSnapshot> firstNavigation = session.NavigateAsync(PaneSessionTestContext.ParsePath("C:\\stale"), CancellationToken.None);
        Task<PaneSnapshot> secondNavigation = session.NavigateAsync(PaneSessionTestContext.ParsePath("C:\\fresh"), CancellationToken.None);
        second.SetResult(DirectoryReadOutcome.Succeeded(fresh, null));
        PaneSnapshot freshSnapshot = await secondNavigation;
        first.SetResult(DirectoryReadOutcome.Succeeded(stale, null));
        PaneSnapshot staleSnapshot = await firstNavigation;

        Assert.AreSame(fresh, Assert.IsInstanceOfType<PaneContentListed>(freshSnapshot.Content).Listing);
        Assert.AreSame(freshSnapshot, staleSnapshot);
        Assert.AreSame(freshSnapshot, session.Current);
        Assert.AreSame(PaneActivity.Idle, session.Current.Activity);
        PaneNavigationHistory history = PaneSessionTestContext.HistoryOf(session.Current);
        Assert.HasCount(1, history.Locations);
        Assert.AreSame(fresh.Location, history.Locations[0]);
    }

    /// <summary>Proves the composition boundary rejects an entry boundary outside the fixed range.</summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(DirectoryListing.EntryBoundaryLimit + 1)]
    public void ConstructWhenEntryBoundaryIsOutOfRangeThrowsArgumentOutOfRangeException(int entryBoundary)
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        VisiblePageCapacity capacity = PaneSessionTestContext.Capacity(4);

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new PaneSession(port,
            new ScriptedFileLauncher(),
            PaneSessionConfiguration.Create(capacity, entryBoundary, HiddenItemVisibility.Hidden, PaneSortOrder.Default)));
    }
}
