using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves pane navigation through the sole session coordinator.</summary>
[TestClass]
public sealed class PaneSessionTests
{
    /// <summary>Proves a successful read lists the location with the first entry focused.</summary>
    [TestMethod]
    public async Task NavigateAsyncWhenReadSucceedsListsLocationWithFirstEntryFocused()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = Listing("C:\\root", ("docs", DirectoryEntryKind.Directory), ("a.txt", DirectoryEntryKind.File));
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing));
        PaneSession session = CreateSession(port);

        PaneSnapshot snapshot = await session.NavigateAsync(ParsePath("C:\\root"), CancellationToken.None);

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
        DirectoryListing listing = Listing("C:\\root", ("a.txt", DirectoryEntryKind.File));
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing));
        port.Enqueue(DirectoryReadOutcome.Failed(FileOperationFailureKind.AccessDenied));
        PaneSession session = CreateSession(port);
        FileSystemPath denied = ParsePath("C:\\root\\denied");

        _ = await session.NavigateAsync(ParsePath("C:\\root"), CancellationToken.None);
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
        PaneSession session = CreateSession(port);
        FileSystemPath target = ParsePath("C:\\root");

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
        PaneSession session = CreateSession(port);

        InvalidOperationException failure = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            async () => await session.NavigateAsync(ParsePath("C:\\root"), CancellationToken.None));

        Assert.AreEqual("The directory read outcome variant is not navigable.", failure.Message);
    }

    /// <summary>Proves opening a focused file next to a directory starts no read.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenOpenFocusedOnFileBesideDirectoryDoesNotRead()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(
            Listing("C:\\root", ("docs", DirectoryEntryKind.Directory), ("a.txt", DirectoryEntryKind.File))));
        PaneSession session = CreateSession(port);
        _ = await session.NavigateAsync(ParsePath("C:\\root"), CancellationToken.None);
        PaneSnapshot onFile = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);

        PaneSnapshot afterOpen = await session.HandleAsync(UserIntent.OpenFocused, CancellationToken.None);

        Assert.AreSame(onFile, afterOpen);
        Assert.HasCount(1, port.Requests);
    }

    /// <summary>Proves intents are ignored before any listing exists.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenNothingIsListedReturnsInitialSnapshotWithoutRead()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        PaneSession session = CreateSession(port);

        PaneSnapshot snapshot = await session.HandleAsync(UserIntent.NavigateParent, CancellationToken.None);

        Assert.AreSame(PaneSnapshot.Initial, snapshot);
        Assert.IsEmpty(port.Requests);
    }

    /// <summary>Proves movement intents pass through the reducer and irrelevant intents keep the snapshot.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenMovementIntentArrivesAppliesReducer()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = Listing("C:\\root", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing));
        PaneSession session = CreateSession(port);
        PaneSnapshot listedSnapshot = await session.NavigateAsync(ParsePath("C:\\root"), CancellationToken.None);

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
        DirectoryListing root = Listing("C:\\root", ("docs", DirectoryEntryKind.Directory));
        DirectoryListing docs = Listing("C:\\root\\docs", ("readme.md", DirectoryEntryKind.File));
        port.Enqueue(DirectoryReadOutcome.Succeeded(root));
        port.Enqueue(DirectoryReadOutcome.Succeeded(docs));
        PaneSession session = CreateSession(port);
        _ = await session.NavigateAsync(ParsePath("C:\\root"), CancellationToken.None);

        PaneSnapshot snapshot = await session.HandleAsync(UserIntent.OpenFocused, CancellationToken.None);

        Assert.AreSame(root.Entries[0].Path, port.Requests[1].Location);
        PaneContentListed listed = Assert.IsInstanceOfType<PaneContentListed>(snapshot.Content);
        Assert.AreSame(docs, listed.Listing);
        Assert.AreSame(docs.Entries[0].Path, listed.State.FocusItem);
    }

    /// <summary>Proves opening a file or an empty listing starts no read.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenOpenFocusedOnFileOrEmptyListingDoesNotRead()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\root", ("a.txt", DirectoryEntryKind.File))));
        port.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\empty")));
        PaneSession session = CreateSession(port);

        PaneSnapshot fileListed = await session.NavigateAsync(ParsePath("C:\\root"), CancellationToken.None);
        PaneSnapshot afterFile = await session.HandleAsync(UserIntent.OpenFocused, CancellationToken.None);
        PaneSnapshot emptyListed = await session.NavigateAsync(ParsePath("C:\\empty"), CancellationToken.None);
        PaneSnapshot afterEmpty = await session.HandleAsync(UserIntent.OpenFocused, CancellationToken.None);

        Assert.AreSame(fileListed, afterFile);
        Assert.AreSame(emptyListed, afterEmpty);
        Assert.HasCount(2, port.Requests);
    }

    /// <summary>Proves navigating to the parent reads it and focuses the origin directory.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenNavigateParentFromNestedReadsParentAndFocusesOrigin()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing docs = Listing("C:\\root\\docs", ("readme.md", DirectoryEntryKind.File));
        DirectoryListing root = Listing("C:\\root", ("archive", DirectoryEntryKind.Directory), ("DOCS", DirectoryEntryKind.Directory));
        port.Enqueue(DirectoryReadOutcome.Succeeded(docs));
        port.Enqueue(DirectoryReadOutcome.Succeeded(root));
        PaneSession session = CreateSession(port);
        _ = await session.NavigateAsync(ParsePath("C:\\root\\docs"), CancellationToken.None);

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
        DirectoryListing first = Listing("C:\\root", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
        DirectoryListing second = Listing("C:\\root", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File), ("c.txt", DirectoryEntryKind.File));
        port.Enqueue(DirectoryReadOutcome.Succeeded(first));
        port.Enqueue(DirectoryReadOutcome.Succeeded(second));
        PaneSession session = CreateSession(port);
        PaneSnapshot initial = await session.RefreshAsync(CancellationToken.None);
        _ = await session.NavigateAsync(ParsePath("C:\\root"), CancellationToken.None);
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
        port.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\", ("Users", DirectoryEntryKind.Directory))));
        PaneSession session = CreateSession(port);
        PaneSnapshot listed = await session.NavigateAsync(ParsePath("C:\\"), CancellationToken.None);

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
        DirectoryListing root = Listing("C:\\root", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
        port.Enqueue(DirectoryReadOutcome.Succeeded(root));
        TaskCompletionSource<DirectoryReadOutcome> pending = port.EnqueuePending();
        PaneSession session = CreateSession(port);
        _ = await session.NavigateAsync(ParsePath("C:\\root"), CancellationToken.None);
        FileSystemPath target = ParsePath("C:\\other");

        Task<PaneSnapshot> navigation = session.NavigateAsync(target, CancellationToken.None);
        PaneSnapshot frozen = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);
        pending.SetResult(DirectoryReadOutcome.Succeeded(Listing("C:\\other", ("c.txt", DirectoryEntryKind.File))));
        PaneSnapshot completed = await navigation;

        Assert.AreSame(target, Assert.IsInstanceOfType<PaneLoading>(frozen.Activity).Target);
        Assert.AreSame(root.Entries[0].Path, Assert.IsInstanceOfType<PaneContentListed>(frozen.Content).State.FocusItem);
        Assert.AreEqual("C:\\other", Assert.IsInstanceOfType<PaneContentListed>(completed.Content).Listing.Location.CanonicalText);
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
        PaneSession session = CreateSession(port);
        DirectoryListing stale = Listing("C:\\stale", ("s.txt", DirectoryEntryKind.File));
        DirectoryListing fresh = Listing("C:\\fresh", ("f.txt", DirectoryEntryKind.File));

        Task<PaneSnapshot> firstNavigation = session.NavigateAsync(ParsePath("C:\\stale"), CancellationToken.None);
        Task<PaneSnapshot> secondNavigation = session.NavigateAsync(ParsePath("C:\\fresh"), CancellationToken.None);
        second.SetResult(DirectoryReadOutcome.Succeeded(fresh));
        PaneSnapshot freshSnapshot = await secondNavigation;
        first.SetResult(DirectoryReadOutcome.Succeeded(stale));
        PaneSnapshot staleSnapshot = await firstNavigation;

        Assert.AreSame(fresh, Assert.IsInstanceOfType<PaneContentListed>(freshSnapshot.Content).Listing);
        Assert.AreSame(freshSnapshot, staleSnapshot);
        Assert.AreSame(freshSnapshot, session.Current);
        Assert.AreSame(PaneActivity.Idle, session.Current.Activity);
    }

    /// <summary>Proves the composition boundary rejects an entry boundary outside the fixed range.</summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(DirectoryListing.EntryBoundaryLimit + 1)]
    public void ConstructWhenEntryBoundaryIsOutOfRangeThrowsArgumentOutOfRangeException(int entryBoundary)
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        VisiblePageCapacity capacity = Capacity(4);

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new PaneSession(port, capacity, entryBoundary));
    }

    private static PaneSession CreateSession(IDirectoryReadPort port)
    {
        return new PaneSession(port, Capacity(4), DirectoryListing.EntryBoundaryLimit);
    }

    private static VisiblePageCapacity Capacity(int rows)
    {
        return Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(VisiblePageCapacity.Create(rows)).Capacity;
    }

    private static DirectoryListing Listing(string location, params (string Name, DirectoryEntryKind Kind)[] entries)
    {
        FileSystemPath parsedLocation = ParsePath(location);
        DirectoryEntry[] built = new DirectoryEntry[entries.Length];
        for (int index = 0; index < entries.Length; index++)
        {
            string separator = parsedLocation.CanonicalText.EndsWith('\\') ? string.Empty : "\\";
            built[index] = DirectoryEntry.Create(
                ParsePath(parsedLocation.CanonicalText + separator + entries[index].Name),
                entries[index].Name,
                entries[index].Kind);
        }
        DirectoryListingCreation creation = DirectoryListing.Create(
            parsedLocation,
            built,
            DirectoryListingCompleteness.Complete,
            0);
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(creation).Listing;
    }

    private static FileSystemPath ParsePath(string input)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input)).Path;
    }
}
