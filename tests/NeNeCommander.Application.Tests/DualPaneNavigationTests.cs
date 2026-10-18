using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves DualPaneNavigation behavior and boundaries.</summary>
[TestClass]
public sealed class DualPaneNavigationTests
{
    /// <summary>Proves each side reads its own location regardless of the active side.</summary>
    [TestMethod]
    public async Task NavigateAsyncWhenSideIsGivenReadsIntoThatSideOnly()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing rightListing = DualPaneSessionTestContext.Listing("C:\\Users", ("xi", DirectoryEntryKind.Directory));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing, null));

        DualPaneSnapshot snapshot = await fixture.Panes.NavigateAsync(PaneSide.Right, DualPaneSessionTestContext.ParsePath("C:\\Users"), CancellationToken.None);

        Assert.IsEmpty(fixture.Left.Requests);
        Assert.HasCount(1, fixture.Right.Requests);
        Assert.AreSame(rightListing, Assert.IsInstanceOfType<PaneContentListed>(snapshot.Right.Content).Listing);
        Assert.AreSame(PaneContent.Absent, snapshot.Left.Content);
        Assert.AreSame(PaneSide.Left, snapshot.ActiveSide);
        Assert.AreSame(snapshot.Right, snapshot.Of(PaneSide.Right));
        Assert.AreSame(snapshot.Left, snapshot.Of(PaneSide.Left));
        Assert.AreNotSame(snapshot.Left, snapshot.Right);
    }

    /// <summary>Proves activation toggles the side without touching either pane's focus.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenActivateOtherPaneTogglesActiveSideWithoutMovingFocus()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File)), null));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing("C:\\right", ("c.txt", DirectoryEntryKind.File), ("d.txt", DirectoryEntryKind.File)), null));
        _ = await fixture.Panes.NavigateAsync(PaneSide.Left, DualPaneSessionTestContext.ParsePath("C:\\left"), CancellationToken.None);
        DualPaneSnapshot before = await fixture.Panes.NavigateAsync(PaneSide.Right, DualPaneSessionTestContext.ParsePath("C:\\right"), CancellationToken.None);

        DualPaneSnapshot toRight = await fixture.Panes.HandleAsync(UserIntent.ActivateOtherPane, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot backToLeft = await fixture.Panes.HandleAsync(UserIntent.ActivateOtherPane, RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreSame(PaneSide.Right, toRight.ActiveSide);
        Assert.AreSame(PaneSide.Left, backToLeft.ActiveSide);
        Assert.AreSame(before.Left, toRight.Left);
        Assert.AreSame(before.Right, toRight.Right);
        Assert.HasCount(1, fixture.Left.Requests);
        Assert.HasCount(1, fixture.Right.Requests);
    }

    /// <summary>Proves movement reaches only the active pane.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenMovementArrivesOnlyActivePaneChanges()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing leftListing = DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
        DirectoryListing rightListing = DualPaneSessionTestContext.Listing("C:\\right", ("c.txt", DirectoryEntryKind.File), ("d.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, rightListing);

        DualPaneSnapshot leftMoved = await fixture.Panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.ActivateOtherPane, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot rightMoved = await fixture.Panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreSame(leftListing.Entries[1].Path, DualPaneSessionTestContext.Focus(leftMoved.Left));
        Assert.AreSame(rightListing.Entries[0].Path, DualPaneSessionTestContext.Focus(leftMoved.Right));
        Assert.AreSame(leftListing.Entries[1].Path, DualPaneSessionTestContext.Focus(rightMoved.Left));
        Assert.AreSame(rightListing.Entries[1].Path, DualPaneSessionTestContext.Focus(rightMoved.Right));
    }

    /// <summary>Proves each active pane moves only its own independent location-history cursor.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenHistoryMovesKeepsTheOtherPaneHistoryIndependent()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing left = DualPaneSessionTestContext.Listing("C:\\left");
        DirectoryListing right = DualPaneSessionTestContext.Listing("C:\\right");
        DirectoryListing leftNext = DualPaneSessionTestContext.Listing("C:\\left-next");
        DirectoryListing rightNext = DualPaneSessionTestContext.Listing("C:\\right-next");
        await fixture.ListBothAsync(left, right);
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(leftNext, null));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(rightNext, null));
        _ = await fixture.Panes.NavigateAsync(PaneSide.Left, leftNext.Location, CancellationToken.None);
        _ = await fixture.Panes.NavigateAsync(PaneSide.Right, rightNext.Location, CancellationToken.None);
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(left, null));

        DualPaneSnapshot leftBack = await fixture.Panes.HandleAsync(
            UserIntent.NavigateBack,
            RecordingDualPaneObserver.Create(),
            CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(
            UserIntent.ActivateOtherPane,
            RecordingDualPaneObserver.Create(),
            CancellationToken.None);
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(right, null));
        DualPaneSnapshot rightBack = await fixture.Panes.HandleAsync(
            UserIntent.NavigateBack,
            RecordingDualPaneObserver.Create(),
            CancellationToken.None);

        AssertHistoryIndex(leftBack.Left.Content, 0);
        AssertHistoryIndex(leftBack.Right.Content, 1);
        AssertHistoryIndex(rightBack.Left.Content, 0);
        AssertHistoryIndex(rightBack.Right.Content, 0);
    }

    /// <summary>Proves hidden-item toggling changes only the active pane.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenToggleHiddenItemsChangesOnlyActivePane()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing leftListing = DualPaneSessionTestContext.ListingWithVisibility(
            "C:\\left",
            ("hidden.txt", DirectoryEntryKind.File, EntryVisibility.Hidden),
            ("visible.txt", DirectoryEntryKind.File, EntryVisibility.Normal));
        DirectoryListing rightListing = DualPaneSessionTestContext.ListingWithVisibility(
            "C:\\right",
            ("hidden.txt", DirectoryEntryKind.File, EntryVisibility.Hidden),
            ("visible.txt", DirectoryEntryKind.File, EntryVisibility.Normal));
        await fixture.ListBothAsync(leftListing, rightListing);

        DualPaneSnapshot shown = await fixture.Panes.HandleAsync(
            UserIntent.ToggleHiddenItems,
            RecordingDualPaneObserver.Create(),
            CancellationToken.None);

        Assert.HasCount(
            2,
            Assert.IsInstanceOfType<PaneContentListed>(shown.Left.Content).State.VisibleEntries);
        Assert.HasCount(
            1,
            Assert.IsInstanceOfType<PaneContentListed>(shown.Right.Content).State.VisibleEntries);
    }

    /// <summary>Proves a read in flight lands in the pane that started it even after activation changes.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-016")]
    public async Task HandleAsyncWhenActivationChangesDuringReadResultLandsInOriginPane()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing rightListing = DualPaneSessionTestContext.Listing("C:\\right", ("c.txt", DirectoryEntryKind.File), ("d.txt", DirectoryEntryKind.File));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing, null));
        TaskCompletionSource<DirectoryReadOutcome> pendingLeft = fixture.Left.EnqueuePending();
        DirectoryListing leftListing = DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File));
        _ = await fixture.Panes.NavigateAsync(PaneSide.Right, DualPaneSessionTestContext.ParsePath("C:\\right"), CancellationToken.None);

        Task<DualPaneSnapshot> leftNavigation = fixture.Panes.NavigateAsync(PaneSide.Left, DualPaneSessionTestContext.ParsePath("C:\\left"), CancellationToken.None);
        DualPaneSnapshot switched = await fixture.Panes.HandleAsync(UserIntent.ActivateOtherPane, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot rightMoved = await fixture.Panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None);
        pendingLeft.SetResult(DirectoryReadOutcome.Succeeded(leftListing, null));
        DualPaneSnapshot completed = await leftNavigation;

        _ = Assert.IsInstanceOfType<PaneLoading>(switched.Left.Activity);
        Assert.AreSame(PaneSide.Right, switched.ActiveSide);
        Assert.AreSame(rightListing.Entries[1].Path, DualPaneSessionTestContext.Focus(rightMoved.Right));
        Assert.AreSame(leftListing, Assert.IsInstanceOfType<PaneContentListed>(completed.Left.Content).Listing);
        Assert.AreSame(rightListing, Assert.IsInstanceOfType<PaneContentListed>(completed.Right.Content).Listing);
        Assert.AreSame(PaneSide.Right, completed.ActiveSide);
        Assert.HasCount(1, fixture.Left.Requests);
    }

    /// <summary>Proves Escape abandons only the active pane's read; the passive pane's read continues until its own Escape.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenBothPanesLoadEscapeAbandonsOnlyTheActivePane()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing leftListing = DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File));
        DirectoryListing rightListing = DualPaneSessionTestContext.Listing("C:\\right", ("c.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, rightListing);
        TaskCompletionSource<DirectoryReadOutcome> leftRead = fixture.Left.EnqueuePending();
        TaskCompletionSource<DirectoryReadOutcome> rightRead = fixture.Right.EnqueuePending();
        Task<DualPaneSnapshot> leftNavigation = fixture.Panes.NavigateAsync(PaneSide.Left, DualPaneSessionTestContext.ParsePath("C:\\left-target"), CancellationToken.None);
        Task<DualPaneSnapshot> rightNavigation = fixture.Panes.NavigateAsync(PaneSide.Right, DualPaneSessionTestContext.ParsePath("C:\\right-target"), CancellationToken.None);

        DualPaneSnapshot leftAbandoned = await fixture.Panes.HandleAsync(UserIntent.Escape, RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreEqual("C:\\left-target", Assert.IsInstanceOfType<PaneReadAbandoned>(leftAbandoned.Left.Activity).Target.CanonicalText);
        Assert.AreSame(leftListing, Assert.IsInstanceOfType<PaneContentListed>(leftAbandoned.Left.Content).Listing);
        _ = Assert.IsInstanceOfType<PaneLoading>(leftAbandoned.Right.Activity);
        Assert.IsTrue(fixture.Left.Tokens[1].IsCancellationRequested);
        Assert.IsFalse(fixture.Right.Tokens[1].IsCancellationRequested);
        Assert.AreSame(OperationActivity.Idle, leftAbandoned.Operation);
        _ = await fixture.Panes.HandleAsync(UserIntent.ActivateOtherPane, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot rightAbandoned = await fixture.Panes.HandleAsync(UserIntent.Escape, RecordingDualPaneObserver.Create(), CancellationToken.None);
        Assert.AreEqual("C:\\right-target", Assert.IsInstanceOfType<PaneReadAbandoned>(rightAbandoned.Right.Activity).Target.CanonicalText);
        Assert.AreSame(rightListing, Assert.IsInstanceOfType<PaneContentListed>(rightAbandoned.Right.Content).Listing);
        Assert.IsTrue(fixture.Right.Tokens[1].IsCancellationRequested);
        leftRead.SetResult(DirectoryReadOutcome.Cancelled());
        rightRead.SetResult(DirectoryReadOutcome.Cancelled());
        _ = await leftNavigation;
        _ = await rightNavigation;
    }

    private static void AssertHistoryIndex(PaneContent content, int expected)
    {
        Assert.AreEqual(expected, Assert.IsInstanceOfType<PaneContentListed>(content).State.NavigationHistory.CurrentIndex);
    }
}
