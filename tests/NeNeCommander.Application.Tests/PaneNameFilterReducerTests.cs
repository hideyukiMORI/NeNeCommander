using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves name filtering controls the same visible rows, owned focus and operation selection.</summary>
[TestClass]
public sealed class PaneNameFilterReducerTests
{
    /// <summary>Proves removed marks and anchors never return when the query is cleared.</summary>
    [TestMethod]
    public void ApplyNameFilterWhenSelectedEntriesDisappearDropsMarksAndAnchorPermanently()
    {
        PaneState initial = CreateState("a.txt", "b.log", "c.txt");
        PaneState selected = PaneReducer.Apply(PaneReducer.Apply(initial, UserIntent.MoveNext), UserIntent.ToggleSelection);
        PaneState all = PaneReducer.Apply(selected, UserIntent.SelectRangeLast);

        PaneState filtered = Filter(all, ".txt");
        PaneState cleared = Filter(filtered, string.Empty);

        PaneSessionTestContext.AssertVisibleNames(["a.txt", "c.txt"], filtered);
        Assert.AreSame(initial.VisibleEntries[2].Path, filtered.FocusItem);
        Assert.AreSame(initial.VisibleEntries[2].Path, filtered.Selection.Single());
        Assert.IsNull(filtered.SelectionAnchor);
        Assert.IsNull(cleared.SelectionAnchor);
        CollectionAssert.AreEqual(filtered.Selection.ToArray(), cleared.Selection.ToArray());
        Assert.HasCount(2, all.Selection);
        Assert.AreSame(initial.VisibleEntries[1].Path, all.SelectionAnchor);
    }

    /// <summary>Proves forward, backward and empty recovery use only visible owned paths.</summary>
    [TestMethod]
    public void ApplyNameFilterWhenFocusDisappearsRecoversForwardThenBackwardThenNothing()
    {
        PaneState initial = CreateState("a.txt", "b.log", "c.txt");
        PaneState middle = PaneReducer.Apply(initial, UserIntent.MoveNext);

        PaneState forward = Filter(middle, ".txt");
        PaneState backward = Filter(middle, "a.txt");
        PaneState empty = Filter(middle, "missing");

        Assert.AreSame(initial.VisibleEntries[2].Path, forward.FocusItem);
        Assert.AreSame(initial.VisibleEntries[0].Path, backward.FocusItem);
        Assert.IsNull(empty.FocusItem);
        Assert.IsEmpty(empty.VisibleEntries);
        Assert.IsEmpty(empty.Selection);
        Assert.IsNull(empty.SelectionAnchor);
        Assert.AreSame(initial.VisibleEntries[0].Path, Filter(empty, string.Empty).FocusItem);
    }

    /// <summary>Proves sort/hidden changes preserve one query and never expose an ineligible mark.</summary>
    [TestMethod]
    public void ApplyNameFilterWhenVisibilityAndSortChangeUsesSingleCombinedProjection()
    {
        DirectoryListing listing = PaneSessionTestContext.HiddenListing(@"C:\filter",
            ("a.txt", EntryVisibility.Normal), ("b.log", EntryVisibility.Normal),
            ("c.txt", EntryVisibility.Hidden));
        PaneState initial = PaneReducer.Navigate(listing, PaneSessionTestContext.Capacity(4), null, HiddenItemVisibility.Hidden);

        PaneState filtered = Filter(initial, ".txt");
        PaneState shown = PaneReducer.Apply(filtered, UserIntent.ToggleHiddenItems);
        PaneState descending = PaneReducer.Apply(shown, UserIntent.SortByName);
        PaneState all = PaneReducer.Apply(descending, UserIntent.SelectAll);
        PaneState hidden = PaneReducer.Apply(all, UserIntent.ToggleHiddenItems);

        PaneSessionTestContext.AssertVisibleNames(["a.txt"], filtered);
        PaneSessionTestContext.AssertVisibleNames(["c.txt", "a.txt"], descending);
        Assert.HasCount(2, all.Selection);
        Assert.AreSame(listing.Entries[0].Path, hidden.Selection.Single());
        Assert.AreEqual(".txt", hidden.NameFilter.Text);
    }

