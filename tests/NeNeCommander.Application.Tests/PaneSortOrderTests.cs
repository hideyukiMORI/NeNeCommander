using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves the pane sort order value and its transitions through the sole pane reducer.</summary>
[TestClass]
public sealed class PaneSortOrderTests
{
    /// <summary>Proves every pane starts with the name order ascending.</summary>
    [TestMethod]
    public void DefaultWhenReadIsNameAscending()
    {
        PaneSortOrder order = PaneSortOrder.Default;

        Assert.AreSame(SortKey.Name, order.Key);
        Assert.AreSame(SortDirection.Ascending, order.Direction);
    }

    /// <summary>Proves the same key reverses the direction and a second request restores it.</summary>
    [TestMethod]
    public void ToggleWhenKeyIsUnchangedReversesDirection()
    {
        PaneSortOrder descending = PaneSortOrder.Default.Toggle(SortKey.Name);
        PaneSortOrder ascending = descending.Toggle(SortKey.Name);

        Assert.AreSame(SortKey.Name, descending.Key);
        Assert.AreSame(SortDirection.Descending, descending.Direction);
        Assert.AreEqual(PaneSortOrder.Default, ascending);
    }

    /// <summary>Proves another key starts ascending even from a descending order.</summary>
    [TestMethod]
    public void ToggleWhenKeyChangesStartsAscending()
    {
        PaneSortOrder nameDescending = PaneSortOrder.Default.Toggle(SortKey.Name);

        PaneSortOrder extension = nameDescending.Toggle(SortKey.Extension);
        PaneSortOrder extensionDescending = extension.Toggle(SortKey.Extension);
        PaneSortOrder name = extensionDescending.Toggle(SortKey.Name);

        Assert.AreSame(SortKey.Extension, extension.Key);
        Assert.AreSame(SortDirection.Ascending, extension.Direction);
        Assert.AreSame(SortDirection.Descending, extensionDescending.Direction);
        Assert.AreEqual(PaneSortOrder.Default, name);
    }

    /// <summary>Proves each direction names its opposite.</summary>
    [TestMethod]
    public void ReversedWhenReadNamesOppositeDirection()
    {
        Assert.AreSame(SortDirection.Descending, SortDirection.Ascending.Reversed);
        Assert.AreSame(SortDirection.Ascending, SortDirection.Descending.Reversed);
    }

    /// <summary>Proves a new state shows its entries in the default order whatever their input order.</summary>
    [TestMethod]
    public void CreateWhenEntriesAreUnorderedProjectsDefaultOrder()
    {
        PaneState state = CreateState([FileNamed("b.txt"), FileNamed("c.md"), FileNamed("a.zip")]);

        Assert.AreEqual(PaneSortOrder.Default, state.SortOrder);
        AssertVisible(["a.zip", "b.txt", "c.md"], state);
        Assert.AreEqual("a.zip", NameOf(state.FocusItem));
    }

    /// <summary>Proves sorting by extension reorders the visible set and keeps focus and selection.</summary>
    [TestMethod]
    public void ApplyWhenSortingByExtensionKeepsFocusAndSelectionIdentities()
    {
        PaneState state = CreateState([FileNamed("a.zip"), FileNamed("b.txt"), FileNamed("c.md")]);
        state = PaneReducer.Apply(state, UserIntent.ToggleSelection);
        state = PaneReducer.Apply(state, UserIntent.MoveNext);
        FileSystemPath? focus = state.FocusItem;
        IReadOnlyList<FileSystemPath> selection = state.Selection;

        PaneState sorted = PaneReducer.Apply(state, UserIntent.SortByExtension);

        AssertVisible(["c.md", "b.txt", "a.zip"], sorted);
        Assert.AreSame(SortKey.Extension, sorted.SortOrder.Key);
        Assert.AreSame(SortDirection.Ascending, sorted.SortOrder.Direction);
        Assert.AreSame(focus, sorted.FocusItem);
        CollectionAssert.AreEqual(selection.ToArray(), sorted.Selection.ToArray());
        Assert.AreEqual("b.txt", NameOf(sorted.FocusItem));
        Assert.AreEqual("a.zip", NameOf(sorted.Selection[0]));
    }

    /// <summary>Proves sorting by name again reverses the order with focus kept.</summary>
    [TestMethod]
    public void ApplyWhenSortingByNameAgainReversesDirection()
    {
        PaneState state = CreateState([FileNamed("a"), FileNamed("b"), FileNamed("c")]);

        PaneState descending = PaneReducer.Apply(state, UserIntent.SortByName);
        PaneState ascending = PaneReducer.Apply(descending, UserIntent.SortByName);

        AssertVisible(["c", "b", "a"], descending);
        Assert.AreSame(SortDirection.Descending, descending.SortOrder.Direction);
        Assert.AreEqual("a", NameOf(descending.FocusItem));
        AssertVisible(["a", "b", "c"], ascending);
        Assert.AreEqual(PaneSortOrder.Default, ascending.SortOrder);
    }

