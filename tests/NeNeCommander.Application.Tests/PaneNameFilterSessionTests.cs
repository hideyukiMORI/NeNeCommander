using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves filtering stays local to the exact pane snapshot across listing lifetimes.</summary>
[TestClass]
public sealed class PaneNameFilterSessionTests
{
    /// <summary>Proves queries perform no read and a stale owner cannot replace a later query.</summary>
    [TestMethod]
    public async Task ApplyNameFilterWhenSnapshotIsStaleDoesNotChangeLaterQueryAsync()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = Listing(@"C:\filter", "a.txt", "b.log", "c.txt");
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        PaneSnapshot original = await session.NavigateAsync(listing.Location, CancellationToken.None);

        PaneSnapshot filtered = session.ApplyNameFilter(original, PaneNameFilterQuery.Parse(".txt"));
        PaneSnapshot stale = session.ApplyNameFilter(original, PaneNameFilterQuery.Parse(".log"));
        PaneSnapshot repeated = session.ApplyNameFilter(filtered, PaneNameFilterQuery.Parse(".txt"));

        Assert.AreSame(filtered, stale);
        Assert.AreSame(filtered, repeated);
        PaneSessionTestContext.AssertVisibleNames(["a.txt", "c.txt"], StateOf(session.Current));
        Assert.HasCount(1, port.Requests);
    }

    /// <summary>Proves refresh retains query and resolves a removed focus in the old visible order.</summary>
    [TestMethod]
    public async Task RefreshAsyncWhenFilteredFocusIsRemovedSkipsNewNonmatchingNeighbour()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing before = Listing(@"C:\filter", "a.txt", "b.txt", "c.log", "d.txt");
        DirectoryListing after = Listing(@"C:\filter", "a.txt", "c.log", "d.txt", "z.txt");
        port.Enqueue(DirectoryReadOutcome.Succeeded(before, null));
        port.Enqueue(DirectoryReadOutcome.Succeeded(after, null));
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        _ = await session.NavigateAsync(before.Location, CancellationToken.None);
        _ = session.ApplyNameFilter(session.Current, PaneNameFilterQuery.Parse(".txt"));
        _ = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.ToggleSelection, CancellationToken.None);

        PaneState refreshed = StateOf(await session.RefreshAsync(CancellationToken.None));

        Assert.AreEqual(".txt", refreshed.NameFilter.Text);
        Assert.AreSame(after.Entries[2].Path, refreshed.FocusItem);
        Assert.IsEmpty(refreshed.Selection);
        Assert.IsNull(refreshed.SelectionAnchor);
        PaneSessionTestContext.AssertVisibleNames(["a.txt", "d.txt", "z.txt"], refreshed);
        Assert.HasCount(2, port.Requests);
    }

    /// <summary>Proves same identity retains a query but a true location change clears it.</summary>
    [TestMethod]
    [DataRow(@"C:\filter", @"c:\FILTER", ".txt")]
    [DataRow(@"C:\filter", @"C:\other", "")]
    [DataRow(@"\\wsl.localhost\Ubuntu\home\filter", @"\\wsl.localhost\Ubuntu\home\filter", ".txt")]
    [DataRow(@"\\wsl.localhost\Ubuntu\home\filter", @"\\wsl.localhost\Ubuntu\home\Filter", "")]
    public async Task NavigateAsyncWhenLocationIdentityChangesUsesProviderIdentity(string origin, string destination, string expectedText)
    {
        ArgumentNullException.ThrowIfNull(expectedText);
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing before = Listing(origin, "a.txt", "b.log");
        DirectoryListing after = Listing(destination, "a.txt", "b.log");
        port.Enqueue(DirectoryReadOutcome.Succeeded(before, null));
        port.Enqueue(DirectoryReadOutcome.Succeeded(after, null));
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        _ = await session.NavigateAsync(before.Location, CancellationToken.None);
        _ = session.ApplyNameFilter(session.Current, PaneNameFilterQuery.Parse(".txt"));

        PaneState navigated = StateOf(await session.NavigateAsync(after.Location, CancellationToken.None));

        Assert.AreEqual(expectedText, navigated.NameFilter.Text);
        Assert.AreSame(after.Entries[0].Path, navigated.FocusItem);
        Assert.AreEqual(expectedText.Length == 0 ? 2 : 1, navigated.VisibleEntries.Count);
        Assert.IsEmpty(navigated.Selection);
    }

    /// <summary>Proves creating a nonmatching preferred name never bypasses the applied projection.</summary>
    [TestMethod]
    public async Task RefreshFocusingAsyncWhenCreatedNameDoesNotMatchKeepsFilterAndVisibleFocus()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing before = Listing(@"C:\filter", "a.txt");
        DirectoryListing after = Listing(@"C:\filter", "a.txt", "new.log");
        port.Enqueue(DirectoryReadOutcome.Succeeded(before, null));
        port.Enqueue(DirectoryReadOutcome.Succeeded(after, null));
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        _ = await session.NavigateAsync(before.Location, CancellationToken.None);
        _ = session.ApplyNameFilter(session.Current, PaneNameFilterQuery.Parse(".txt"));

        PaneState refreshed = StateOf(await session.RefreshFocusingAsync(after.Entries[1].Path, CancellationToken.None));

        PaneSessionTestContext.AssertVisibleNames(["a.txt"], refreshed);
        Assert.AreSame(after.Entries[0].Path, refreshed.FocusItem);
        Assert.AreEqual(".txt", refreshed.NameFilter.Text);
    }

    /// <summary>Proves reads own input, and cancellation retains the original query and listing.</summary>
    [TestMethod]
    public async Task ApplyNameFilterWhenReadOwnsPaneRejectsUpdateAndRetainsQueryAfterCancelAsync()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing before = Listing(@"C:\filter", "a.txt", "b.log");
        port.Enqueue(DirectoryReadOutcome.Succeeded(before, null));
        TaskCompletionSource<DirectoryReadOutcome> release = port.EnqueuePendingInline();
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        _ = await session.NavigateAsync(before.Location, CancellationToken.None);
        _ = session.ApplyNameFilter(session.Current, PaneNameFilterQuery.Parse(".txt"));
        Task<PaneSnapshot> reading = session.NavigateAsync(PaneSessionTestContext.ParsePath(@"C:\other"), CancellationToken.None);
        PaneSnapshot pending = session.Current;

        PaneSnapshot rejected = session.ApplyNameFilter(pending, PaneNameFilterQuery.Empty);
        release.SetResult(DirectoryReadOutcome.Cancelled());
        PaneSnapshot cancelled = await reading;

        Assert.AreSame(pending, rejected);
        _ = Assert.IsInstanceOfType<PaneReadCancelled>(cancelled.Activity);
        Assert.AreEqual(".txt", StateOf(cancelled).NameFilter.Text);
        PaneSessionTestContext.AssertVisibleNames(["a.txt"], StateOf(cancelled));
    }

    /// <summary>Proves each side owns its query and passive edits require existing activation first.</summary>
    [TestMethod]
    public async Task ApplyNameFilterWhenSideOrOperationDoesNotOwnInputDoesNotChangePaneAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        await fixture.ListBothAsync(Listing(@"C:\left", "a.txt", "b.log"), Listing(@"C:\right", "x.txt", "y.log"));
        DualPaneSnapshot before = fixture.Panes.Current;
        DualPaneSnapshot left = fixture.Panes.ApplyNameFilter(PaneSide.Left, before.Left, PaneNameFilterQuery.Parse(".txt"));
        DualPaneSnapshot passive = fixture.Panes.ApplyNameFilter(PaneSide.Right, left.Right, PaneNameFilterQuery.Parse(".log"));
        _ = await fixture.Panes.HandleAsync(UserIntent.ActivateOtherPane, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot right = fixture.Panes.ApplyNameFilter(PaneSide.Right, passive.Right, PaneNameFilterQuery.Parse(".log"));
        _ = await fixture.Panes.HandleAsync(UserIntent.CreateDirectory, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot blocked = fixture.Panes.ApplyNameFilter(PaneSide.Right, right.Right, PaneNameFilterQuery.Empty);

        Assert.AreSame(before.Right, passive.Right);
        Assert.AreEqual(".txt", StateOf(right.Left).NameFilter.Text);
        Assert.AreEqual(".log", StateOf(right.Right).NameFilter.Text);
        Assert.AreSame(right.Right, blocked.Right);
        Assert.HasCount(1, fixture.Left.Requests);
        Assert.HasCount(1, fixture.Right.Requests);
    }

    /// <summary>Supplies terminal provider results which retain a previously loaded listing.</summary>
    public static IEnumerable<object[]> RetainedOutcomes()
    {
        yield return new object[] { DirectoryReadOutcome.Cancelled() };
        yield return new object[] { DirectoryReadOutcome.Failed(FileOperationFailureKind.AccessDenied) };
    }

    /// <summary>Proves local filtering and both clearing entries cannot erase a failed/cancelled read qualifier.</summary>
    [TestMethod]
    [DynamicData(nameof(RetainedOutcomes))]
    public async Task ApplyNameFilterWhenReadRetainsOldListingPreservesExactTerminalActivity(DirectoryReadOutcome outcome)
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = Listing(@"C:\filter", "a.txt", "b.log");
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        port.Enqueue(outcome);
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        _ = await session.NavigateAsync(listing.Location, CancellationToken.None);
        PaneSnapshot retained = await session.RefreshAsync(CancellationToken.None);

        await AssertFilterRetainsActivityAsync(session, retained);

        Assert.HasCount(2, port.Requests);
        Assert.AreSame(listing, Assert.IsInstanceOfType<PaneContentListed>(session.Current.Content).Listing);
    }

    /// <summary>Proves abandoning a read retains its qualifier while local queries change.</summary>
    [TestMethod]
    public async Task ApplyNameFilterWhenReadIsAbandonedPreservesExactTerminalActivity()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = Listing(@"C:\filter", "a.txt", "b.log");
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        TaskCompletionSource<DirectoryReadOutcome> release = port.EnqueuePendingInline();
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        _ = await session.NavigateAsync(listing.Location, CancellationToken.None);
        Task<PaneSnapshot> work = session.RefreshAsync(CancellationToken.None);
        PaneSnapshot retained = await session.HandleAsync(UserIntent.Escape, CancellationToken.None);
        _ = await work;
        _ = Assert.IsInstanceOfType<PaneReadAbandoned>(retained.Activity);
        release.SetResult(DirectoryReadOutcome.Cancelled());

        await AssertFilterRetainsActivityAsync(session, retained);

        Assert.HasCount(2, port.Requests);
        Assert.AreSame(listing, Assert.IsInstanceOfType<PaneContentListed>(session.Current.Content).Listing);
    }

    private static async Task AssertFilterRetainsActivityAsync(PaneSession session, PaneSnapshot retained)
    {
        foreach (string text in new[] { ".txt", "", ".log" })
        {
            PaneSnapshot changed = session.ApplyNameFilter(session.Current, PaneNameFilterQuery.Parse(text));
            Assert.AreSame(retained.Activity, changed.Activity);
            Assert.AreEqual(text, StateOf(changed).NameFilter.Text);
        }
        PaneSnapshot escaped = await session.HandleAsync(UserIntent.Escape, CancellationToken.None);
        Assert.AreSame(retained.Activity, escaped.Activity);
        Assert.IsTrue(StateOf(escaped).NameFilter.IsEmpty);
        Assert.HasCount(2, StateOf(escaped).VisibleEntries);
    }

    private static PaneState StateOf(PaneSnapshot snapshot)
    {
        return Assert.IsInstanceOfType<PaneContentListed>(snapshot.Content).State;
    }

    private static DirectoryListing Listing(string location, params string[] names)
    {
        return PaneSessionTestContext.Listing(location, [.. names.Select(name => (name, DirectoryEntryKind.File))]);
    }
}
