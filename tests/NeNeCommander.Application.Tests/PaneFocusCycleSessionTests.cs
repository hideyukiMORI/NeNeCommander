using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves cyclic focus follows fresh listings and the active pane's existing session.</summary>
[TestClass]
public sealed class PaneFocusCycleSessionTests
{
    /// <summary>Proves the next movement after refresh uses the new visible endpoints.</summary>
    [TestMethod]
    public async Task HandleAsyncAfterRefreshRemovesEndpointCyclesCurrentListingAsync()
    {
        DirectoryListing before = PaneSessionTestContext.Listing("C:\\cycle",
            ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File), ("c.txt", DirectoryEntryKind.File));
        DirectoryListing after = PaneSessionTestContext.Listing("C:\\cycle",
            ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(before, null));
        port.Enqueue(DirectoryReadOutcome.Succeeded(after, null));
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        _ = await session.NavigateAsync(before.Location, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.FocusLast, CancellationToken.None);
        PaneSnapshot refreshed = await session.RefreshAsync(CancellationToken.None);

        PaneSnapshot next = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);
        PaneSnapshot previous = await session.HandleAsync(UserIntent.MovePrevious, CancellationToken.None);

        Assert.AreSame(after.Entries[1].Path, DualPaneSessionTestContext.Focus(refreshed));
        Assert.AreSame(after.Entries[0].Path, DualPaneSessionTestContext.Focus(next));
        Assert.AreSame(after.Entries[1].Path, DualPaneSessionTestContext.Focus(previous));
        Assert.AreSame(after, Assert.IsInstanceOfType<PaneContentListed>(previous.Content).Listing);
        Assert.AreSame(previous, session.Current);
        Assert.HasCount(2, port.Requests);
        Assert.HasCount(3, before.Entries);
    }

    /// <summary>Proves cycling on either side preserves the passive pane and selected identities.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenActivePaneChangesCyclesEachPaneIndependentlyAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing left = DualPaneSessionTestContext.Listing("C:\\left",
            ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File), ("c.txt", DirectoryEntryKind.File));
        DirectoryListing right = DualPaneSessionTestContext.Listing("C:\\right",
            ("x.txt", DirectoryEntryKind.File), ("y.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(left, right);
        RecordingDualPaneObserver observer = RecordingDualPaneObserver.Create();
        DualPaneSnapshot selected = await fixture.Panes.HandleAsync(UserIntent.ToggleSelection, observer, CancellationToken.None);

        DualPaneSnapshot leftPrevious = await fixture.Panes.HandleAsync(UserIntent.MovePrevious, observer, CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.ActivateOtherPane, observer, CancellationToken.None);
        DualPaneSnapshot rightPrevious = await fixture.Panes.HandleAsync(UserIntent.MovePrevious, observer, CancellationToken.None);
        DualPaneSnapshot rightNext = await fixture.Panes.HandleAsync(UserIntent.MoveNext, observer, CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.ActivateOtherPane, observer, CancellationToken.None);
        DualPaneSnapshot leftNext = await fixture.Panes.HandleAsync(UserIntent.MoveNext, observer, CancellationToken.None);

        Assert.AreSame(left.Entries[2].Path, DualPaneSessionTestContext.Focus(leftPrevious.Left));
        Assert.AreSame(selected.Right, leftPrevious.Right);
        Assert.AreSame(leftPrevious.Left, rightPrevious.Left);
        Assert.AreSame(right.Entries[1].Path, DualPaneSessionTestContext.Focus(rightPrevious.Right));
        Assert.AreSame(right.Entries[0].Path, DualPaneSessionTestContext.Focus(rightNext.Right));
        Assert.AreSame(rightNext.Right, leftNext.Right);
        Assert.AreSame(left.Entries[0].Path, DualPaneSessionTestContext.Focus(leftNext.Left));
        Assert.AreSame(PaneSide.Left, leftNext.ActiveSide);
        PaneState selectedState = Assert.IsInstanceOfType<PaneContentListed>(selected.Left.Content).State;
        PaneState finalState = Assert.IsInstanceOfType<PaneContentListed>(leftNext.Left.Content).State;
        Assert.AreSame(selectedState.Selection, finalState.Selection);
        Assert.HasCount(1, fixture.Left.Requests);
        Assert.HasCount(1, fixture.Right.Requests);
    }
}
