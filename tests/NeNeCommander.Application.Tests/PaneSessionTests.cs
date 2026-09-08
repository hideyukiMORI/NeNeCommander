using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Launching;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;
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

    /// <summary>Proves opening a focused Windows file performs one handoff without another read.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenOpenFocusedOnWindowsFileLaunchesOnceWithoutRead()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(
            Listing("C:\\root", ("docs", DirectoryEntryKind.Directory), ("a.txt", DirectoryEntryKind.File))));
        ScriptedFileLauncher launcher = new();
        launcher.Enqueue(FileLaunchOutcome.Accepted());
        PaneSession session = CreateSession(port, launcher);
        using CancellationTokenSource cancellation = new();
        _ = await session.NavigateAsync(ParsePath("C:\\root"), CancellationToken.None);
        PaneSnapshot onFile = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);

        PaneSnapshot afterOpen = await session.HandleAsync(UserIntent.OpenFocused, cancellation.Token);

        Assert.HasCount(1, launcher.Targets);
        Assert.AreSame(
            Assert.IsInstanceOfType<WindowsLocalPath>(
                Assert.IsInstanceOfType<PaneContentListed>(onFile.Content).State.FocusItem),
            launcher.Targets[0]);
        Assert.AreEqual(cancellation.Token, launcher.CancellationTokens[0]);
        Assert.AreSame(onFile.Content, afterOpen.Content);
        Assert.AreSame(PaneActivity.Idle, afterOpen.Activity);
        Assert.HasCount(1, port.Requests);
    }

    /// <summary>Proves launch activity freezes repeat dispatch until the handoff outcome arrives.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenFileLaunchIsPendingFreezesRepeatedIntent()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(
            Listing("C:\\root", ("a.txt", DirectoryEntryKind.File))));
        ScriptedFileLauncher launcher = new();
        TaskCompletionSource<FileLaunchOutcome> release = launcher.EnqueuePending();
        PaneSession session = CreateSession(port, launcher);
        PaneSnapshot listed = await session.NavigateAsync(ParsePath("C:\\root"), CancellationToken.None);

        Task<PaneSnapshot> opening = session.HandleAsync(UserIntent.OpenFocused, CancellationToken.None);
        PaneSnapshot repeated = await session.HandleAsync(UserIntent.OpenFocused, CancellationToken.None);

        PaneLaunching activity = Assert.IsInstanceOfType<PaneLaunching>(repeated.Activity);
        Assert.AreSame(launcher.Targets[0], activity.Target);
        Assert.AreSame(listed.Content, repeated.Content);
        Assert.HasCount(1, launcher.Targets);
        release.SetResult(FileLaunchOutcome.Accepted());
        PaneSnapshot completed = await opening;
        Assert.AreSame(PaneActivity.Idle, completed.Activity);
        Assert.AreSame(listed.Content, completed.Content);
    }

    /// <summary>Proves every direct pane-read entry point is frozen while a file handoff is pending.</summary>
    [TestMethod]
    public async Task PaneReadEntryPointsWhenFileLaunchIsPendingDoNotStartReadOrChangeState()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = Listing("C:\\root", ("a.txt", DirectoryEntryKind.File));
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing));
        ScriptedFileLauncher launcher = new();
        TaskCompletionSource<FileLaunchOutcome> release = launcher.EnqueuePending();
        PaneSession session = CreateSession(port, launcher);
        PaneSnapshot listed = await session.NavigateAsync(listing.Location, CancellationToken.None);
        Task<PaneSnapshot> opening = session.HandleAsync(UserIntent.OpenFocused, CancellationToken.None);
        PaneSnapshot launching = session.Current;

        PaneSnapshot navigated = await session.NavigateAsync(ParsePath("C:\\other"), CancellationToken.None);
        PaneSnapshot refreshed = await session.RefreshAsync(CancellationToken.None);
        PaneSnapshot focusRefreshed = await session.RefreshFocusingAsync(
            listing.Entries[0].Path,
            CancellationToken.None);

        Assert.AreSame(launching, navigated);
        Assert.AreSame(launching, refreshed);
        Assert.AreSame(launching, focusRefreshed);
        Assert.AreSame(launching, session.Current);
        Assert.HasCount(1, port.Requests);
        release.SetResult(FileLaunchOutcome.Accepted());
        PaneSnapshot completed = await opening;
        Assert.AreSame(listed.Content, completed.Content);
        Assert.AreSame(PaneActivity.Idle, completed.Activity);
        Assert.HasCount(1, port.Requests);
    }

    /// <summary>Proves normalized launch failure and cancellation preserve all listed pane state.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenFileLaunchDoesNotHandoffReportsTypedActivity()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(
            Listing("C:\\root", ("a.txt", DirectoryEntryKind.File))));
        ScriptedFileLauncher launcher = new();
        launcher.Enqueue(FileLaunchOutcome.Failed(FileLaunchFailureKind.AccessDenied));
        launcher.Enqueue(FileLaunchOutcome.Cancelled());
        PaneSession session = CreateSession(port, launcher);
        PaneSnapshot listed = await session.NavigateAsync(ParsePath("C:\\root"), CancellationToken.None);

        PaneSnapshot failed = await session.HandleAsync(UserIntent.OpenFocused, CancellationToken.None);
        PaneSnapshot cancelled = await session.HandleAsync(UserIntent.OpenFocused, CancellationToken.None);

        PaneLaunchFailed failure = Assert.IsInstanceOfType<PaneLaunchFailed>(failed.Activity);
        Assert.AreSame(FileLaunchFailureKind.AccessDenied, failure.Failure);
        Assert.AreSame(listed.Content, failed.Content);
        Assert.AreSame(failure.Target, Assert.IsInstanceOfType<PaneLaunchCancelled>(cancelled.Activity).Target);
        Assert.AreSame(listed.Content, cancelled.Content);
        Assert.HasCount(2, launcher.Targets);
    }

    /// <summary>Proves unsupported provider files fail before the Windows-local launch port.</summary>
    [TestMethod]
    [TestProperty("ThreatId", "ADV-019")]
    [TestCategory("Adversarial")]
    public async Task HandleAsyncWhenFocusedFileProviderIsUnsupportedDoesNotCallLauncher()
    {
        string[] locations = ["\\\\wsl.localhost\\Ubuntu\\home", "\\\\server\\share\\folder"];
        foreach (string location in locations)
        {
            ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
            port.Enqueue(DirectoryReadOutcome.Succeeded(
                Listing(location, ("a.txt", DirectoryEntryKind.File))));
            ScriptedFileLauncher launcher = new();
            PaneSession session = CreateSession(port, launcher);
            PaneSnapshot listed = await session.NavigateAsync(ParsePath(location), CancellationToken.None);

            PaneSnapshot snapshot = await session.HandleAsync(UserIntent.OpenFocused, CancellationToken.None);

            PaneLaunchFailed failed = Assert.IsInstanceOfType<PaneLaunchFailed>(snapshot.Activity);
            Assert.AreSame(FileLaunchFailureKind.ProviderUnavailable, failed.Failure);
            Assert.AreSame(listed.Content, snapshot.Content);
            Assert.IsEmpty(launcher.Targets);
        }
    }

    /// <summary>Proves an unregistered launch outcome variant is a defect instead of implicit success.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenFileLaunchOutcomeVariantIsUnsupportedThrows()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(
            Listing("C:\\root", ("a.txt", DirectoryEntryKind.File))));
        ScriptedFileLauncher launcher = new();
        launcher.Enqueue(new UnsupportedFileLaunchOutcome());
        PaneSession session = CreateSession(port, launcher);
        _ = await session.NavigateAsync(ParsePath("C:\\root"), CancellationToken.None);

        InvalidOperationException failure = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            async () => await session.HandleAsync(UserIntent.OpenFocused, CancellationToken.None));

        Assert.AreEqual("The file launch outcome variant is not supported.", failure.Message);
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
        ScriptedFileLauncher launcher = new();
        PaneSession session = CreateSession(port, launcher);
        _ = await session.NavigateAsync(ParsePath("C:\\root"), CancellationToken.None);

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
        port.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\empty")));
        ScriptedFileLauncher launcher = new();
        PaneSession session = CreateSession(port, launcher);

        PaneSnapshot emptyListed = await session.NavigateAsync(ParsePath("C:\\empty"), CancellationToken.None);
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

    /// <summary>Proves both refresh forms do nothing while a read is in flight, so a refresh never races the navigation it would duplicate.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-016")]
    public async Task RefreshAsyncWhenReadIsInFlightDoesNothing()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing root = Listing("C:\\root", ("a.txt", DirectoryEntryKind.File));
        port.Enqueue(DirectoryReadOutcome.Succeeded(root));
        TaskCompletionSource<DirectoryReadOutcome> pending = port.EnqueuePending();
        PaneSession session = CreateSession(port);
        _ = await session.NavigateAsync(ParsePath("C:\\root"), CancellationToken.None);

        Task<PaneSnapshot> navigation = session.NavigateAsync(ParsePath("C:\\other"), CancellationToken.None);
        PaneSnapshot plain = await session.RefreshAsync(CancellationToken.None);
        PaneSnapshot focusing = await session.RefreshFocusingAsync(root.Entries[0].Path, CancellationToken.None);
        pending.SetResult(DirectoryReadOutcome.Succeeded(Listing("C:\\other", ("c.txt", DirectoryEntryKind.File))));
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
        PaneNavigationHistory history = HistoryOf(session.Current);
        Assert.HasCount(1, history.Locations);
        Assert.AreSame(fresh.Location, history.Locations[0]);
    }

    /// <summary>Proves the composed visibility decides the first listing's visible set.</summary>
    [TestMethod]
    public async Task NavigateAsyncWhenComposedVisibilityOmitsHiddenEntriesListsOnlyTheVisibleOnes()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = HiddenListing(
            "C:\\root",
            ("a.txt", EntryVisibility.Normal),
            ("b.txt", EntryVisibility.Hidden));
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing));
        PaneSession session = CreateSession(port, HiddenItemVisibility.Hidden);

        PaneSnapshot snapshot = await session.NavigateAsync(ParsePath("C:\\root"), CancellationToken.None);

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
        DirectoryListing first = HiddenListing("C:\\root", ("a.txt", EntryVisibility.Hidden));
        DirectoryListing second = HiddenListing("C:\\other", ("b.txt", EntryVisibility.Hidden));
        port.Enqueue(DirectoryReadOutcome.Succeeded(first));
        port.Enqueue(DirectoryReadOutcome.Succeeded(second));
        PaneSession session = CreateSession(port, HiddenItemVisibility.Shown);

        PaneSnapshot initial = await session.NavigateAsync(ParsePath("C:\\root"), CancellationToken.None);
        PaneSnapshot later = await session.NavigateAsync(ParsePath("C:\\other"), CancellationToken.None);

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
        DirectoryListing root = Listing("C:\\root", ("z.txt", DirectoryEntryKind.File));
        DirectoryListing other = Listing("C:\\other", ("b.txt", DirectoryEntryKind.File));
        port.Enqueue(DirectoryReadOutcome.Succeeded(root));
        port.Enqueue(DirectoryReadOutcome.Succeeded(other));
        port.Enqueue(DirectoryReadOutcome.Succeeded(root));
        port.Enqueue(DirectoryReadOutcome.Succeeded(other));
        PaneSession session = CreateSession(port);
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
        PaneNavigationHistory history = HistoryOf(forwarded);
        Assert.AreEqual(1, history.CurrentIndex);
        Assert.HasCount(2, history.Locations);
    }

    /// <summary>Proves unavailable directions return the current snapshot without starting a read.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenHistoryHasNoCandidateDoesNotRead()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\root")));
        PaneSession session = CreateSession(port);
        PaneSnapshot listed = await session.NavigateAsync(ParsePath("C:\\root"), CancellationToken.None);

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
        DirectoryListing root = Listing("C:\\root");
        DirectoryListing other = Listing("C:\\other");
        port.Enqueue(DirectoryReadOutcome.Succeeded(root));
        port.Enqueue(DirectoryReadOutcome.Succeeded(other));
        port.Enqueue(DirectoryReadOutcome.Failed(FileOperationFailureKind.AccessDenied));
        port.Enqueue(DirectoryReadOutcome.Cancelled());
        port.Enqueue(DirectoryReadOutcome.Succeeded(root));
        PaneSession session = CreateSession(port);
        _ = await session.NavigateAsync(root.Location, CancellationToken.None);
        _ = await session.NavigateAsync(other.Location, CancellationToken.None);

        PaneSnapshot failed = await session.HandleAsync(UserIntent.NavigateBack, CancellationToken.None);
        PaneSnapshot cancelled = await session.HandleAsync(UserIntent.NavigateBack, CancellationToken.None);
        PaneSnapshot succeeded = await session.HandleAsync(UserIntent.NavigateBack, CancellationToken.None);

        Assert.AreEqual(1, HistoryOf(failed).CurrentIndex);
        Assert.AreEqual(1, HistoryOf(cancelled).CurrentIndex);
        Assert.AreEqual(0, HistoryOf(succeeded).CurrentIndex);
        Assert.AreSame(root.Location, port.Requests[2].Location);
        Assert.AreSame(root.Location, port.Requests[3].Location);
        Assert.AreSame(root.Location, port.Requests[4].Location);
    }

    /// <summary>Proves same-location and refresh reads retain a Forward candidate after Back.</summary>
    [TestMethod]
    public async Task NavigateAndRefreshWhenLocationIdentityIsUnchangedPreserveForwardHistory()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing root = Listing("C:\\root");
        DirectoryListing other = Listing("C:\\other");
        DirectoryListing sameIdentity = Listing("c:\\ROOT");
        port.Enqueue(DirectoryReadOutcome.Succeeded(root));
        port.Enqueue(DirectoryReadOutcome.Succeeded(other));
        port.Enqueue(DirectoryReadOutcome.Succeeded(root));
        port.Enqueue(DirectoryReadOutcome.Succeeded(sameIdentity));
        port.Enqueue(DirectoryReadOutcome.Succeeded(sameIdentity));
        port.Enqueue(DirectoryReadOutcome.Succeeded(other));
        PaneSession session = CreateSession(port);
        _ = await session.NavigateAsync(root.Location, CancellationToken.None);
        _ = await session.NavigateAsync(other.Location, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.NavigateBack, CancellationToken.None);

        _ = await session.NavigateAsync(sameIdentity.Location, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.Refresh, CancellationToken.None);
        PaneSnapshot forwarded = await session.HandleAsync(UserIntent.NavigateForward, CancellationToken.None);

        PaneNavigationHistory history = HistoryOf(forwarded);
        Assert.HasCount(2, history.Locations);
        Assert.AreEqual(1, history.CurrentIndex);
        Assert.AreSame(other.Location, Assert.IsInstanceOfType<PaneContentListed>(forwarded.Content).State.Location);
    }

    /// <summary>Proves the composition boundary rejects an entry boundary outside the fixed range.</summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(DirectoryListing.EntryBoundaryLimit + 1)]
    public void ConstructWhenEntryBoundaryIsOutOfRangeThrowsArgumentOutOfRangeException(int entryBoundary)
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        VisiblePageCapacity capacity = Capacity(4);

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new PaneSession(
            port,
            new ScriptedFileLauncher(),
            capacity,
            entryBoundary,
            HiddenItemVisibility.Hidden));
    }

    private static DirectoryListing HiddenListing(string location, params (string Name, EntryVisibility Visibility)[] entries)
    {
        FileSystemPath parsedLocation = ParsePath(location);
        DirectoryEntry[] built = new DirectoryEntry[entries.Length];
        for (int index = 0; index < entries.Length; index++)
        {
            built[index] = DirectoryEntry.Create(
                ParsePath(parsedLocation.CanonicalText + "\\" + entries[index].Name),
                entries[index].Name,
                DirectoryEntryKind.File,
                entries[index].Visibility);
        }
        DirectoryListingCreation creation = DirectoryListing.Create(
            parsedLocation,
            built,
            DirectoryListingCompleteness.Complete,
            0);
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(creation).Listing;
    }

    private static PaneSession CreateSession(IDirectoryReadPort port)
    {
        return CreateSession(port, HiddenItemVisibility.Hidden);
    }

    private static PaneSession CreateSession(IDirectoryReadPort port, HiddenItemVisibility visibility)
    {
        return CreateSession(port, new ScriptedFileLauncher(), visibility);
    }

    private static PaneSession CreateSession(IDirectoryReadPort port, IFileLauncher fileLauncher)
    {
        return CreateSession(port, fileLauncher, HiddenItemVisibility.Hidden);
    }

    private static PaneSession CreateSession(
        IDirectoryReadPort port,
        IFileLauncher fileLauncher,
        HiddenItemVisibility visibility)
    {
        return new PaneSession(
            port,
            fileLauncher,
            Capacity(4),
            DirectoryListing.EntryBoundaryLimit,
            visibility);
    }

    private static PaneNavigationHistory HistoryOf(PaneSnapshot snapshot)
    {
        return Assert.IsInstanceOfType<PaneContentListed>(snapshot.Content).State.NavigationHistory;
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
                entries[index].Kind,
                EntryVisibility.Normal);
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
