using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves visible entries remain owned across public reads and reducer transitions.</summary>
[TestClass]
public sealed class PaneVisibleEntryOwnershipTests
{
    /// <summary>Proves public construction retains entries after caller and SyncRoot mutation attempts.</summary>
    [TestMethod]
    public void CreateWhenExternalCollectionsChangeRetainsVisibleEntries()
    {
        DirectoryListing listing = CreateListing();
        List<DirectoryEntry> input = [.. listing.Entries];
        PaneState original = Assert.IsInstanceOfType<PaneStateAccepted>(PaneState.Create(
            listing.Location, input, PaneSessionTestContext.Capacity(4), HiddenItemVisibility.Hidden)).State;
        PaneState copied = original with { };
        DirectoryEntry[] expected = [.. original.VisibleEntries];

        input.Clear();
        TryClearThroughSyncRoot(original.VisibleEntries);

        CollectionAssert.AreEqual(expected, original.VisibleEntries.ToArray());
        CollectionAssert.AreEqual(expected, copied.VisibleEntries.ToArray());
        Assert.AreSame(expected[0].Path, original.FocusItem);
        Assert.AreSame(expected[0].Path, copied.FocusItem);
    }

    /// <summary>Proves navigation and later selection or movement never expose shared mutable entries.</summary>
    [TestMethod]
    public void NavigateWhenPublishedEntriesAreInspectedPreservesAllDerivedSnapshots()
    {
        PaneState original = CreateState();
        PaneState selected = PaneReducer.Apply(original, UserIntent.ToggleSelection);
        PaneState moved = PaneReducer.Apply(selected, UserIntent.MoveNext);
        DirectoryEntry[] expected = [.. original.VisibleEntries];

        TryClearThroughSyncRoot(moved.VisibleEntries);

        CollectionAssert.AreEqual(expected, original.VisibleEntries.ToArray());
        CollectionAssert.AreEqual(expected, selected.VisibleEntries.ToArray());
        CollectionAssert.AreEqual(expected, moved.VisibleEntries.ToArray());
        Assert.AreSame(original.VisibleEntries, moved.VisibleEntries);
        Assert.AreSame(expected[1].Path, moved.FocusItem);
        Assert.AreSame(expected[0].Path, moved.Selection[0]);
        Assert.AreSame(expected[0].Path, PaneReducer.Apply(moved, UserIntent.MovePrevious).FocusItem);
    }

    /// <summary>Proves a visibility rebuild owns its new sequence without altering the prior pane.</summary>
    [TestMethod]
    public void ApplyWhenHiddenEntriesBecomeVisibleKeepsBothSnapshotsImmutable()
    {
        PaneState original = CreateState();
        PaneState shown = PaneReducer.Apply(original, UserIntent.ToggleHiddenItems);
        DirectoryEntry[] expected = [.. shown.VisibleEntries];

        TryClearThroughSyncRoot(shown.VisibleEntries);

        CollectionAssert.AreEqual(expected, shown.VisibleEntries.ToArray());
        PaneSessionTestContext.AssertVisibleNames(["a.zip", "c.md"], original);
        PaneSessionTestContext.AssertVisibleNames(["a.zip", "b.txt", "c.md"], shown);
        Assert.AreSame(original.FocusItem, shown.FocusItem);
        Assert.AreSame(expected[1].Path, PaneReducer.Apply(shown, UserIntent.MoveNext).FocusItem);
    }

    /// <summary>Proves an ordering rebuild preserves entries, selected identity and the original order.</summary>
    [TestMethod]
    public void ApplyWhenSortChangesKeepsBothSnapshotsImmutable()
    {
        PaneState original = PaneReducer.Apply(CreateState(), UserIntent.ToggleSelection);
        PaneState sorted = PaneReducer.Apply(original, UserIntent.SortByExtension);
        DirectoryEntry[] expected = [.. sorted.VisibleEntries];

        TryClearThroughSyncRoot(sorted.VisibleEntries);

        CollectionAssert.AreEqual(expected, sorted.VisibleEntries.ToArray());
        PaneSessionTestContext.AssertVisibleNames(["a.zip", "c.md"], original);
        PaneSessionTestContext.AssertVisibleNames(["c.md", "a.zip"], sorted);
        Assert.AreSame(original.FocusItem, sorted.FocusItem);
        Assert.AreSame(original.Selection, sorted.Selection);
        Assert.AreSame(expected[0].Path, PaneReducer.Apply(sorted, UserIntent.MovePrevious).FocusItem);
    }

