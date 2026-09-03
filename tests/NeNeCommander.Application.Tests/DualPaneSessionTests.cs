using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves the dual-pane coordinator routes intents to the sole active pane.</summary>
[TestClass]
public sealed class DualPaneSessionTests
{
    /// <summary>Proves the left pane is active before any intent and both panes start empty.</summary>
    [TestMethod]
    public void CurrentWhenNothingHappenedReportsLeftActiveAndBothInitial()
    {
        DualPaneSession panes = CreatePanes(out ScriptedDirectoryReadPort _, out ScriptedDirectoryReadPort _);

        DualPaneSnapshot snapshot = panes.Current;

        Assert.AreSame(PaneSide.Left, snapshot.ActiveSide);
        Assert.AreSame(PaneSnapshot.Initial, snapshot.Left);
        Assert.AreSame(PaneSnapshot.Initial, snapshot.Right);
        Assert.AreSame(snapshot.Left, snapshot.Of(PaneSide.Left));
        Assert.AreSame(snapshot.Right, snapshot.Of(PaneSide.Right));
    }

    /// <summary>Proves each side reads its own location regardless of the active side.</summary>
    [TestMethod]
    public async Task NavigateAsyncWhenSideIsGivenReadsIntoThatSideOnly()
    {
        DualPaneSession panes = CreatePanes(out ScriptedDirectoryReadPort left, out ScriptedDirectoryReadPort right);
        DirectoryListing rightListing = Listing("C:\\Users", ("xi", DirectoryEntryKind.Directory));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing));

        DualPaneSnapshot snapshot = await panes.NavigateAsync(PaneSide.Right, ParsePath("C:\\Users"), CancellationToken.None);

        Assert.IsEmpty(left.Requests);
        Assert.HasCount(1, right.Requests);
        Assert.AreSame(rightListing, Assert.IsInstanceOfType<PaneContentListed>(snapshot.Right.Content).Listing);
        Assert.AreSame(PaneContent.Absent, snapshot.Left.Content);
        Assert.AreSame(PaneSide.Left, snapshot.ActiveSide);
    }

    /// <summary>Proves activation toggles the side without touching either pane's focus.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenActivateOtherPaneTogglesActiveSideWithoutMovingFocus()
    {
        DualPaneSession panes = CreatePanes(out ScriptedDirectoryReadPort left, out ScriptedDirectoryReadPort right);
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File))));
        right.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\right", ("c.txt", DirectoryEntryKind.File), ("d.txt", DirectoryEntryKind.File))));
        _ = await panes.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        DualPaneSnapshot before = await panes.NavigateAsync(PaneSide.Right, ParsePath("C:\\right"), CancellationToken.None);

        DualPaneSnapshot toRight = await panes.HandleAsync(UserIntent.ActivateOtherPane, CancellationToken.None);
        DualPaneSnapshot backToLeft = await panes.HandleAsync(UserIntent.ActivateOtherPane, CancellationToken.None);

        Assert.AreSame(PaneSide.Right, toRight.ActiveSide);
        Assert.AreSame(PaneSide.Left, backToLeft.ActiveSide);
        Assert.AreSame(before.Left, toRight.Left);
        Assert.AreSame(before.Right, toRight.Right);
        Assert.HasCount(1, left.Requests);
        Assert.HasCount(1, right.Requests);
    }

    /// <summary>Proves movement reaches only the active pane.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenMovementArrivesOnlyActivePaneChanges()
    {
        DualPaneSession panes = CreatePanes(out ScriptedDirectoryReadPort left, out ScriptedDirectoryReadPort right);
        DirectoryListing leftListing = Listing("C:\\left", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
        DirectoryListing rightListing = Listing("C:\\right", ("c.txt", DirectoryEntryKind.File), ("d.txt", DirectoryEntryKind.File));
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing));
        _ = await panes.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        _ = await panes.NavigateAsync(PaneSide.Right, ParsePath("C:\\right"), CancellationToken.None);

        DualPaneSnapshot leftMoved = await panes.HandleAsync(UserIntent.MoveNext, CancellationToken.None);
        _ = await panes.HandleAsync(UserIntent.ActivateOtherPane, CancellationToken.None);
        DualPaneSnapshot rightMoved = await panes.HandleAsync(UserIntent.MoveNext, CancellationToken.None);

        Assert.AreSame(leftListing.Entries[1].Path, Focus(leftMoved.Left));
        Assert.AreSame(rightListing.Entries[0].Path, Focus(leftMoved.Right));
        Assert.AreSame(leftListing.Entries[1].Path, Focus(rightMoved.Left));
        Assert.AreSame(rightListing.Entries[1].Path, Focus(rightMoved.Right));
    }

    /// <summary>Proves a read in flight lands in the pane that started it even after activation changes.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-016")]
    public async Task HandleAsyncWhenActivationChangesDuringReadResultLandsInOriginPane()
    {
        DualPaneSession panes = CreatePanes(out ScriptedDirectoryReadPort left, out ScriptedDirectoryReadPort right);
        DirectoryListing rightListing = Listing("C:\\right", ("c.txt", DirectoryEntryKind.File), ("d.txt", DirectoryEntryKind.File));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing));
        TaskCompletionSource<DirectoryReadOutcome> pendingLeft = left.EnqueuePending();
        DirectoryListing leftListing = Listing("C:\\left", ("a.txt", DirectoryEntryKind.File));
        _ = await panes.NavigateAsync(PaneSide.Right, ParsePath("C:\\right"), CancellationToken.None);

        Task<DualPaneSnapshot> leftNavigation = panes.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        DualPaneSnapshot switched = await panes.HandleAsync(UserIntent.ActivateOtherPane, CancellationToken.None);
        DualPaneSnapshot rightMoved = await panes.HandleAsync(UserIntent.MoveNext, CancellationToken.None);
        pendingLeft.SetResult(DirectoryReadOutcome.Succeeded(leftListing));
        DualPaneSnapshot completed = await leftNavigation;

        _ = Assert.IsInstanceOfType<PaneLoading>(switched.Left.Activity);
        Assert.AreSame(PaneSide.Right, switched.ActiveSide);
        Assert.AreSame(rightListing.Entries[1].Path, Focus(rightMoved.Right));
        Assert.AreSame(leftListing, Assert.IsInstanceOfType<PaneContentListed>(completed.Left.Content).Listing);
        Assert.AreSame(rightListing, Assert.IsInstanceOfType<PaneContentListed>(completed.Right.Content).Listing);
        Assert.AreSame(PaneSide.Right, completed.ActiveSide);
        Assert.HasCount(1, left.Requests);
    }

    /// <summary>Proves one session cannot serve both sides.</summary>
    [TestMethod]
    public void ConstructWhenBothSidesShareOneSessionThrowsArgumentException()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        PaneSession shared = new(port, Capacity(4), DirectoryListing.EntryBoundaryLimit);

        _ = Assert.ThrowsExactly<ArgumentException>(() => new DualPaneSession(shared, shared));
    }

    private static FileSystemPath? Focus(PaneSnapshot snapshot)
    {
        return Assert.IsInstanceOfType<PaneContentListed>(snapshot.Content).State.FocusItem;
    }

    private static DualPaneSession CreatePanes(out ScriptedDirectoryReadPort left, out ScriptedDirectoryReadPort right)
    {
        left = ScriptedDirectoryReadPort.Create();
        right = ScriptedDirectoryReadPort.Create();
        return new DualPaneSession(
            new PaneSession(left, Capacity(4), DirectoryListing.EntryBoundaryLimit),
            new PaneSession(right, Capacity(4), DirectoryListing.EntryBoundaryLimit));
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