    /// <summary>Proves movement, paging, first, and last address the projected order.</summary>
    [TestMethod]
    public void ApplyWhenSortedMovesOverProjectedOrder()
    {
        PaneState state = CreateState(
            [FileNamed("a.zip"), FileNamed("b.txt"), FileNamed("c.md"), FileNamed("d")]);
        PaneState sorted = PaneReducer.Apply(state, UserIntent.SortByExtension);

        PaneState first = PaneReducer.Apply(sorted, UserIntent.FocusFirst);
        PaneState next = PaneReducer.Apply(first, UserIntent.MoveNext);
        PaneState paged = PaneReducer.Apply(first, UserIntent.MoveHalfPageDown);
        PaneState last = PaneReducer.Apply(next, UserIntent.FocusLast);

        Assert.AreEqual("d", NameOf(first.FocusItem));
        Assert.AreEqual("c.md", NameOf(next.FocusItem));
        Assert.AreEqual("b.txt", NameOf(paged.FocusItem));
        Assert.AreEqual("a.zip", NameOf(last.FocusItem));
    }

    /// <summary>Proves a focus item that becomes hidden moves to the next visible entry in projected order.</summary>
    [TestMethod]
    public void ApplyHiddenItemVisibilityWhenSortedRecoversFocusInProjectedOrder()
    {
        PaneState shown = CreateState(
            [FileNamed("a.txt"), HiddenFileNamed("b.md"), FileNamed("c.zip"), FileNamed("d.aa")],
            HiddenItemVisibility.Shown);
        PaneState sorted = PaneReducer.Apply(shown, UserIntent.SortByExtension);
        PaneState onHidden = PaneReducer.Apply(
            PaneReducer.Apply(sorted, UserIntent.FocusFirst),
            UserIntent.MoveNext);

        PaneState omitted = PaneReducer.ApplyHiddenItemVisibility(onHidden, HiddenItemVisibility.Hidden);

        AssertVisible(["d.aa", "b.md", "a.txt", "c.zip"], sorted);
        Assert.AreEqual("b.md", NameOf(onHidden.FocusItem));
        Assert.AreEqual("a.txt", NameOf(omitted.FocusItem));
        Assert.AreEqual(sorted.SortOrder, omitted.SortOrder);
    }

    /// <summary>Proves a sort order applied after a read keeps the read's preferred focus.</summary>
    [TestMethod]
    public void ApplySortOrderWhenTargetIsVisibleFocusesTarget()
    {
        PaneState state = CreateState([FileNamed("a.zip"), FileNamed("b.txt"), FileNamed("c.md")]);
        FileSystemPath target = state.VisibleEntries[1].Path;

        PaneState sorted = PaneReducer.ApplySortOrder(state, ExtensionAscending(), target);

        Assert.AreSame(target, sorted.FocusItem);
        AssertVisible(["c.md", "b.txt", "a.zip"], sorted);
    }

    /// <summary>Proves an absent target focuses the first entry of the projected order.</summary>
    [TestMethod]
    public void ApplySortOrderWhenTargetIsAbsentFocusesFirstProjectedEntry()
    {
        PaneState state = CreateState([FileNamed("a.zip"), FileNamed("b.txt"), FileNamed("c.md")]);

        PaneState sorted = PaneReducer.ApplySortOrder(state, ExtensionAscending(), null);
        PaneState missing = PaneReducer.ApplySortOrder(state, ExtensionAscending(), ParsePath("C:\\other"));

        Assert.AreEqual("c.md", NameOf(sorted.FocusItem));
        Assert.AreEqual("c.md", NameOf(missing.FocusItem));
    }

    /// <summary>Proves a hidden target focuses the nearest visible entry after it in projected order.</summary>
    [TestMethod]
    public void ApplySortOrderWhenTargetIsHiddenFocusesNearestVisibleEntry()
    {
        DirectoryEntry hidden = HiddenFileNamed("b.md");
        PaneState state = CreateState([FileNamed("a.txt"), hidden, FileNamed("c.zip"), FileNamed("d.aa")]);

        PaneState sorted = PaneReducer.ApplySortOrder(state, ExtensionAscending(), hidden.Path);
        PaneState descending = PaneReducer.ApplySortOrder(state, ExtensionAscending().Toggle(SortKey.Extension), hidden.Path);

        Assert.AreEqual("a.txt", NameOf(sorted.FocusItem));
        Assert.AreEqual("d.aa", NameOf(descending.FocusItem));
    }

