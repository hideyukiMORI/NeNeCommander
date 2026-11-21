using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves selection ownership, safe reuse, and ordinary collection consumption.</summary>
[TestClass]
public sealed class PaneSelectionOwnershipTests
{
    /// <summary>Proves empty and selected movement reuses owned storage through paging and bounds.</summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(2)]
    public void ApplyWhenMovementKeepsSelectionReusesOwnedSnapshot(int selectedCount)
    {
        PaneState original = CreateState(selectedCount);
        PaneState current = original with { };
        FileSystemPath[] expectedSelection = [.. original.Selection];
        UserIntent[] intents = [UserIntent.MoveNext, UserIntent.MovePrevious,
            UserIntent.MoveHalfPageDown, UserIntent.MoveHalfPageUp, UserIntent.FocusLast,
            UserIntent.MoveNext, UserIntent.FocusFirst, UserIntent.MovePrevious];
        int[] expectedFocus = [1, 0, 2, 0, 2, 2, 0, 0];

        for (int index = 0; index < intents.Length; index++)
        {
            current = PaneReducer.Apply(current, intents[index]);

            Assert.AreSame(original.Selection, current.Selection);
            Assert.AreSame(original.VisibleEntries, current.VisibleEntries);
            Assert.AreSame(original.VisibleEntries[expectedFocus[index]].Path, current.FocusItem);
            CollectionAssert.AreEqual(expectedSelection, current.Selection.ToArray());
            CollectionAssert.AreEqual(expectedSelection, original.Selection.ToArray());
            Assert.AreSame(original.VisibleEntries[0].Path, original.FocusItem);
        }
    }

    /// <summary>Proves ordering changes retain the owned selection and actual focus path.</summary>
    [TestMethod]
    public void ApplyWhenSortingReusesOwnedSelection()
    {
        PaneState original = CreateState(2);

        PaneState sorted = PaneReducer.Apply(original, UserIntent.SortByExtension);

        Assert.AreSame(original.Selection, sorted.Selection);
        Assert.AreSame(original.FocusItem, sorted.FocusItem);
        Assert.AreNotSame(original.VisibleEntries, sorted.VisibleEntries);
        Assert.AreEqual("c.md", sorted.VisibleEntries[0].Name);
        Assert.HasCount(2, original.Selection);
    }

    /// <summary>Proves equal-content foreign mutable storage is copied.</summary>
    [TestMethod]
    public void TransitionWhenSelectionIsForeignOwnsItsContents()
    {
        PaneState original = CreateState(2);
        List<FileSystemPath> mutable = [.. original.Selection];

        PaneState next = original.Transition(original.FocusItem, mutable);
        mutable.Clear();

        Assert.HasCount(2, original.Selection);
        Assert.HasCount(2, next.Selection);
        Assert.AreNotSame(mutable, next.Selection);
        Assert.AreNotSame(original.Selection, next.Selection);
        Assert.AreSame(original.Selection[0], next.Selection[0]);
        Assert.AreSame(original.Selection[1], next.Selection[1]);
    }

    /// <summary>Proves equal-content foreign readonly wrappers do not transfer their mutable storage.</summary>
    [TestMethod]
    public void TransitionWhenSelectionIsWrappedOwnsItsContents()
    {
        PaneState original = CreateState(2);
        List<FileSystemPath> mutable = [.. original.Selection];
        IReadOnlyList<FileSystemPath> input = mutable.AsReadOnly();

        PaneState next = original.Transition(original.FocusItem, input);
        mutable.Clear();

        Assert.HasCount(2, original.Selection);
        Assert.HasCount(2, next.Selection);
        Assert.AreNotSame(input, next.Selection);
        Assert.AreNotSame(original.Selection, next.Selection);
        Assert.AreSame(original.Selection[0], next.Selection[0]);
        Assert.AreSame(original.Selection[1], next.Selection[1]);
    }

    /// <summary>Proves the former SyncRoot route cannot mutate selected or copied snapshots.</summary>
    [TestMethod]
    public void SelectionWhenSyncRootIsInspectedRetainsOwnedContents()
    {
        PaneState original = CreateState(2);
        PaneState copied = original with { };
        PaneState moved = PaneReducer.Apply(original, UserIntent.MoveNext);

        if (original.Selection is ICollection collection &&
            collection.SyncRoot is ICollection<FileSystemPath> exposed && !exposed.IsReadOnly)
        {
            exposed.Clear();
        }

        Assert.HasCount(2, original.Selection);
        Assert.HasCount(2, copied.Selection);
        Assert.HasCount(2, moved.Selection);
        Assert.AreSame(original.VisibleEntries[0].Path, original.Selection[0]);
        Assert.AreSame(original.VisibleEntries[1].Path, original.Selection[1]);
    }

