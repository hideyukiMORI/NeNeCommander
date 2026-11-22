using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves refresh recovers a removed focus through the previous visible order.</summary>
[TestClass]
public sealed class PaneRefreshFocusTests
{
    /// <summary>Proves surviving identities, forward preference, backward fallback, and empty replacements.</summary>
    [TestMethod]
    [DataRow(2, "a,b,d,e", "d")]
    [DataRow(4, "a,b,c,d", "d")]
    [DataRow(2, "a,b,e", "e")]
    [DataRow(3, "a,b", "b")]
    [DataRow(2, "aa,b,z", "b")]
    [DataRow(2, "a,b,c,e", "c")]
    [DataRow(2, "a,b,C,d,e", "C")]
    [DataRow(2, "x,y", "x")]
    [DataRow(2, "", null)]
    public async Task RefreshAsyncWhenListingChangesRecoversOwnedFocus(int focusIndex, string remaining, string? expected)
    {
        ArgumentNullException.ThrowIfNull(remaining);
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing before = Listing("a,b,c,d,e");
        DirectoryListing after = Listing(remaining);
        port.Enqueue(DirectoryReadOutcome.Succeeded(before, null));
        port.Enqueue(DirectoryReadOutcome.Succeeded(after, null));
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        _ = await session.NavigateAsync(before.Location, CancellationToken.None);
        for (int index = 0; index < focusIndex; index++)
        {
            _ = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);
        }
        _ = await session.HandleAsync(UserIntent.ToggleSelection, CancellationToken.None);
        PaneState previous = StateOf(session.Current);

        PaneSnapshot snapshot = await session.RefreshAsync(CancellationToken.None);

