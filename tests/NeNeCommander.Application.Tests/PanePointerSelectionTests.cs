using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves pointer selection uses the existing pane owner and immutable visible entries.</summary>
[TestClass]
public sealed class PanePointerSelectionTests
{
    /// <summary>Proves a plain left-pane click replaces the exact owned selection.</summary>
    [TestMethod]
    public async Task PlainRowClickWhenLeftSetsExactFocusSelectionAndAnchorAsync()
    {
        await AssertPlainRowClickAsync(PaneSide.Left);
    }

    /// <summary>Proves a plain right-pane click activates that side and replaces its selection.</summary>
    [TestMethod]
    public async Task PlainRowClickWhenRightSetsExactFocusSelectionAndAnchorAsync()
    {
        await AssertPlainRowClickAsync(PaneSide.Right);
    }

    private static async Task AssertPlainRowClickAsync(PaneSide side)
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing listing = Listing();
        await fixture.ListBothAsync(listing, listing);
        PaneSnapshot passive = fixture.Panes.Current.Of(side.Other);
        _ = await ClickAsync(fixture, side, listing.Entries[0], PaneEntryAction.Select);

        PaneState selected = await ClickAsync(fixture, side, listing.Entries[2], PaneEntryAction.Select);

        Assert.AreSame(side, fixture.Panes.Current.ActiveSide);
        Assert.AreSame(passive, fixture.Panes.Current.Of(side.Other));
        Assert.AreSame(listing.Entries[2].Path, selected.FocusItem);
        Assert.AreSame(selected.FocusItem, selected.SelectionAnchor);
        AssertSelection(selected, listing.Entries[2]);
        Assert.HasCount(1, fixture.Left.Requests);
        Assert.HasCount(1, fixture.Right.Requests);
    }

    /// <summary>Proves Control toggles without losing other selected entries and moves the anchor.</summary>
    [TestMethod]
    public async Task ControlClickTogglesOneEntryAndRetainsOtherMarks()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing listing = Listing();
        await fixture.ListBothAsync(listing, listing);
        _ = await ClickAsync(fixture, PaneSide.Left, listing.Entries[0], PaneEntryAction.Select);
        _ = await ClickAsync(fixture, PaneSide.Left, listing.Entries[2], PaneEntryAction.ToggleSelection);

        PaneState toggled = await ClickAsync(fixture, PaneSide.Left, listing.Entries[0], PaneEntryAction.ToggleSelection);

        AssertSelection(toggled, listing.Entries[2]);
        Assert.AreSame(listing.Entries[0].Path, toggled.FocusItem);
        Assert.AreSame(toggled.FocusItem, toggled.SelectionAnchor);
    }

    /// <summary>Proves inclusive ranges in either direction use visible order and retain their anchor.</summary>
    [TestMethod]
    [DataRow(0, 2)]
    [DataRow(2, 0)]
    public async Task ShiftClickSelectsInclusiveRangeInBothDirections(int anchor, int target)
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing listing = Listing();
        await fixture.ListBothAsync(listing, listing);
        _ = await ClickAsync(fixture, PaneSide.Left, listing.Entries[anchor], PaneEntryAction.Select);

        PaneState ranged = await ClickAsync(fixture, PaneSide.Left, listing.Entries[target], PaneEntryAction.SelectRange);

        AssertSelection(ranged, listing.Entries[0], listing.Entries[1], listing.Entries[2]);
        Assert.AreSame(listing.Entries[target].Path, ranged.FocusItem);
        Assert.AreSame(listing.Entries[anchor].Path, ranged.SelectionAnchor);
        CollectionAssert.AreEqual(listing.Entries.Take(3).Select(entry => entry.Path).ToArray(), ranged.Selection.ToArray());
    }

    /// <summary>Proves the first fallback anchor is committed rather than following each new focus.</summary>
    [TestMethod]
    public async Task RangeWithoutAnchorKeepsOriginalFocusAcrossSuccessiveGestures()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing listing = Listing();
        await fixture.ListBothAsync(listing, listing);
        _ = await fixture.Panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await ClickAsync(fixture, PaneSide.Left, listing.Entries[2], PaneEntryAction.SelectRange);

        PaneState ranged = await ClickAsync(fixture, PaneSide.Left, listing.Entries[3], PaneEntryAction.SelectRange);

        AssertSelection(ranged, listing.Entries[1], listing.Entries[2], listing.Entries[3]);
        Assert.AreSame(listing.Entries[1].Path, ranged.SelectionAnchor);
    }

    /// <summary>Proves Control+Shift unions a range once while preserving existing marks outside it.</summary>
    [TestMethod]
    public async Task AddRangeRetainsOutsideMarksAndDoesNotDuplicateItsAnchor()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing listing = Listing();
        await fixture.ListBothAsync(listing, listing);
        _ = await ClickAsync(fixture, PaneSide.Left, listing.Entries[0], PaneEntryAction.Select);
        _ = await ClickAsync(fixture, PaneSide.Left, listing.Entries[3], PaneEntryAction.ToggleSelection);

        PaneState ranged = await ClickAsync(fixture, PaneSide.Left, listing.Entries[2], PaneEntryAction.AddRange);

        AssertSelection(ranged, listing.Entries[0], listing.Entries[2], listing.Entries[3]);
        Assert.AreSame(listing.Entries[3].Path, ranged.SelectionAnchor);
        Assert.AreSame(listing.Entries[2].Path, ranged.FocusItem);
    }

    /// <summary>Proves headers preserve marks while blank background clears marks and anchor.</summary>
    [TestMethod]
    public async Task HeaderAndBlankActionsHaveDistinctSelectionEffects()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing listing = Listing();
        await fixture.ListBothAsync(listing, listing);
        PaneState selected = await ClickAsync(fixture, PaneSide.Right, listing.Entries[2], PaneEntryAction.Select);
        _ = await fixture.Panes.HandleAsync(UserIntent.ActivateOtherPane, RecordingDualPaneObserver.Create(), CancellationToken.None);
        PaneSnapshot captured = fixture.Panes.Current.Right;

        _ = await fixture.Panes.HandleAsync(UserIntent.InteractWithPaneSurface(PaneSide.Right, captured, PaneSurfaceAction.Activate), RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreSame(PaneSide.Right, fixture.Panes.Current.ActiveSide);
        Assert.AreNotSame(captured, fixture.Panes.Current.Right);
        Assert.AreSame(captured.Content, fixture.Panes.Current.Right.Content);
        Assert.AreSame(captured.Activity, fixture.Panes.Current.Right.Activity);
        Assert.AreSame(selected, StateOf(fixture.Panes.Current.Right));
        captured = fixture.Panes.Current.Right;
        _ = await fixture.Panes.HandleAsync(UserIntent.InteractWithPaneSurface(PaneSide.Right, captured, PaneSurfaceAction.ClearSelection), RecordingDualPaneObserver.Create(), CancellationToken.None);
        PaneState cleared = StateOf(fixture.Panes.Current.Right);
        Assert.IsEmpty(cleared.Selection);
        Assert.IsNull(cleared.SelectionAnchor);
        Assert.AreSame(selected.FocusItem, cleared.FocusItem);
        AssertSelection(selected, listing.Entries[2]);
    }

    /// <summary>Proves repeated selection still creates an explicit Application transition receipt.</summary>
    [TestMethod]
    public async Task RepeatedPlainClickCreatesNewSnapshotWithoutMutatingItsPredecessor()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing listing = Listing();
        await fixture.ListBothAsync(listing, listing);
        PaneState first = await ClickAsync(fixture, PaneSide.Left, listing.Entries[1], PaneEntryAction.Select);
        PaneSnapshot before = fixture.Panes.Current.Left;

        PaneState second = await ClickAsync(fixture, PaneSide.Left, listing.Entries[1], PaneEntryAction.Select);

        Assert.AreNotSame(before, fixture.Panes.Current.Left);
        Assert.AreNotSame(first, second);
        AssertSelection(first, listing.Entries[1]);
        AssertSelection(second, listing.Entries[1]);
    }

    /// <summary>Proves keyboard selection and movement share the same anchor lifetime.</summary>
    [TestMethod]
    public async Task KeyboardSpaceEstablishesAnchorMovementKeepsItAndEscapeClearsIt()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing listing = Listing();
        await fixture.ListBothAsync(listing, listing);
        _ = await fixture.Panes.HandleAsync(UserIntent.ToggleSelection, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None);
        Assert.AreSame(listing.Entries[0].Path, StateOf(fixture.Panes.Current.Left).SelectionAnchor);

        _ = await fixture.Panes.HandleAsync(UserIntent.Escape, RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.IsNull(StateOf(fixture.Panes.Current.Left).SelectionAnchor);
        Assert.IsEmpty(StateOf(fixture.Panes.Current.Left).Selection);
        Assert.AreSame(listing.Entries[1].Path, StateOf(fixture.Panes.Current.Left).FocusItem);
    }

    private static DirectoryListing Listing()
    {
        return DualPaneSessionTestContext.Listing("C:\\pointer", ("a", DirectoryEntryKind.File), ("b", DirectoryEntryKind.File), ("c", DirectoryEntryKind.File), ("d", DirectoryEntryKind.File));
    }

    private static async Task<PaneState> ClickAsync(DualPaneSessionTestContext fixture, PaneSide side, DirectoryEntry entry, PaneEntryAction action)
    {
        UserIntent intent = UserIntent.InteractWithPaneEntry(side, fixture.Panes.Current.Of(side), entry, action);
        DualPaneSnapshot after = await fixture.Panes.HandleAsync(intent, RecordingDualPaneObserver.Create(), CancellationToken.None);
        return StateOf(after.Of(side));
    }

    private static PaneState StateOf(PaneSnapshot snapshot)
    {
        return Assert.IsInstanceOfType<PaneContentListed>(snapshot.Content).State;
    }

    private static void AssertSelection(PaneState state, params DirectoryEntry[] entries)
    {
        FileSystemPath[] expected = [.. entries.Select(entry => entry.Path)];
        CollectionAssert.AreEquivalent(expected, state.Selection.ToArray());
        foreach (FileSystemPath selected in state.Selection)
        {
            Assert.IsTrue(state.VisibleEntries.Any(entry => ReferenceEquals(entry.Path, selected)));
        }
    }
}
