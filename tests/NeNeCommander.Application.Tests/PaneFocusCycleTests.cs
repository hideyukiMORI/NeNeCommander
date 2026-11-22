using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves single-step movement cycles only through the current visible set.</summary>
[TestClass]
public sealed class PaneFocusCycleTests
{
    /// <summary>Proves both edge transitions preserve selected identities and owned projections.</summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(-1)]
    public void ApplyWhenSingleStepCrossesEdgeCyclesAndPreservesSelection(int step)
    {
        PaneState state = ThreeEntries(4);
        state = PaneReducer.Apply(state, UserIntent.ToggleSelection);
        state = PaneReducer.Apply(state, UserIntent.MoveNext);
        state = PaneReducer.Apply(state, UserIntent.ToggleSelection);
        state = PaneReducer.Apply(state, step > 0 ? UserIntent.FocusLast : UserIntent.FocusFirst);
        int expectedIndex = step > 0 ? 0 : 2;

        PaneState moved = PaneReducer.Apply(state, step > 0 ? UserIntent.MoveNext : UserIntent.MovePrevious);

        Assert.AreSame(state.VisibleEntries[expectedIndex].Path, moved.FocusItem);
        Assert.AreSame(state.Selection, moved.Selection);
        Assert.HasCount(2, moved.Selection);
        Assert.AreSame(state.Entries, moved.Entries);
        Assert.AreSame(state.VisibleEntries, moved.VisibleEntries);
        Assert.AreSame(state.NavigationHistory, moved.NavigationHistory);
        Assert.AreNotSame(state, moved);
        Assert.AreSame(state.VisibleEntries[2 - expectedIndex].Path, state.FocusItem);
    }

    /// <summary>Proves cycling uses the visible projected order after a descending sort.</summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(-1)]
    public void ApplyWhenHiddenEntriesSurroundDescendingOrderCyclesVisibleEntries(int step)
    {
        DirectoryListing listing = PaneSessionTestContext.HiddenListing("C:\\cycle",
            ("a-hidden", EntryVisibility.Hidden), ("b-visible", EntryVisibility.Normal),
            ("c-visible", EntryVisibility.Normal), ("z-hidden", EntryVisibility.Hidden));
        PaneState state = PaneReducer.Navigate(listing, PaneSessionTestContext.Capacity(4), null, HiddenItemVisibility.Hidden);
        state = PaneReducer.Apply(state, UserIntent.ToggleSelection);
        state = PaneReducer.Apply(state, UserIntent.SortByName);
        state = PaneReducer.Apply(state, step > 0 ? UserIntent.FocusLast : UserIntent.FocusFirst);

        PaneState moved = PaneReducer.Apply(state, step > 0 ? UserIntent.MoveNext : UserIntent.MovePrevious);

        PaneSessionTestContext.AssertVisibleNames(["c-visible", "b-visible"], moved);
        Assert.AreSame(listing.Entries[step > 0 ? 2 : 1].Path, moved.FocusItem);
        Assert.AreSame(state.Selection, moved.Selection);
        Assert.AreSame(state.SortOrder, moved.SortOrder);
        Assert.AreSame(state.HiddenItemVisibility, moved.HiddenItemVisibility);
    }

    /// <summary>Proves absence of visible entries keeps both directions inert.</summary>
    [TestMethod]
    public void ApplyWhenNoEntriesAreVisibleKeepsSnapshotForBothDirections()
    {
        DirectoryListing empty = PaneSessionTestContext.Listing("C:\\empty");
        DirectoryListing hidden = PaneSessionTestContext.HiddenListing("C:\\hidden", ("hidden", EntryVisibility.Hidden));
        foreach (DirectoryListing listing in new[] { empty, hidden })
        {
            PaneState state = PaneReducer.Navigate(listing, PaneSessionTestContext.Capacity(1), null, HiddenItemVisibility.Hidden);

            Assert.AreSame(state, PaneReducer.Apply(state, UserIntent.MovePrevious));
            Assert.AreSame(state, PaneReducer.Apply(state, UserIntent.MoveNext));
            Assert.IsNull(state.FocusItem);
            Assert.IsEmpty(state.Selection);
        }
    }

    /// <summary>Proves a one-entry visible set retains its item and explicit selection.</summary>
    [TestMethod]
    public void ApplyWhenOnlyOneEntryIsVisibleKeepsFocusAndSelection()
    {
        DirectoryListing listing = PaneSessionTestContext.HiddenListing("C:\\single",
            ("a-hidden", EntryVisibility.Hidden), ("b-visible", EntryVisibility.Normal), ("c-hidden", EntryVisibility.Hidden));
        PaneState initial = PaneReducer.Navigate(listing, PaneSessionTestContext.Capacity(1), null, HiddenItemVisibility.Hidden);
        initial = PaneReducer.Apply(initial, UserIntent.ToggleSelection);
        PaneState state = initial;

        foreach (UserIntent intent in new[] { UserIntent.MovePrevious, UserIntent.MovePrevious, UserIntent.MoveNext, UserIntent.MoveNext })
        {
            state = PaneReducer.Apply(state, intent);
            Assert.AreSame(listing.Entries[1].Path, state.FocusItem);
            Assert.AreSame(initial.Selection, state.Selection);
            Assert.AreSame(initial.VisibleEntries, state.VisibleEntries);
            Assert.AreNotSame(initial, state);
            Assert.AreEqual(initial, state);
        }
    }

    /// <summary>Proves half-page movement still clamps, including a one-row page step.</summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(4)]
    public void ApplyWhenPagingOrTargetingEndsKeepsExistingBoundaries(int capacity)
    {
        PaneState first = ThreeEntries(capacity);
        first = PaneReducer.Apply(first, UserIntent.ToggleSelection);
        PaneState last = PaneReducer.Apply(first, UserIntent.FocusLast);

        PaneState beforeFirst = PaneReducer.Apply(first, UserIntent.MoveHalfPageUp);
        PaneState afterLast = PaneReducer.Apply(last, UserIntent.MoveHalfPageDown);
        PaneState pageDown = PaneReducer.Apply(first, UserIntent.MoveHalfPageDown);

        Assert.AreSame(first.FocusItem, beforeFirst.FocusItem);
        Assert.AreSame(last.FocusItem, afterLast.FocusItem);
        Assert.AreSame(first.Selection, beforeFirst.Selection);
        Assert.AreSame(first.Selection, afterLast.Selection);
        Assert.AreSame(first.VisibleEntries[capacity == 1 ? 1 : 2].Path, pageDown.FocusItem);
        Assert.AreSame(first.FocusItem, PaneReducer.Apply(last, UserIntent.FocusFirst).FocusItem);
        Assert.AreSame(last.FocusItem, PaneReducer.Apply(first, UserIntent.FocusLast).FocusItem);
    }

    private static PaneState ThreeEntries(int capacity)
    {
        DirectoryListing listing = PaneSessionTestContext.Listing("C:\\cycle",
            ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File), ("c.txt", DirectoryEntryKind.File));
        return PaneReducer.Navigate(listing, PaneSessionTestContext.Capacity(capacity), null, HiddenItemVisibility.Hidden);
    }
}
