using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves visibility, sort order, and navigation history survive reads through one pane session.</summary>
[TestClass]
public sealed class PaneSessionNavigationStateTests
{
    /// <summary>Proves the composed visibility decides the first listing's visible set.</summary>
    [TestMethod]
    public async Task NavigateAsyncWhenComposedVisibilityOmitsHiddenEntriesListsOnlyTheVisibleOnes()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = PaneSessionTestContext.HiddenListing(
            "C:\\root",
            ("a.txt", EntryVisibility.Normal),
            ("b.txt", EntryVisibility.Hidden));
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        PaneSession session = PaneSessionTestContext.CreateSession(port, HiddenItemVisibility.Hidden);

        PaneSnapshot snapshot = await session.NavigateAsync(PaneSessionTestContext.ParsePath("C:\\root"), CancellationToken.None);

        PaneContentListed listed = Assert.IsInstanceOfType<PaneContentListed>(snapshot.Content);
        Assert.HasCount(2, listed.Listing.Entries);
        Assert.HasCount(1, listed.State.VisibleEntries);
        Assert.AreSame(listing.Entries[0], listed.State.VisibleEntries[0]);
        Assert.AreSame(HiddenItemVisibility.Hidden, listed.State.HiddenItemVisibility);
    }

    /// <summary>Proves every later read carries the listed state's own visibility, not a session copy.</summary>
    [TestMethod]
    public async Task NavigateAsyncWhenVisibilityShowsHiddenEntriesEveryLocationListsThem()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing first = PaneSessionTestContext.HiddenListing("C:\\root", ("a.txt", EntryVisibility.Hidden));
        DirectoryListing second = PaneSessionTestContext.HiddenListing("C:\\other", ("b.txt", EntryVisibility.Hidden));
        port.Enqueue(DirectoryReadOutcome.Succeeded(first, null));
        port.Enqueue(DirectoryReadOutcome.Succeeded(second, null));
        PaneSession session = PaneSessionTestContext.CreateSession(port, HiddenItemVisibility.Shown);

        PaneSnapshot initial = await session.NavigateAsync(PaneSessionTestContext.ParsePath("C:\\root"), CancellationToken.None);
        PaneSnapshot later = await session.NavigateAsync(PaneSessionTestContext.ParsePath("C:\\other"), CancellationToken.None);

        Assert.HasCount(1, Assert.IsInstanceOfType<PaneContentListed>(initial.Content).State.VisibleEntries);
        PaneContentListed listed = Assert.IsInstanceOfType<PaneContentListed>(later.Content);
        Assert.HasCount(1, listed.State.VisibleEntries);
        Assert.AreSame(second.Entries[0], listed.State.VisibleEntries[0]);
        Assert.AreSame(HiddenItemVisibility.Shown, listed.State.HiddenItemVisibility);
    }

    /// <summary>Proves Back and Forward read the retained locations and commit only their cursor.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenHistoryMovesBackAndForwardReadsLocationsAndClearsSelection()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing root = PaneSessionTestContext.Listing("C:\\root", ("z.txt", DirectoryEntryKind.File));
        DirectoryListing other = PaneSessionTestContext.Listing("C:\\other", ("b.txt", DirectoryEntryKind.File));
        port.Enqueue(DirectoryReadOutcome.Succeeded(root, null));
        port.Enqueue(DirectoryReadOutcome.Succeeded(other, null));
        port.Enqueue(DirectoryReadOutcome.Succeeded(root, null));
        port.Enqueue(DirectoryReadOutcome.Succeeded(other, null));
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        _ = await session.NavigateAsync(root.Location, CancellationToken.None);
        _ = await session.NavigateAsync(other.Location, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.ToggleSelection, CancellationToken.None);

        PaneSnapshot backed = await session.HandleAsync(UserIntent.NavigateBack, CancellationToken.None);
        PaneSnapshot forwarded = await session.HandleAsync(UserIntent.NavigateForward, CancellationToken.None);

        Assert.AreSame(root.Location, port.Requests[2].Location);
        Assert.AreSame(other.Location, port.Requests[3].Location);
        PaneContentListed backListed = Assert.IsInstanceOfType<PaneContentListed>(backed.Content);
        Assert.AreSame(root.Entries[0].Path, backListed.State.FocusItem);
        Assert.IsEmpty(backListed.State.Selection);
        Assert.AreEqual(0, backListed.State.NavigationHistory.CurrentIndex);
        PaneNavigationHistory history = PaneSessionTestContext.HistoryOf(forwarded);
        Assert.AreEqual(1, history.CurrentIndex);
        Assert.HasCount(2, history.Locations);
    }

    /// <summary>Proves unavailable directions return the current snapshot without starting a read.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenHistoryHasNoCandidateDoesNotRead()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(PaneSessionTestContext.Listing("C:\\root"), null));
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        PaneSnapshot listed = await session.NavigateAsync(PaneSessionTestContext.ParsePath("C:\\root"), CancellationToken.None);

        PaneSnapshot back = await session.HandleAsync(UserIntent.NavigateBack, CancellationToken.None);
        PaneSnapshot forward = await session.HandleAsync(UserIntent.NavigateForward, CancellationToken.None);

        Assert.AreSame(listed, back);
        Assert.AreSame(listed, forward);
        Assert.HasCount(1, port.Requests);
    }

    /// <summary>Proves failed and cancelled Back reads leave the same candidate available for retry.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenBackFailsOrIsCancelledPreservesHistoryUntilSuccess()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing root = PaneSessionTestContext.Listing("C:\\root");
        DirectoryListing other = PaneSessionTestContext.Listing("C:\\other");
        port.Enqueue(DirectoryReadOutcome.Succeeded(root, null));
        port.Enqueue(DirectoryReadOutcome.Succeeded(other, null));
        port.Enqueue(DirectoryReadOutcome.Failed(FileOperationFailureKind.AccessDenied));
        port.Enqueue(DirectoryReadOutcome.Cancelled());
        port.Enqueue(DirectoryReadOutcome.Succeeded(root, null));
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        _ = await session.NavigateAsync(root.Location, CancellationToken.None);
        _ = await session.NavigateAsync(other.Location, CancellationToken.None);

        PaneSnapshot failed = await session.HandleAsync(UserIntent.NavigateBack, CancellationToken.None);
        PaneSnapshot cancelled = await session.HandleAsync(UserIntent.NavigateBack, CancellationToken.None);
        PaneSnapshot succeeded = await session.HandleAsync(UserIntent.NavigateBack, CancellationToken.None);

        Assert.AreEqual(1, PaneSessionTestContext.HistoryOf(failed).CurrentIndex);
        Assert.AreEqual(1, PaneSessionTestContext.HistoryOf(cancelled).CurrentIndex);
        Assert.AreEqual(0, PaneSessionTestContext.HistoryOf(succeeded).CurrentIndex);
        Assert.AreSame(root.Location, port.Requests[2].Location);
        Assert.AreSame(root.Location, port.Requests[3].Location);
        Assert.AreSame(root.Location, port.Requests[4].Location);
    }

    /// <summary>Proves same-location and refresh reads retain a Forward candidate after Back.</summary>
    [TestMethod]
    public async Task NavigateAndRefreshWhenLocationIdentityIsUnchangedPreserveForwardHistory()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing root = PaneSessionTestContext.Listing("C:\\root");
        DirectoryListing other = PaneSessionTestContext.Listing("C:\\other");
        DirectoryListing sameIdentity = PaneSessionTestContext.Listing("c:\\ROOT");
        port.Enqueue(DirectoryReadOutcome.Succeeded(root, null));
        port.Enqueue(DirectoryReadOutcome.Succeeded(other, null));
        port.Enqueue(DirectoryReadOutcome.Succeeded(root, null));
        port.Enqueue(DirectoryReadOutcome.Succeeded(sameIdentity, null));
        port.Enqueue(DirectoryReadOutcome.Succeeded(sameIdentity, null));
        port.Enqueue(DirectoryReadOutcome.Succeeded(other, null));
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        _ = await session.NavigateAsync(root.Location, CancellationToken.None);
        _ = await session.NavigateAsync(other.Location, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.NavigateBack, CancellationToken.None);

        _ = await session.NavigateAsync(sameIdentity.Location, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.Refresh, CancellationToken.None);
        PaneSnapshot forwarded = await session.HandleAsync(UserIntent.NavigateForward, CancellationToken.None);

        PaneNavigationHistory history = PaneSessionTestContext.HistoryOf(forwarded);
        Assert.HasCount(2, history.Locations);
        Assert.AreEqual(1, history.CurrentIndex);
        Assert.AreSame(other.Location, Assert.IsInstanceOfType<PaneContentListed>(forwarded.Content).State.Location);
    }

    /// <summary>Proves the first read uses the default order and a sort intent keeps the focus item.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenSortIntentArrivesReordersListedPaneKeepingFocus()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing root = PaneSessionTestContext.Listing(
            "C:\\root",
            ("b.txt", DirectoryEntryKind.File),
            ("a.md", DirectoryEntryKind.File),
            ("c", DirectoryEntryKind.File));
        port.Enqueue(DirectoryReadOutcome.Succeeded(root, null));
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        PaneSnapshot initial = await session.NavigateAsync(root.Location, CancellationToken.None);

        PaneSnapshot sorted = await session.HandleAsync(UserIntent.SortByExtension, CancellationToken.None);

        PaneContentListed before = Assert.IsInstanceOfType<PaneContentListed>(initial.Content);
        PaneContentListed after = Assert.IsInstanceOfType<PaneContentListed>(sorted.Content);
        Assert.AreEqual(PaneSortOrder.Default, before.State.SortOrder);
        PaneSessionTestContext.AssertVisibleNames(["c", "a.md", "b.txt"], after.State);
        Assert.AreSame(before.State.FocusItem, after.State.FocusItem);
        Assert.AreSame(root, after.Listing);
        Assert.HasCount(1, port.Requests);
    }

    /// <summary>Proves the pane order survives refresh, a new location, and Back.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenSortedCarriesOrderThroughRefreshNavigationAndHistory()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing root = PaneSessionTestContext.Listing(
            "C:\\root",
            ("file10.txt", DirectoryEntryKind.File),
            ("a.md", DirectoryEntryKind.File),
            ("file2.txt", DirectoryEntryKind.File));
        DirectoryListing next = PaneSessionTestContext.Listing("C:\\next", ("y10.txt", DirectoryEntryKind.File), ("y2.txt", DirectoryEntryKind.File), ("z.md", DirectoryEntryKind.File));
        port.Enqueue(DirectoryReadOutcome.Succeeded(root, null));
        port.Enqueue(DirectoryReadOutcome.Succeeded(root, null));
        port.Enqueue(DirectoryReadOutcome.Succeeded(next, null));
        port.Enqueue(DirectoryReadOutcome.Succeeded(root, null));
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        _ = await session.NavigateAsync(root.Location, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.SortByExtension, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.SortByExtension, CancellationToken.None);

        PaneSnapshot refreshed = await session.HandleAsync(UserIntent.Refresh, CancellationToken.None);
        PaneSnapshot moved = await session.NavigateAsync(next.Location, CancellationToken.None);
        PaneSnapshot back = await session.HandleAsync(UserIntent.NavigateBack, CancellationToken.None);

        PaneState refreshedState = Assert.IsInstanceOfType<PaneContentListed>(refreshed.Content).State;
        PaneState movedState = Assert.IsInstanceOfType<PaneContentListed>(moved.Content).State;
        PaneState backState = Assert.IsInstanceOfType<PaneContentListed>(back.Content).State;
        PaneSessionTestContext.AssertVisibleNames(["file2.txt", "file10.txt", "a.md"], refreshedState);
        Assert.AreSame(root.Entries[0].Path, refreshedState.FocusItem);
        PaneSessionTestContext.AssertVisibleNames(["y2.txt", "y10.txt", "z.md"], movedState);
        Assert.AreSame(movedState.VisibleEntries[0].Path, movedState.FocusItem);
        PaneSessionTestContext.AssertVisibleNames(["file2.txt", "file10.txt", "a.md"], backState);
        Assert.AreSame(backState.VisibleEntries[0].Path, backState.FocusItem);
        Assert.AreSame(SortKey.Extension, backState.SortOrder.Key);
        Assert.AreSame(SortDirection.Descending, backState.SortOrder.Direction);
    }
}
