using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves bulk selection uses only current visible owned paths and preserves focus.</summary>
[TestClass]
public sealed class PaneBulkSelectionTests
{
    /// <summary>Proves all and inversion exclude hidden entries, reset anchors and preserve old state.</summary>
    [TestMethod]
    public void ApplyWhenBulkSelectionUsesVisibleEntriesKeepsFocusAndImmutablePredecessor()
    {
        DirectoryListing listing = PaneSessionTestContext.HiddenListing("C:\\selection",
            ("a", EntryVisibility.Normal), ("b", EntryVisibility.Hidden), ("c", EntryVisibility.Normal));
        PaneState initial = PaneReducer.Navigate(listing, PaneSessionTestContext.Capacity(4), null, HiddenItemVisibility.Hidden);
        PaneState selected = PaneReducer.Apply(PaneReducer.Apply(initial, UserIntent.ToggleSelection), UserIntent.MoveNext);

        PaneState all = PaneReducer.Apply(selected, UserIntent.SelectAll);
        PaneState inverted = PaneReducer.Apply(selected, UserIntent.InvertSelection);
        PaneState restored = PaneReducer.Apply(inverted, UserIntent.InvertSelection);

        CollectionAssert.AreEqual(new[] { listing.Entries[0].Path, listing.Entries[2].Path }, all.Selection.ToArray());
        CollectionAssert.AreEqual(new[] { listing.Entries[2].Path }, inverted.Selection.ToArray());
        CollectionAssert.AreEqual(selected.Selection.ToArray(), restored.Selection.ToArray());
        Assert.AreSame(listing.Entries[2].Path, all.FocusItem);
        Assert.AreSame(selected.FocusItem, inverted.FocusItem);
        Assert.IsNull(all.SelectionAnchor);
        Assert.IsNull(inverted.SelectionAnchor);
        Assert.AreSame(listing.Entries[0].Path, selected.SelectionAnchor);
        Assert.HasCount(1, selected.Selection);
    }

    /// <summary>Proves all/invert obey empty/full algebra and all is idempotent.</summary>
    [TestMethod]
    public void ApplyWhenSelectionIsEmptyOrFullObeysBulkAlgebra()
    {
        PaneState initial = CreateState("C:\\selection", "a", "b");

        PaneState all = PaneReducer.Apply(initial, UserIntent.SelectAll);
        PaneState allAgain = PaneReducer.Apply(all, UserIntent.SelectAll);
        PaneState invertedEmpty = PaneReducer.Apply(initial, UserIntent.InvertSelection);
        PaneState invertedFull = PaneReducer.Apply(all, UserIntent.InvertSelection);

        CollectionAssert.AreEqual(initial.VisibleEntries.Select(entry => entry.Path).ToArray(), all.Selection.ToArray());
        CollectionAssert.AreEqual(all.Selection.ToArray(), allAgain.Selection.ToArray());
        CollectionAssert.AreEqual(all.Selection.ToArray(), invertedEmpty.Selection.ToArray());
        Assert.IsEmpty(invertedFull.Selection);
        Assert.AreSame(initial.FocusItem, invertedFull.FocusItem);
    }

    /// <summary>Proves explicit clear has the same idle selection and anchor effect as Escape.</summary>
    [TestMethod]
    public void ApplyWhenExplicitClearFollowsSelectionClearsAnchorAndPreservesFocus()
    {
        PaneState selected = PaneReducer.Apply(CreateState("C:\\selection", "a", "b"), UserIntent.ToggleSelection);

        PaneState cleared = PaneReducer.Apply(selected, UserIntent.ClearSelection);
        PaneState escaped = PaneReducer.Apply(selected, UserIntent.Escape);

        Assert.IsEmpty(cleared.Selection);
        Assert.IsNull(cleared.SelectionAnchor);
        Assert.AreSame(selected.FocusItem, cleared.FocusItem);
        Assert.AreSame(escaped.FocusItem, cleared.FocusItem);
        Assert.IsNull(escaped.SelectionAnchor);
        Assert.HasCount(1, selected.Selection);
        Assert.AreSame(selected.FocusItem, selected.SelectionAnchor);
    }

    /// <summary>Proves bulk operations distinguish case-sensitive WSL names.</summary>
    [TestMethod]
    public void ApplyWhenWslNamesDifferOnlyByCaseInvertsEachOwnedIdentitySeparately()
    {
        PaneState initial = CreateState("\\\\wsl.localhost\\Ubuntu\\home\\selection", "A", "a");
        PaneState selected = PaneReducer.Apply(initial, UserIntent.ToggleSelection);

        PaneState inverted = PaneReducer.Apply(selected, UserIntent.InvertSelection);
        PaneState all = PaneReducer.Apply(inverted, UserIntent.SelectAll);

        Assert.HasCount(1, inverted.Selection);
        Assert.AreSame(initial.VisibleEntries[1].Path, inverted.Selection[0]);
        Assert.HasCount(2, all.Selection);
        Assert.AreSame(initial.VisibleEntries[0].Path, all.Selection[0]);
        Assert.AreSame(initial.VisibleEntries[1].Path, all.Selection[1]);
    }

    /// <summary>Proves empty panes remain empty and single entries range to themselves.</summary>
    [TestMethod]
    public void ApplyWhenVisibleSetIsEmptyOrSingleKeepsValidFocusAndSelection()
    {
        PaneState empty = CreateState("C:\\selection");
        PaneState single = CreateState("C:\\selection", "one");
        UserIntent[] ranges = [UserIntent.SelectRangeNext, UserIntent.SelectRangePrevious,
            UserIntent.SelectRangeHalfPageDown, UserIntent.SelectRangeHalfPageUp,
            UserIntent.SelectRangeFirst, UserIntent.SelectRangeLast];

        foreach (UserIntent intent in ranges.Concat([UserIntent.SelectAll, UserIntent.InvertSelection, UserIntent.ClearSelection]))
        {
            PaneState unchanged = PaneReducer.Apply(empty, intent);
            Assert.IsNull(unchanged.FocusItem);
            Assert.IsEmpty(unchanged.Selection);
            Assert.IsNull(unchanged.SelectionAnchor);
        }
        foreach (UserIntent intent in ranges)
        {
            PaneState ranged = PaneReducer.Apply(single, intent);
            Assert.AreSame(single.FocusItem, ranged.FocusItem);
            Assert.AreSame(single.FocusItem, ranged.SelectionAnchor);
            CollectionAssert.AreEqual(new[] { single.FocusItem }, ranged.Selection.ToArray());
        }
    }

    private static PaneState CreateState(string location, params string[] names)
    {
        DirectoryListing listing = PaneSessionTestContext.Listing(location,
            [.. names.Select(name => (name, DirectoryEntryKind.File))]);
        return PaneReducer.Navigate(listing, PaneSessionTestContext.Capacity(4), null, HiddenItemVisibility.Hidden);
    }
}