    /// <summary>Proves ranges, all and invert address only matches under provider-specific identity.</summary>
    [TestMethod]
    public void ApplyNameFilterWhenWslNamesDifferByCaseKeepsBothOwnedIdentities()
    {
        DirectoryListing listing = PaneSessionTestContext.Listing(@"\\wsl.localhost\Ubuntu\home\filter",
            ("A.txt", DirectoryEntryKind.File), ("a.txt", DirectoryEntryKind.File), ("b.log", DirectoryEntryKind.File));
        PaneState initial = PaneReducer.Navigate(listing, PaneSessionTestContext.Capacity(4), null, HiddenItemVisibility.Hidden);
        PaneState filtered = Filter(initial, "A.TXT");

        PaneState ranged = PaneReducer.Apply(filtered, UserIntent.SelectRangeLast);
        PaneState inverted = PaneReducer.Apply(ranged, UserIntent.InvertSelection);
        PaneState all = PaneReducer.Apply(inverted, UserIntent.SelectAll);

        Assert.HasCount(2, ranged.Selection);
        Assert.IsEmpty(inverted.Selection);
        Assert.HasCount(2, all.Selection);
        Assert.AreSame(listing.Entries[0].Path, all.Selection[0]);
        Assert.AreSame(listing.Entries[1].Path, all.Selection[1]);
    }

    /// <summary>Proves idle Escape clears marks/anchor before query, while explicit clear keeps query.</summary>
    [TestMethod]
    public void ApplyWhenEscapeFollowsFilteredSelectionClearsMarksBeforeFilter()
    {
        PaneState filtered = Filter(CreateState("a.txt", "b.log"), ".txt");
        PaneState selected = PaneReducer.Apply(filtered, UserIntent.ToggleSelection);

        PaneState first = PaneReducer.Apply(selected, UserIntent.Escape);
        PaneState second = PaneReducer.Apply(first, UserIntent.Escape);
        PaneState explicitClear = PaneReducer.Apply(first, UserIntent.ClearSelection);

        Assert.IsEmpty(first.Selection);
        Assert.IsNull(first.SelectionAnchor);
        Assert.AreEqual(".txt", first.NameFilter.Text);
        Assert.IsTrue(second.NameFilter.IsEmpty);
        Assert.HasCount(2, second.VisibleEntries);
        Assert.AreEqual(".txt", explicitClear.NameFilter.Text);
    }

    /// <summary>Proves unchanged committed text does not supersede exact-owner edit captures.</summary>
    [TestMethod]
    public void ApplyNameFilterWhenExactTextRepeatsReturnsSameStateAndRetainsVisibleAnchor()
    {
        PaneState selected = PaneReducer.Apply(CreateState("a.txt", "b.log"), UserIntent.ToggleSelection);
        PaneState filtered = Filter(selected, ".txt");

        PaneState same = Filter(filtered, ".txt");
        PaneState retitled = Filter(filtered, " .txt ");

        Assert.AreSame(filtered, same);
        Assert.AreSame(selected.SelectionAnchor, filtered.SelectionAnchor);
        Assert.AreEqual(" .txt ", retitled.NameFilter.Text);
        Assert.AreSame(filtered.FocusItem, retitled.FocusItem);
        Assert.AreEqual(".txt", filtered.NameFilter.Text);
    }

    private static PaneState Filter(PaneState state, string text)
    {
        return PaneReducer.ApplyNameFilter(state, PaneNameFilterQuery.Parse(text));
    }

    private static PaneState CreateState(params string[] names)
    {
        DirectoryListing listing = PaneSessionTestContext.Listing(@"C:\filter",
            [.. names.Select(name => (name, DirectoryEntryKind.File))]);
        return PaneReducer.Navigate(listing, PaneSessionTestContext.Capacity(4), null, HiddenItemVisibility.Hidden);
    }
}