    /// <summary>Proves an empty pane accepts a sort intent and stays focusless.</summary>
    [TestMethod]
    public void ApplyWhenPaneIsEmptyChangesOrderWithoutFocus()
    {
        PaneState state = CreateState([]);

        PaneState sorted = PaneReducer.Apply(state, UserIntent.SortByExtension);

        Assert.IsNull(sorted.FocusItem);
        Assert.IsEmpty(sorted.VisibleEntries);
        Assert.AreSame(SortKey.Extension, sorted.SortOrder.Key);
    }

    /// <summary>Proves a read keeps the order it was given through history commitment.</summary>
    [TestMethod]
    public void CommitNavigationWhenOrderIsCarriedKeepsOrderAndHistory()
    {
        PaneState previous = CreateState([FileNamed("a")]);
        PaneState navigated = PaneReducer.ApplySortOrder(
            PaneReducer.Navigate(Listing("C:\\next", "b.txt", "a.md"), Capacity(), null, HiddenItemVisibility.Hidden),
            ExtensionAscending(),
            null);

        PaneState committed = PaneReducer.CommitNavigation(previous, navigated, PaneNavigationAction.Append);

        Assert.AreEqual(ExtensionAscending(), committed.SortOrder);
        AssertVisible(["a.md", "b.txt"], committed);
        Assert.HasCount(2, committed.NavigationHistory.Locations);
    }

    /// <summary>Proves an absent state or order is rejected by its own name before any projection.</summary>
    [TestMethod]
    public void ApplySortOrderWhenArgumentIsNullNamesTheParameter()
    {
        PaneState state = CreateState([FileNamed("a")]);

        ArgumentNullException absentState = Assert.ThrowsExactly<ArgumentNullException>(
            () => PaneReducer.ApplySortOrder(null!, PaneSortOrder.Default, null));
        ArgumentNullException absentOrder = Assert.ThrowsExactly<ArgumentNullException>(
            () => PaneReducer.ApplySortOrder(state, null!, null));

        Assert.AreEqual("state", absentState.ParamName);
        Assert.AreEqual("sortOrder", absentOrder.ParamName);
    }

    private static PaneSortOrder ExtensionAscending()
    {
        return PaneSortOrder.Default.Toggle(SortKey.Extension);
    }

    private static void AssertVisible(string[] expected, PaneState state)
    {
        CollectionAssert.AreEqual(expected, state.VisibleEntries.Select(entry => entry.Name).ToArray());
    }

    private static string NameOf(FileSystemPath? path)
    {
        Assert.IsNotNull(path);
        return path.CanonicalText[(path.CanonicalText.LastIndexOf('\\') + 1)..];
    }

    private static PaneState CreateState(IReadOnlyList<DirectoryEntry> entries)
    {
        return CreateState(entries, HiddenItemVisibility.Hidden);
    }

    private static PaneState CreateState(IReadOnlyList<DirectoryEntry> entries, HiddenItemVisibility visibility)
    {
        PaneStateCreation outcome = PaneState.Create(ParsePath("C:\\"), entries, Capacity(), visibility);
        return Assert.IsInstanceOfType<PaneStateAccepted>(outcome).State;
    }

    private static DirectoryListing Listing(string location, params string[] names)
    {
        FileSystemPath parsed = ParsePath(location);
        DirectoryEntry[] entries = [.. names.Select(name => DirectoryEntry.Create(
            ParsePath(parsed.CanonicalText + "\\" + name),
            name,
            DirectoryEntryKind.File,
            EntryVisibility.Normal))];
        DirectoryListingCreation creation = DirectoryListing.Create(
            parsed,
            entries,
            DirectoryListingCompleteness.Complete,
            0);
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(creation).Listing;
    }

    private static DirectoryEntry FileNamed(string name)
    {
        return DirectoryEntry.Create(ParsePath("C:\\" + name), name, DirectoryEntryKind.File, EntryVisibility.Normal);
    }

    private static DirectoryEntry HiddenFileNamed(string name)
    {
        return DirectoryEntry.Create(ParsePath("C:\\" + name), name, DirectoryEntryKind.File, EntryVisibility.Hidden);
    }

    private static VisiblePageCapacity Capacity()
    {
        return Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(VisiblePageCapacity.Create(4)).Capacity;
    }

    private static FileSystemPath ParsePath(string input)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input)).Path;
    }
}