    /// <summary>Proves generic collection mutation is rejected without changing the visible sequence.</summary>
    [TestMethod]
    public void VisibleEntriesWhenGenericMutatorsAreCalledRejectsChanges()
    {
        PaneState state = CreateState();
        ICollection<DirectoryEntry> collection = Assert.IsInstanceOfType<ICollection<DirectoryEntry>>(state.VisibleEntries);
        DirectoryEntry first = state.VisibleEntries[0];

        _ = Assert.ThrowsExactly<NotSupportedException>(() => collection.Add(first));
        _ = Assert.ThrowsExactly<NotSupportedException>(collection.Clear);
        _ = Assert.ThrowsExactly<NotSupportedException>(() => collection.Remove(first));

        Assert.IsTrue(collection.IsReadOnly);
        Assert.HasCount(2, state.VisibleEntries);
        Assert.AreSame(first, state.VisibleEntries[0]);
    }

    /// <summary>Proves count, contains, copying and both enumeration routes retain ordered entries.</summary>
    [TestMethod]
    public void VisibleEntriesWhenReadOrCopiedPreservesOrderAndOwnership()
    {
        PaneState state = CreateState();
        ICollection<DirectoryEntry> collection = Assert.IsInstanceOfType<ICollection<DirectoryEntry>>(state.VisibleEntries);
        DirectoryEntry first = state.VisibleEntries[0];
        DirectoryEntry second = state.VisibleEntries[1];
        DirectoryEntry[] destination = new DirectoryEntry[3];

        collection.CopyTo(destination, 1);
        IEnumerable nonGeneric = state.VisibleEntries;
        List<DirectoryEntry> enumerated = [];
        foreach (object entry in nonGeneric)
        {
            enumerated.Add(Assert.IsInstanceOfType<DirectoryEntry>(entry));
        }
        destination[1] = second;

        Assert.AreEqual(2, collection.Count);
        Assert.IsTrue(collection.Contains(first));
        Assert.IsFalse(collection.Contains(CreateListing().Entries[1]));
        Assert.IsNull(destination[0]);
        Assert.AreSame(second, destination[2]);
        Assert.AreSame(first, state.VisibleEntries[0]);
        CollectionAssert.AreEqual(new[] { first, second }, enumerated);
        CollectionAssert.AreEqual(new[] { first, second }, state.VisibleEntries.ToArray());
    }

    /// <summary>Proves invalid indices keep the previous list boundary contract.</summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(2)]
    public void VisibleEntriesWhenIndexIsOutsideBoundsThrowsArgumentOutOfRangeException(int index)
    {
        PaneState state = CreateState();

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = state.VisibleEntries[index]);
    }

    /// <summary>Proves an empty visible sequence can be enumerated and copied without a focus.</summary>
    [TestMethod]
    public void NavigateWhenListingIsEmptyPublishesAnEmptySnapshot()
    {
        DirectoryListing listing = PaneSessionTestContext.Listing("C:\\visible");
        PaneState state = PaneReducer.Navigate(
            listing, PaneSessionTestContext.Capacity(4), null, HiddenItemVisibility.Hidden);
        ICollection<DirectoryEntry> collection = Assert.IsInstanceOfType<ICollection<DirectoryEntry>>(state.VisibleEntries);

        collection.CopyTo([], 0);

        Assert.IsEmpty(state.VisibleEntries);
        Assert.IsNull(state.FocusItem);
    }

    private static PaneState CreateState()
    {
        return PaneReducer.Navigate(CreateListing(), PaneSessionTestContext.Capacity(4), null, HiddenItemVisibility.Hidden);
    }

    private static DirectoryListing CreateListing()
    {
        return PaneSessionTestContext.HiddenListing("C:\\visible",
            ("a.zip", EntryVisibility.Normal), ("b.txt", EntryVisibility.Hidden), ("c.md", EntryVisibility.Normal));
    }

    private static void TryClearThroughSyncRoot(IReadOnlyList<DirectoryEntry> entries)
    {
        if (entries is ICollection collection &&
            collection.SyncRoot is ICollection<DirectoryEntry> mutable && !mutable.IsReadOnly)
        {
            mutable.Clear();
        }
    }
}
