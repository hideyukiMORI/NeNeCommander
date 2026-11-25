using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves clamped keyboard ranges share the immutable pane anchor.</summary>
[TestClass]
public sealed class PaneKeyboardSelectionTests
{
    /// <summary>Proves consecutive ranges grow, shrink and cross the original focus.</summary>
    [TestMethod]
    public void ApplyWhenRangeCrossesAnchorReplacesInclusiveVisibleInterval()
    {
        PaneState initial = PaneReducer.Apply(CreateState(4), UserIntent.MoveNext);
        PaneState current = initial;
        UserIntent[] intents = [UserIntent.SelectRangeNext, UserIntent.SelectRangeNext,
            UserIntent.SelectRangePrevious, UserIntent.SelectRangePrevious, UserIntent.SelectRangePrevious];
        int[][] expected = [[1, 2], [1, 2, 3], [1, 2], [1], [0, 1]];
        int[] focus = [2, 3, 2, 1, 0];

        for (int index = 0; index < intents.Length; index++)
        {
            current = PaneReducer.Apply(current, intents[index]);

            Assert.AreSame(initial.VisibleEntries[1].Path, current.SelectionAnchor);
            Assert.AreSame(initial.VisibleEntries[focus[index]].Path, current.FocusItem);
            CollectionAssert.AreEqual(expected[index].Select(row => initial.VisibleEntries[row].Path).ToArray(), current.Selection.ToArray());
        }
        Assert.IsEmpty(initial.Selection);
        Assert.IsNull(initial.SelectionAnchor);
        Assert.AreSame(initial.VisibleEntries[1].Path, initial.FocusItem);
    }

    /// <summary>Proves all six range movements choose their declared clamped endpoint.</summary>
    [TestMethod]
    public void ApplyWhenRangeStartsAtMiddleUsesMovementDistanceAndOwnedEndpoints()
    {
        PaneState initial = PaneReducer.Apply(PaneReducer.Apply(CreateState(4), UserIntent.MoveNext), UserIntent.MoveNext);
        UserIntent[] intents = [UserIntent.SelectRangeNext, UserIntent.SelectRangePrevious,
            UserIntent.SelectRangeHalfPageDown, UserIntent.SelectRangeHalfPageUp,
            UserIntent.SelectRangeFirst, UserIntent.SelectRangeLast];
        int[] targets = [3, 1, 4, 0, 0, 4];
        int[][] expected = [[2, 3], [1, 2], [2, 3, 4], [0, 1, 2], [0, 1, 2], [2, 3, 4]];

        for (int index = 0; index < intents.Length; index++)
        {
            PaneState ranged = PaneReducer.Apply(initial, intents[index]);

            Assert.AreSame(initial.VisibleEntries[targets[index]].Path, ranged.FocusItem);
            Assert.AreSame(initial.FocusItem, ranged.SelectionAnchor);
            CollectionAssert.AreEqual(expected[index].Select(row => initial.VisibleEntries[row].Path).ToArray(), ranged.Selection.ToArray());
        }
    }

    /// <summary>Proves endpoint ranges select their endpoint and never wrap.</summary>
    [TestMethod]
    public void ApplyWhenRangeMovesPastEitherEndKeepsInclusiveEndpoint()
    {
        PaneState first = CreateState(1);
        PaneState last = PaneReducer.Apply(first, UserIntent.FocusLast);

        foreach (UserIntent intent in new[] { UserIntent.SelectRangePrevious, UserIntent.SelectRangeHalfPageUp, UserIntent.SelectRangeFirst })
        {
            PaneState ranged = PaneReducer.Apply(first, intent);
            Assert.AreSame(first.FocusItem, ranged.FocusItem);
            Assert.AreSame(first.FocusItem, ranged.SelectionAnchor);
            CollectionAssert.AreEqual(new[] { first.FocusItem }, ranged.Selection.ToArray());
        }
        foreach (UserIntent intent in new[] { UserIntent.SelectRangeNext, UserIntent.SelectRangeHalfPageDown, UserIntent.SelectRangeLast })
        {
            PaneState ranged = PaneReducer.Apply(last, intent);
            Assert.AreSame(last.FocusItem, ranged.FocusItem);
            Assert.AreSame(last.FocusItem, ranged.SelectionAnchor);
            CollectionAssert.AreEqual(new[] { last.FocusItem }, ranged.Selection.ToArray());
        }
    }

    /// <summary>Proves a capacity of one still moves one row in each direction.</summary>
    [TestMethod]
    public void ApplyWhenHalfPageCapacityIsOneRangesByOneRow()
    {
        PaneState initial = PaneReducer.Apply(CreateState(1), UserIntent.MoveNext);

        PaneState down = PaneReducer.Apply(initial, UserIntent.SelectRangeHalfPageDown);
        PaneState up = PaneReducer.Apply(initial, UserIntent.SelectRangeHalfPageUp);

        Assert.AreSame(initial.VisibleEntries[2].Path, down.FocusItem);
        Assert.AreSame(initial.VisibleEntries[0].Path, up.FocusItem);
        Assert.HasCount(2, down.Selection);
        Assert.HasCount(2, up.Selection);
    }

    /// <summary>Proves ordinary movement retains the same keyboard anchor.</summary>
    [TestMethod]
    public void ApplyWhenOrdinaryMovementFollowsRangeRetainsAnchorForNextRange()
    {
        PaneState initial = PaneReducer.Apply(CreateState(4), UserIntent.MoveNext);
        PaneState ranged = PaneReducer.Apply(initial, UserIntent.SelectRangeNext);
        PaneState moved = PaneReducer.Apply(PaneReducer.Apply(ranged, UserIntent.MovePrevious), UserIntent.MovePrevious);

        PaneState next = PaneReducer.Apply(moved, UserIntent.SelectRangeNext);

        Assert.AreSame(initial.FocusItem, next.SelectionAnchor);
        Assert.AreSame(initial.FocusItem, next.FocusItem);
        CollectionAssert.AreEqual(new[] { initial.FocusItem }, next.Selection.ToArray());
        Assert.HasCount(2, ranged.Selection);
    }

    /// <summary>Proves Space establishes the keyboard range anchor.</summary>
    [TestMethod]
    public void ApplyWhenSpaceEstablishedAnchorRangesFromSameOwnedPath()
    {
        PaneState initial = CreateState(4);
        PaneState spaced = PaneReducer.Apply(initial, UserIntent.ToggleSelection);

        PaneState moved = PaneReducer.Apply(spaced, UserIntent.MoveNext);
        PaneState ranged = PaneReducer.Apply(moved, UserIntent.SelectRangeNext);

        Assert.AreSame(initial.FocusItem, ranged.SelectionAnchor);
        CollectionAssert.AreEqual(initial.VisibleEntries.Take(3).Select(entry => entry.Path).ToArray(), ranged.Selection.ToArray());
    }

    private static PaneState CreateState(int capacity)
    {
        DirectoryListing listing = PaneSessionTestContext.Listing("C:\\selection",
            ("a", DirectoryEntryKind.File), ("b", DirectoryEntryKind.File), ("c", DirectoryEntryKind.File),
            ("d", DirectoryEntryKind.File), ("e", DirectoryEntryKind.File));
        return PaneReducer.Navigate(listing, PaneSessionTestContext.Capacity(capacity), null, HiddenItemVisibility.Hidden);
    }
}
