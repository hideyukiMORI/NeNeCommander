using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Launching;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves the pane-local admission and range endpoint boundaries cannot be bypassed.</summary>
[TestClass]
public sealed class PanePointerBoundaryTests
{
    /// <summary>Proves the pane-local route rejects a current row request before a listing exists.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenUnlistedRejectsForeignRowWithoutReadOrLaunchAsync()
    {
        DirectoryListing listing = Listing();
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        ScriptedFileLauncher launcher = new();
        PaneSession session = PaneSessionTestContext.CreateSession(port, launcher);
        PaneSnapshot before = session.Current;

        PaneSnapshot after = await session.HandleAsync(UserIntent.InteractWithPaneEntry(
            PaneSide.Left, before, listing.Entries[0], PaneEntryAction.Open), CancellationToken.None);

        Assert.AreSame(before, after);
        Assert.IsEmpty(port.Requests);
        Assert.IsEmpty(launcher.Targets);
    }

    /// <summary>Proves direct pane input cannot bypass the read owner through a current snapshot.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenDirectPaneReadRunsRejectsCurrentSurfaceAsync()
    {
        DirectoryListing listing = Listing();
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        TaskCompletionSource<DirectoryReadOutcome> pending = port.EnqueuePending();
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        PaneSnapshot ready = await session.NavigateAsync(listing.Location, CancellationToken.None);
        Assert.IsTrue(ready.CanNavigateParent);
        Task<PaneSnapshot> read = session.NavigateAsync(listing.Location, CancellationToken.None);
        PaneSnapshot before = session.Current;
        foreach (UserIntent selection in new[] { UserIntent.SelectAll, UserIntent.InvertSelection, UserIntent.ClearSelection,
            UserIntent.SelectRangeNext, UserIntent.SelectRangePrevious, UserIntent.SelectRangeHalfPageDown,
            UserIntent.SelectRangeHalfPageUp, UserIntent.SelectRangeFirst, UserIntent.SelectRangeLast })
        {
            Assert.AreSame(before, await session.HandleAsync(selection, CancellationToken.None));
        }

        PaneSnapshot after = await session.HandleAsync(UserIntent.InteractWithPaneSurface(
            PaneSide.Left, before, PaneSurfaceAction.Activate), CancellationToken.None);

        Assert.AreSame(before, after);
        Assert.AreSame(ready.Content, before.Content);
        Assert.HasCount(2, port.Requests);
        Assert.IsFalse(before.CanNavigateParent);
        pending.SetResult(DirectoryReadOutcome.Cancelled());
        PaneSnapshot completed = await read;
        Assert.AreSame(ready.Content, completed.Content);
        Assert.IsTrue(completed.CanNavigateParent);
    }

    /// <summary>Proves direct pane input cannot bypass the launch owner through a current snapshot.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenDirectPaneLaunchRunsRejectsCurrentSurfaceAsync()
    {
        DirectoryListing listing = Listing();
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        ScriptedFileLauncher launcher = new();
        TaskCompletionSource<FileLaunchOutcome> pending = launcher.EnqueuePending();
        PaneSession session = PaneSessionTestContext.CreateSession(port, launcher);
        _ = await session.NavigateAsync(listing.Location, CancellationToken.None);
        Task<PaneSnapshot> launch = session.HandleAsync(UserIntent.OpenFocused, CancellationToken.None);
        PaneSnapshot before = session.Current;
        foreach (UserIntent selection in new[] { UserIntent.SelectAll, UserIntent.InvertSelection, UserIntent.ClearSelection,
            UserIntent.SelectRangeNext, UserIntent.SelectRangePrevious, UserIntent.SelectRangeHalfPageDown,
            UserIntent.SelectRangeHalfPageUp, UserIntent.SelectRangeFirst, UserIntent.SelectRangeLast })
        {
            Assert.AreSame(before, await session.HandleAsync(selection, CancellationToken.None));
        }

        PaneSnapshot after = await session.HandleAsync(UserIntent.InteractWithPaneSurface(
            PaneSide.Left, before, PaneSurfaceAction.ClearSelection), CancellationToken.None);

        Assert.AreSame(before, after);
        Assert.HasCount(1, launcher.Targets);
        Assert.IsFalse(before.CanNavigateParent);
        pending.SetResult(FileLaunchOutcome.Accepted());
        Assert.IsTrue((await launch).CanNavigateParent);
    }