        PaneState current = StateOf(snapshot);
        Assert.AreSame(after.Entries.SingleOrDefault(entry => entry.Name == expected)?.Path, current.FocusItem);
        Assert.IsEmpty(current.Selection);
        Assert.AreSame(previous.NavigationHistory, current.NavigationHistory);
        Assert.AreSame(previous.SortOrder, current.SortOrder);
        Assert.HasCount(2, port.Requests);
    }

    /// <summary>Proves the old descending visible order owns the neighbour decision.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenRefreshRemovesSortedFocusUsesPreviousVisibleOrder()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing before = Listing("a,b,c,d,e");
        DirectoryListing after = Listing("a,b,d,e,z");
        port.Enqueue(DirectoryReadOutcome.Succeeded(before, null));
        port.Enqueue(DirectoryReadOutcome.Succeeded(after, null));
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        _ = await session.NavigateAsync(before.Location, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.SortByName, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.MovePrevious, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.MovePrevious, CancellationToken.None);
        Assert.AreSame(before.Entries[2].Path, StateOf(session.Current).FocusItem);

        PaneSnapshot snapshot = await session.HandleAsync(UserIntent.Refresh, CancellationToken.None);

        Assert.AreSame(after.Entries[1].Path, StateOf(snapshot).FocusItem);
        PaneSessionTestContext.AssertVisibleNames(["z", "e", "d", "b", "a"], StateOf(snapshot));
    }

    /// <summary>Proves a formerly visible neighbour that becomes hidden cannot receive focus.</summary>
    [TestMethod]
    public async Task RefreshAsyncWhenNextSurvivorBecomesHiddenSkipsIt()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing before = Listing("a,b,c,d,e");
        DirectoryListing after = PaneSessionTestContext.HiddenListing("C:\\root",
            ("a", EntryVisibility.Normal), ("b", EntryVisibility.Normal),
            ("d", EntryVisibility.Hidden), ("e", EntryVisibility.Normal));
        port.Enqueue(DirectoryReadOutcome.Succeeded(before, null));
        port.Enqueue(DirectoryReadOutcome.Succeeded(after, null));
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        _ = await session.NavigateAsync(before.Location, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);

        PaneSnapshot snapshot = await session.RefreshAsync(CancellationToken.None);

        Assert.AreSame(after.Entries[3].Path, StateOf(snapshot).FocusItem);
        PaneSessionTestContext.AssertVisibleNames(["a", "b", "e"], StateOf(snapshot));
    }

    /// <summary>Proves explicit creation/rename targets retain their own focus recovery contract.</summary>
    [TestMethod]
    [DataRow("z", "z")]
    [DataRow("absent", "a")]
    public async Task RefreshFocusingAsyncWhenTargetIsExplicitRetainsPreferredTargetPolicy(string target, string expected)
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing before = Listing("a,b,c,d");
        DirectoryListing after = Listing("a,b,d,z");
        port.Enqueue(DirectoryReadOutcome.Succeeded(before, null));
        port.Enqueue(DirectoryReadOutcome.Succeeded(after, null));
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        _ = await session.NavigateAsync(before.Location, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);

        PaneSnapshot snapshot = await session.RefreshFocusingAsync(
            PaneSessionTestContext.ParsePath("C:\\root\\" + target), CancellationToken.None);

        Assert.AreSame(after.Entries.Single(entry => entry.Name == expected).Path, StateOf(snapshot).FocusItem);
    }

    /// <summary>Proves a direct navigation to the same location still starts at its first item.</summary>
    [TestMethod]
    public async Task NavigateAsyncWhenCurrentLocationIsRequestedDoesNotRestoreRemovedFocus()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing before = Listing("a,b,c,d");
        DirectoryListing after = Listing("a,b,d");
        port.Enqueue(DirectoryReadOutcome.Succeeded(before, null));
        port.Enqueue(DirectoryReadOutcome.Succeeded(after, null));
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        _ = await session.NavigateAsync(before.Location, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);

        PaneSnapshot snapshot = await session.NavigateAsync(after.Location, CancellationToken.None);

        Assert.AreSame(after.Entries[0].Path, StateOf(snapshot).FocusItem);
    }

    /// <summary>Proves unsuccessful reads keep the complete prior state until a successful refresh.</summary>
    [TestMethod]
    public async Task RefreshAsyncWhenReadFailsOrCancelsPreservesFocusAndSelection()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing before = Listing("a,b,c,d");
        port.Enqueue(DirectoryReadOutcome.Succeeded(before, null));
        port.Enqueue(DirectoryReadOutcome.Failed(FileOperationFailureKind.AccessDenied));
        port.Enqueue(DirectoryReadOutcome.Cancelled());
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        _ = await session.NavigateAsync(before.Location, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.ToggleSelection, CancellationToken.None);
        PaneState previous = StateOf(session.Current);

        PaneSnapshot failed = await session.RefreshAsync(CancellationToken.None);
        PaneSnapshot cancelled = await session.RefreshAsync(CancellationToken.None);

        Assert.AreSame(previous, StateOf(failed));
        Assert.AreSame(previous, StateOf(cancelled));
        _ = Assert.IsInstanceOfType<PaneReadFailed>(failed.Activity);
        _ = Assert.IsInstanceOfType<PaneReadCancelled>(cancelled.Activity);
    }

    /// <summary>Proves an empty prior listing has no neighbour and adopts the first new visible entry.</summary>
    [TestMethod]
    public async Task RefreshAsyncWhenPreviouslyEmptyFocusesFirstNewItem()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing before = Listing("");
        DirectoryListing after = Listing("a,b");
        port.Enqueue(DirectoryReadOutcome.Succeeded(before, null));
        port.Enqueue(DirectoryReadOutcome.Succeeded(after, null));
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        _ = await session.NavigateAsync(before.Location, CancellationToken.None);

        PaneSnapshot snapshot = await session.RefreshAsync(CancellationToken.None);

        Assert.AreSame(after.Entries[0].Path, StateOf(snapshot).FocusItem);
    }

    /// <summary>Proves a late refresh cannot replace a more recent navigation or restore its old focus.</summary>
    [TestMethod]
    public async Task RefreshAsyncWhenNavigationSupersedesItDiscardsOldFocusRecovery()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing before = Listing("a,b,c,d");
        port.Enqueue(DirectoryReadOutcome.Succeeded(before, null));
        TaskCompletionSource<DirectoryReadOutcome> pending = port.EnqueuePending();
        DirectoryListing other = PaneSessionTestContext.Listing("C:\\other", ("z", DirectoryEntryKind.File));
        port.Enqueue(DirectoryReadOutcome.Succeeded(other, null));
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        _ = await session.NavigateAsync(before.Location, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);
        Task<PaneSnapshot> refresh = session.RefreshAsync(CancellationToken.None);

        PaneSnapshot newer = await session.NavigateAsync(other.Location, CancellationToken.None);
        pending.SetResult(DirectoryReadOutcome.Succeeded(Listing("a,c,d"), null));
        PaneSnapshot late = await refresh;

        Assert.AreSame(newer, late);
        Assert.AreSame(other.Entries[0].Path, StateOf(late).FocusItem);
        Assert.AreSame(other.Location, StateOf(late).Location);
    }

    private static DirectoryListing Listing(string names)
    {
        return PaneSessionTestContext.Listing("C:\\root", [.. names.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(name => (name, DirectoryEntryKind.File))]);
    }

    private static PaneState StateOf(PaneSnapshot snapshot)
    {
        return Assert.IsInstanceOfType<PaneContentListed>(snapshot.Content).State;
    }
}