    /// <summary>Proves an Escape-empty snapshot cannot be populated through the former alias.</summary>
    [TestMethod]
    public void SelectionWhenEscapeClearsItemsCannotBeChangedThroughSyncRoot()
    {
        PaneState original = CreateState(2);
        PaneState escaped = PaneReducer.Apply(original, UserIntent.Escape);

        if (escaped.Selection is ICollection collection &&
            collection.SyncRoot is ICollection<FileSystemPath> exposed && !exposed.IsReadOnly)
        {
            exposed.Add(original.VisibleEntries[2].Path);
        }

        Assert.IsEmpty(escaped.Selection);
        Assert.HasCount(2, original.Selection);
        Assert.AreSame(original.FocusItem, escaped.FocusItem);
    }

    /// <summary>Proves generic consumer fast paths copy values and cannot mutate the snapshot.</summary>
    [TestMethod]
    public void SelectionWhenUsedAsGenericCollectionCopiesAndRejectsMutation()
    {
        PaneState state = CreateState(2);
        ICollection<FileSystemPath> collection = Assert.IsInstanceOfType<ICollection<FileSystemPath>>(state.Selection);
        FileSystemPath last = state.VisibleEntries[2].Path;
        FileSystemPath[] destination = [last, last, last, last];

        collection.CopyTo(destination, 1);
        IEnumerable source = state.Selection;
        IEnumerator untyped = source.GetEnumerator();
        List<FileSystemPath> enumerated = [];
        while (untyped.MoveNext())
        {
            enumerated.Add(Assert.IsInstanceOfType<FileSystemPath>(untyped.Current));
        }

        Assert.IsTrue(collection.IsReadOnly);
        Assert.AreEqual(2, collection.Count);
        Assert.IsTrue(collection.Contains(state.Selection[0]));
        Assert.IsFalse(collection.Contains(last));
        Assert.AreSame(state.Selection[0], destination[1]);
        Assert.AreSame(state.Selection[1], destination[2]);
        CollectionAssert.AreEqual(state.Selection.ToArray(), enumerated.ToArray());
        destination[1] = last;
        Assert.AreSame(state.VisibleEntries[0].Path, state.Selection[0]);
        _ = Assert.ThrowsExactly<NotSupportedException>(() => collection.Add(last));
        _ = Assert.ThrowsExactly<NotSupportedException>(collection.Clear);
        _ = Assert.ThrowsExactly<NotSupportedException>(() => collection.Remove(state.Selection[0]));
        Assert.HasCount(2, state.Selection);
    }

    /// <summary>Proves the collection keeps the existing invalid-index exception contract.</summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(2)]
    public void SelectionWhenIndexIsOutsideBoundsThrowsArgumentOutOfRangeException(int index)
    {
        PaneState state = CreateState(2);

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = state.Selection[index]);
    }

    /// <summary>Proves changed selection detaches from previously shared snapshots.</summary>
    [TestMethod]
    public void ApplyWhenSelectionChangesPreservesOlderSnapshots()
    {
        PaneState original = CreateState(2);
        PaneState moved = PaneReducer.Apply(original, UserIntent.MoveNext);

        PaneState toggled = PaneReducer.Apply(moved, UserIntent.ToggleSelection);
        PaneState escaped = PaneReducer.Apply(moved, UserIntent.Escape);

        Assert.HasCount(2, original.Selection);
        Assert.HasCount(2, moved.Selection);
        Assert.HasCount(1, toggled.Selection);
        Assert.IsEmpty(escaped.Selection);
        Assert.AreSame(original.Selection, moved.Selection);
        Assert.AreNotSame(moved.Selection, toggled.Selection);
        Assert.AreNotSame(moved.Selection, escaped.Selection);
        Assert.AreSame(original.Selection[0], toggled.Selection[0]);
    }

    /// <summary>Proves unchanged payloads may compare equal while movement still creates a new state.</summary>
    [TestMethod]
    public void ApplyWhenFocusReturnsKeepsNewReferencesAndValueEquality()
    {
        PaneState original = CreateState(2);
        PaneState next = PaneReducer.Apply(original, UserIntent.MoveNext);

        PaneState returned = PaneReducer.Apply(next, UserIntent.MovePrevious);
        PaneState clamped = PaneReducer.Apply(original, UserIntent.MovePrevious);

        Assert.AreNotSame(original, returned);
        Assert.AreNotSame(original, clamped);
        Assert.AreEqual(original, returned);
        Assert.AreEqual(original, clamped);
        Assert.AreEqual(original.GetHashCode(), returned.GetHashCode());
        Assert.AreEqual(original.GetHashCode(), clamped.GetHashCode());
    }

    private static PaneState CreateState(int selectedCount)
    {
        DirectoryListing listing = PaneSessionTestContext.Listing("C:\\selection",
            ("a.zip", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File), ("c.md", DirectoryEntryKind.File));
        PaneState state = PaneReducer.Navigate(listing, PaneSessionTestContext.Capacity(4), null, HiddenItemVisibility.Hidden);
        for (int index = 0; index < selectedCount; index++)
        {
            state = PaneReducer.Apply(state, UserIntent.ToggleSelection);
            state = PaneReducer.Apply(state, UserIntent.MoveNext);
        }
        return PaneReducer.Apply(state, UserIntent.FocusFirst);
    }
}