    /// <summary>Proves the reducer repairs an absent intermediate focus with the qualified range target.</summary>
    [TestMethod]
    public void ApplyInteractionWhenAnchorAndIntermediateFocusAbsentUsesTarget()
    {
        DirectoryListing listing = Listing();
        PaneState state = PaneReducer.Navigate(listing, DualPaneSessionTestContext.Capacity(4), null,
            NeNeCommander.Application.Settings.HiddenItemVisibility.Hidden);
        PaneState unfocused = state.Transition(null, state.Selection, null);
        PaneInteraction request = Assert.IsInstanceOfType<PaneInteraction>(UserIntent.InteractWithPaneEntry(
            PaneSide.Left, PaneSnapshot.Initial, listing.Entries[1], PaneEntryAction.SelectRange));

        PaneState after = PaneReducer.ApplyInteraction(unfocused, request);

        Assert.AreSame(listing.Entries[1].Path, after.FocusItem);
        Assert.AreSame(after.FocusItem, after.SelectionAnchor);
        Assert.HasCount(1, after.Selection);
        Assert.AreSame(after.FocusItem, after.Selection[0]);
    }

    /// <summary>Proves equal path values cannot substitute for owned endpoints in the internal range boundary.</summary>
    [TestMethod]
    public void RangeWhenEndpointHasForeignIdentityThrowsInsteadOfSelectingAnotherRow()
    {
        DirectoryListing listing = Listing();
        DirectoryListing foreign = Listing();
        PaneState state = PaneReducer.Navigate(listing, DualPaneSessionTestContext.Capacity(4), null,
            NeNeCommander.Application.Settings.HiddenItemVisibility.Hidden);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => PaneSelection.Range(
            state, foreign.Entries[0].Path, listing.Entries[1].Path, PaneEntryAction.SelectRange));
        _ = Assert.ThrowsExactly<InvalidOperationException>(() => PaneSelection.Range(
            state, listing.Entries[0].Path, foreign.Entries[1].Path, PaneEntryAction.SelectRange));

        Assert.IsEmpty(state.Selection);
        Assert.IsNull(state.SelectionAnchor);
    }

    /// <summary>Supplies the surfaces that must never start a navigation or Shell handoff.</summary>
    public static IEnumerable<object[]> PassiveSurfaces()
    {
        return [[PaneSurfaceAction.Activate], [PaneSurfaceAction.ClearSelection], [PaneSurfaceAction.OpenContextMenu]];
    }

    /// <summary>Proves a qualified passive surface cannot acquire the parent-navigation behavior.</summary>
    [TestMethod]
    [DynamicData(nameof(PassiveSurfaces))]
    public async Task HandleAsyncWhenSurfaceIsPassiveDoesNotNavigateOrLaunchAsync(PaneSurfaceAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        DirectoryListing listing = Listing();
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        ScriptedFileLauncher launcher = new();
        PaneSession session = PaneSessionTestContext.CreateSession(port, launcher);
        PaneSnapshot before = await session.NavigateAsync(listing.Location, CancellationToken.None);

        PaneSnapshot after = await session.HandleAsync(UserIntent.InteractWithPaneSurface(
            PaneSide.Left, before, action), CancellationToken.None);

        Assert.AreNotSame(before, after);
        PaneContentListed content = Assert.IsInstanceOfType<PaneContentListed>(after.Content);
        PaneContentListed previous = Assert.IsInstanceOfType<PaneContentListed>(before.Content);
        Assert.AreSame(listing, content.Listing);
        Assert.AreSame(previous.State.FocusItem, content.State.FocusItem);
        Assert.AreSame(previous.State.NavigationHistory, content.State.NavigationHistory);
        Assert.AreSame(before.Activity, after.Activity);
        Assert.IsTrue(after.CanNavigateParent);
        Assert.HasCount(1, port.Requests);
        Assert.IsEmpty(launcher.Targets);
    }

    private static DirectoryListing Listing()
    {
        return PaneSessionTestContext.Listing("C:\\pointer", ("a", DirectoryEntryKind.File), ("b", DirectoryEntryKind.File));
    }
}
