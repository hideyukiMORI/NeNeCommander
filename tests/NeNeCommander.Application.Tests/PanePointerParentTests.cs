using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves parent entry uses exact pane ownership and the existing provider navigation contract.</summary>
[TestClass]
public sealed class PanePointerParentTests
{
    /// <summary>Supplies both addressed sides across all supported provider boundaries.</summary>
    public static IEnumerable<object[]> SidesAndLocations()
    {
        return [
            ["left", @"C:\child", @"C:\"],
            ["right", @"C:\child", @"C:\"],
            ["left", @"\\server\share\child", @"\\server\share"],
            ["right", @"\\server\share\child", @"\\server\share"],
            ["left", @"\\wsl.localhost\Ubuntu\child", @"\\wsl.localhost\Ubuntu"],
            ["right", @"\\wsl.localhost\Ubuntu\child", @"\\wsl.localhost\Ubuntu"],
        ];
    }

    /// <summary>Proves either inactive target navigates once, focuses its origin and records history.</summary>
    [TestMethod]
    [DynamicData(nameof(SidesAndLocations))]
    public async Task ParentWhenQualifiedNavigatesOnlyAddressedPaneAndRejectsReplayAsync(string sideName, string location, string parent)
    {
        PaneSide side = sideName == "left" ? PaneSide.Left : PaneSide.Right;
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing child = DualPaneSessionTestContext.Listing(location, ("file.txt", DirectoryEntryKind.File));
        DirectoryListing parentListing = DualPaneSessionTestContext.Listing(parent, ("child", DirectoryEntryKind.Directory));
        await fixture.ListBothAsync(child, child);
        if (side == PaneSide.Left)
        {
            _ = await HandleAsync(fixture.Panes, UserIntent.ActivateOtherPane);
        }
        PaneSession target = side == PaneSide.Left ? fixture.LeftSession : fixture.RightSession;
        _ = await target.HandleAsync(UserIntent.ToggleSelection, CancellationToken.None);
        DualPaneSnapshot before = fixture.Panes.Current;
        ScriptedDirectoryReadPort port = side == PaneSide.Left ? fixture.Left : fixture.Right;
        port.Enqueue(DirectoryReadOutcome.Succeeded(parentListing, null));
        UserIntent request = Parent(side, before.Of(side));
        using CancellationTokenSource cancellation = new();

        DualPaneSnapshot after = await fixture.Panes.HandleAsync(request, RecordingDualPaneObserver.Create(), cancellation.Token);
        DualPaneSnapshot replay = await HandleAsync(fixture.Panes, request);

        PaneContentListed content = Assert.IsInstanceOfType<PaneContentListed>(after.Of(side).Content);
        Assert.AreSame(side, after.ActiveSide);
        Assert.AreSame(before.Of(side.Other), after.Of(side.Other));
        Assert.AreSame(parentListing, content.Listing);
        Assert.AreSame(parentListing.Entries[0].Path, content.State.FocusItem);
        Assert.AreEqual(child.Location, content.State.NavigationHistory.BackTarget);
        Assert.IsEmpty(content.State.Selection);
        Assert.IsNull(content.State.SelectionAnchor);
        Assert.HasCount(2, port.Requests);
        Assert.AreEqual(parentListing.Location, port.Requests[1].Location);
        Assert.AreSame(after.Of(side), replay.Of(side));
        Assert.IsEmpty(fixture.Port.Calls);
    }

    /// <summary>Proves every provider root refuses before activation or an accepted-input receipt.</summary>
    [TestMethod]
    [DataRow(@"C:\")]
    [DataRow(@"\\server\share")]
    [DataRow(@"\\wsl.localhost\Ubuntu")]
    public async Task ParentWhenRootDoesNotActivateOrReadAsync(string root)
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing listing = DualPaneSessionTestContext.Listing(root);
        await fixture.ListBothAsync(listing, listing);
        DualPaneSnapshot before = fixture.Panes.Current;

        DualPaneSnapshot after = await HandleAsync(fixture.Panes, Parent(PaneSide.Right, before.Right));

        AssertUnchanged(before, after);
        Assert.IsFalse(before.Right.CanNavigateParent);
        Assert.HasCount(1, fixture.Right.Requests);
    }

    /// <summary>Proves absent content does not authorize parent navigation or activation.</summary>
    [TestMethod]
    public async Task ParentWhenUnlistedDoesNotCreateContentOrActivateAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DualPaneSnapshot before = fixture.Panes.Current;
        DualPaneSnapshot after = await HandleAsync(fixture.Panes, Parent(PaneSide.Right, before.Right));
        AssertUnchanged(before, after);
        Assert.IsEmpty(fixture.Right.Requests);
    }

    /// <summary>Proves stale and foreign wrappers cannot use an equal location as authority.</summary>
    [TestMethod]
    public async Task ParentWhenOwnerIsStaleOrForeignDoesNotActivateOrReadAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing listing = DualPaneSessionTestContext.Listing(@"C:\child", ("file", DirectoryEntryKind.File));
        await fixture.ListBothAsync(listing, listing);
        PaneSnapshot stale = fixture.Panes.Current.Right;
        _ = await fixture.RightSession.HandleAsync(UserIntent.ToggleSelection, CancellationToken.None);
        DualPaneSnapshot before = fixture.Panes.Current;

        AssertUnchanged(before, await HandleAsync(fixture.Panes, Parent(PaneSide.Right, stale)));
        AssertUnchanged(before, await HandleAsync(fixture.Panes, Parent(PaneSide.Right, before.Left)));
        Assert.HasCount(1, fixture.Right.Requests);
    }

    /// <summary>Proves an in-flight parent read blocks replay and other-pane parent input; Escape abandons only it.</summary>
    [TestMethod]
    public async Task ParentWhenPendingFreezesBothSidesAndEscapeAbandonsExistingReadAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing listing = DualPaneSessionTestContext.Listing(@"C:\child");
        await fixture.ListBothAsync(listing, listing);
        PaneContent retained = fixture.Panes.Current.Right.Content;
        TaskCompletionSource<DirectoryReadOutcome> release = fixture.Right.EnqueuePendingInline();
        using CancellationTokenSource cancellation = new();
        UserIntent request = Parent(PaneSide.Right, fixture.Panes.Current.Right);
        Task<DualPaneSnapshot> reading = fixture.Panes.HandleAsync(request, RecordingDualPaneObserver.Create(), cancellation.Token);
        DualPaneSnapshot pending = fixture.Panes.Current;
        Assert.AreSame(PaneSide.Right, pending.ActiveSide);
        _ = Assert.IsInstanceOfType<PaneLoading>(pending.Right.Activity);
        AssertUnchanged(pending, await HandleAsync(fixture.Panes, request));
        AssertUnchanged(pending, await HandleAsync(fixture.Panes, Parent(PaneSide.Left, pending.Left)));
        AssertUnchanged(pending, await HandleAsync(fixture.Panes, Parent(PaneSide.Right, pending.Right)));

        _ = await HandleAsync(fixture.Panes, UserIntent.Escape);
        DualPaneSnapshot abandoned = await reading;
        Assert.IsTrue(fixture.Right.Tokens[1].IsCancellationRequested);
        _ = Assert.IsInstanceOfType<PaneReadAbandoned>(abandoned.Right.Activity);
        Assert.AreSame(retained, abandoned.Right.Content);
        release.SetResult(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing(@"C:\"), null));
        Assert.AreSame(abandoned.Right, fixture.Panes.Current.Right);
        Assert.HasCount(2, fixture.Right.Requests);
        Assert.HasCount(1, fixture.Left.Requests);
    }

    /// <summary>Proves provider rejection preserves the displayed content and history after explicit activation.</summary>
    [TestMethod]
    public async Task ParentWhenReadFailsPreservesContentAndExistingFailureOutcomeAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing listing = DualPaneSessionTestContext.Listing(@"C:\child");
        await fixture.ListBothAsync(listing, listing);
        DualPaneSnapshot before = fixture.Panes.Current;
        fixture.Right.Enqueue(DirectoryReadOutcome.Failed(FileOperationFailureKind.AccessDenied));

        DualPaneSnapshot after = await HandleAsync(fixture.Panes, Parent(PaneSide.Right, before.Right));

        Assert.AreSame(PaneSide.Right, after.ActiveSide);
        Assert.AreSame(before.Right.Content, after.Right.Content);
        Assert.AreSame(before.Left, after.Left);
        PaneReadFailed failure = Assert.IsInstanceOfType<PaneReadFailed>(after.Right.Activity);
        Assert.AreSame(FileOperationFailureKind.AccessDenied, failure.Failure);
        Assert.IsTrue(after.Right.CanNavigateParent);
    }

    /// <summary>Proves the direct pane boundary shares the same navigation and cancellation route.</summary>
    [TestMethod]
    public async Task ParentWhenDirectPaneCancelsPreservesContentAndForwardsCancellationAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing listing = DualPaneSessionTestContext.Listing(@"C:\child");
        await fixture.ListBothAsync(listing, listing);
        PaneSnapshot before = fixture.RightSession.Current;
        TaskCompletionSource<DirectoryReadOutcome> release = fixture.Right.EnqueuePendingInline();
        using CancellationTokenSource cancellation = new();
        Task<PaneSnapshot> reading = fixture.RightSession.HandleAsync(Parent(PaneSide.Right, before), cancellation.Token);
        cancellation.Cancel();
        Assert.IsTrue(fixture.Right.Tokens[1].IsCancellationRequested);
        release.SetResult(DirectoryReadOutcome.Cancelled());

        PaneSnapshot after = await reading;

        _ = Assert.IsInstanceOfType<PaneReadCancelled>(after.Activity);
        Assert.AreSame(before.Content, after.Content);
        Assert.HasCount(2, fixture.Right.Requests);
    }

    private static UserIntent Parent(PaneSide side, PaneSnapshot snapshot)
    {
        return UserIntent.InteractWithPaneSurface(side, snapshot, PaneSurfaceAction.NavigateParent);
    }

    private static Task<DualPaneSnapshot> HandleAsync(DualPaneSession session, UserIntent intent)
    {
        return session.HandleAsync(intent, RecordingDualPaneObserver.Create(), CancellationToken.None);
    }

    private static void AssertUnchanged(DualPaneSnapshot before, DualPaneSnapshot after)
    {
        Assert.AreSame(before.Left, after.Left);
        Assert.AreSame(before.Right, after.Right);
        Assert.AreSame(before.ActiveSide, after.ActiveSide);
        Assert.AreSame(before.Operation, after.Operation);
    }
}
