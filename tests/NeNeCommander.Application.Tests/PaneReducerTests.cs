using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves immutable pane construction and canonical reducer transitions.</summary>
[TestClass]
public sealed class PaneReducerTests
{
    /// <summary>Proves valid input is owned and initially focused.</summary>
    [TestMethod]
    public void CreateWhenEntriesAreValidFocusesFirstItemAndOwnsSnapshot()
    {
        DirectoryEntry first = Entry("one", EntryVisibility.Normal);
        DirectoryEntry second = Entry("two", EntryVisibility.Normal);
        List<DirectoryEntry> inputs = [first, second];

        PaneState state = CreateState(inputs, 4);
        inputs.Clear();

        Assert.AreSame(first.Path, state.FocusItem);
        Assert.HasCount(2, state.VisibleEntries);
        Assert.IsEmpty(state.Selection);
        Assert.AreSame(HiddenItemVisibility.Hidden, state.HiddenItemVisibility);
    }

    /// <summary>Proves an empty pane has no focus.</summary>
    [TestMethod]
    public void CreateWhenEntriesAreEmptyHasNoFocus()
    {
        PaneState state = CreateState([], 1);

        Assert.IsNull(state.FocusItem);
        Assert.IsEmpty(state.VisibleEntries);
    }

    /// <summary>Proves duplicate visible identity is rejected.</summary>
    [TestMethod]
    public void CreateWhenEntriesContainDuplicateDuplicateItemFailure()
    {
        DirectoryEntry item = Entry("Same", EntryVisibility.Normal);
        DirectoryEntry caseVariant = DirectoryEntry.Create(
            ParsePath("c:\\same"),
            "same",
            DirectoryEntryKind.File,
            EntryVisibility.Normal);

        PaneStateCreation outcome = PaneState.Create(
            ParsePath("C:\\"),
            [item, caseVariant],
            CreateCapacity(2),
            HiddenItemVisibility.Hidden);

        PaneStateRejected rejected = Assert.IsInstanceOfType<PaneStateRejected>(outcome);
        Assert.AreSame(PaneStateFailureKind.DuplicateItem, rejected.Kind);
    }

    /// <summary>Proves a null entry is rejected.</summary>
    [TestMethod]
    public void CreateWhenEntriesContainNullNullItemFailure()
    {
        DirectoryEntry[] entries = new DirectoryEntry[1];

        PaneStateCreation outcome = PaneState.Create(
            ParsePath("C:\\"),
            entries,
            CreateCapacity(1),
            HiddenItemVisibility.Hidden);

        PaneStateRejected rejected = Assert.IsInstanceOfType<PaneStateRejected>(outcome);
        Assert.AreSame(PaneStateFailureKind.NullItem, rejected.Kind);
    }

    /// <summary>Proves the omitting visibility keeps hidden entries out of the visible set.</summary>
    [TestMethod]
    public void CreateWhenHiddenEntriesAreOmittedExcludesThemFromTheVisibleSet()
    {
        DirectoryEntry hidden = Entry("one", EntryVisibility.Hidden);
        DirectoryEntry normal = Entry("two", EntryVisibility.Normal);

        PaneState state = CreateState([hidden, normal], 4);

        Assert.HasCount(1, state.VisibleEntries);
        Assert.AreSame(normal, state.VisibleEntries[0]);
        Assert.AreSame(normal.Path, state.FocusItem);
    }

    /// <summary>Proves the showing visibility keeps hidden entries inside the visible set.</summary>
    [TestMethod]
    public void CreateWhenHiddenEntriesAreShownIncludesThemInTheVisibleSet()
    {
        DirectoryEntry hidden = Entry("one", EntryVisibility.Hidden);
        DirectoryEntry normal = Entry("two", EntryVisibility.Normal);

        PaneState state = CreateState([hidden, normal], 4, HiddenItemVisibility.Shown);

        Assert.HasCount(2, state.VisibleEntries);
        Assert.AreSame(hidden.Path, state.FocusItem);
        Assert.AreSame(HiddenItemVisibility.Shown, state.HiddenItemVisibility);
    }

    /// <summary>Proves movement clamps focus while preserving explicit selection.</summary>
    [TestMethod]
    public void ApplyWhenMovingAndPagingClampsFocusAndPreservesSelection()
    {
        DirectoryEntry first = Entry("one", EntryVisibility.Normal);
        DirectoryEntry second = Entry("two", EntryVisibility.Normal);
        DirectoryEntry third = Entry("three", EntryVisibility.Normal);
        PaneState state = CreateState([first, second, third], 4);
        state = PaneReducer.Apply(state, UserIntent.ToggleSelection);

        state = PaneReducer.Apply(state, UserIntent.MoveNext);
        Assert.AreSame(second.Path, state.FocusItem);
        Assert.AreSame(first.Path, state.Selection[0]);

        state = PaneReducer.Apply(state, UserIntent.MoveHalfPageDown);
        Assert.AreSame(third.Path, state.FocusItem);

        state = PaneReducer.Apply(state, UserIntent.MovePrevious);
        Assert.AreSame(second.Path, state.FocusItem);
        state = PaneReducer.Apply(state, UserIntent.MoveHalfPageUp);
        Assert.AreSame(first.Path, state.FocusItem);

        state = PaneReducer.Apply(state, UserIntent.FocusLast);
        Assert.AreSame(third.Path, state.FocusItem);
        state = PaneReducer.Apply(state, UserIntent.FocusFirst);
        Assert.AreSame(first.Path, state.FocusItem);
    }

    /// <summary>Proves every movement addresses the visible set alone while entries are omitted.</summary>
    [TestMethod]
    public void ApplyWhenHiddenEntriesAreOmittedMovesOverTheVisibleSetOnly()
    {
        DirectoryEntry first = Entry("one", EntryVisibility.Normal);
        DirectoryEntry skipped = Entry("two", EntryVisibility.Hidden);
        DirectoryEntry last = Entry("three", EntryVisibility.Normal);
        PaneState state = CreateState([first, skipped, last], 4);

        PaneState next = PaneReducer.Apply(state, UserIntent.MoveNext);
        PaneState beyond = PaneReducer.Apply(next, UserIntent.MoveNext);
        PaneState toLast = PaneReducer.Apply(state, UserIntent.FocusLast);

        Assert.AreSame(last.Path, next.FocusItem);
        Assert.AreSame(last.Path, beyond.FocusItem);
        Assert.AreSame(last.Path, toLast.FocusItem);
    }

    /// <summary>Proves toggle and escape selection behavior.</summary>
    [TestMethod]
    public void ApplyWhenTogglingAndEscapingSelectionIsExact()
    {
        DirectoryEntry first = Entry("one", EntryVisibility.Normal);
        PaneState state = CreateState([first], 1);

        state = PaneReducer.Apply(state, UserIntent.ToggleSelection);
        Assert.HasCount(1, state.Selection);
        state = PaneReducer.Apply(state, UserIntent.ToggleSelection);
        Assert.IsEmpty(state.Selection);
        state = PaneReducer.Apply(state, UserIntent.ToggleSelection);
        state = PaneReducer.Apply(state, UserIntent.Escape);
        Assert.IsEmpty(state.Selection);
        Assert.AreSame(first.Path, state.FocusItem);
    }

    /// <summary>Proves irrelevant intents retain snapshot identity.</summary>
    [TestMethod]
    public void ApplyWhenIntentDoesNotChangePaneReturnsSameSnapshot()
    {
        PaneState empty = CreateState([], 1);
        PaneState populated = CreateState([Entry("one", EntryVisibility.Normal)], 1);

        Assert.AreSame(empty, PaneReducer.Apply(empty, UserIntent.MoveNext));
        Assert.AreSame(empty, PaneReducer.Apply(empty, UserIntent.ToggleSelection));
        Assert.AreSame(populated, PaneReducer.Apply(populated, UserIntent.Copy));
    }

    /// <summary>Proves visible page capacity must be positive.</summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void CreateWhenVisiblePageCapacityIsNotPositiveRejected(int value)
    {
        VisiblePageCapacityCreation outcome = VisiblePageCapacity.Create(value);

        _ = Assert.IsInstanceOfType<VisiblePageCapacityRejected>(outcome);
    }

    /// <summary>Proves half-page movement uses half the measured capacity with a minimum of one.</summary>
    [TestMethod]
    public void ApplyWhenHalfPageMovesUsesMeasuredHalfCapacity()
    {
        DirectoryEntry[] entries = [
            Entry("one", EntryVisibility.Normal),
            Entry("two", EntryVisibility.Normal),
            Entry("three", EntryVisibility.Normal),
            Entry("four", EntryVisibility.Normal),
            Entry("five", EntryVisibility.Normal),
            Entry("six", EntryVisibility.Normal),
        ];
        PaneState state = CreateState(entries, 4);

        PaneState moved = PaneReducer.Apply(state, UserIntent.MoveHalfPageDown);

        Assert.AreSame(entries[2].Path, moved.FocusItem);
        Assert.AreEqual(4, moved.VisiblePageCapacity.Value);
    }

    /// <summary>Proves a location change focuses the first entry and clears selection.</summary>
    [TestMethod]
    public void NavigateWhenPreferredFocusIsAbsentFocusesFirstEntryWithEmptySelection()
    {
        DirectoryListing listing = CreateListing("C:\\root", ("b.txt", EntryVisibility.Normal), ("a.txt", EntryVisibility.Normal));

        PaneState state = Navigate(listing, null, HiddenItemVisibility.Hidden);

        Assert.AreSame(listing.Location, state.Location);
        Assert.AreSame(listing.Entries[0].Path, state.FocusItem);
        Assert.AreEqual("a.txt", listing.Entries[0].Name);
        Assert.HasCount(2, state.VisibleEntries);
        Assert.IsEmpty(state.Selection);
        Assert.AreEqual(3, state.VisiblePageCapacity.Value);
    }

    /// <summary>Proves a preferred item is focused by provider identity when present.</summary>
    [TestMethod]
    public void NavigateWhenPreferredFocusIsPresentFocusesItByIdentity()
    {
        DirectoryListing listing = CreateListing("C:\\root", ("a.txt", EntryVisibility.Normal), ("Docs", EntryVisibility.Normal));

        PaneState state = Navigate(listing, ParsePath("c:\\root\\docs"), HiddenItemVisibility.Hidden);

        Assert.AreSame(listing.Entries[1].Path, state.FocusItem);
    }

    /// <summary>Proves a preferred item outside the listing falls back to the first entry.</summary>
    [TestMethod]
    public void NavigateWhenPreferredFocusIsMissingFocusesFirstEntry()
    {
        DirectoryListing listing = CreateListing("C:\\root", ("a.txt", EntryVisibility.Normal));

        PaneState state = Navigate(listing, ParsePath("C:\\root\\missing"), HiddenItemVisibility.Hidden);

        Assert.AreSame(listing.Entries[0].Path, state.FocusItem);
    }

    /// <summary>Proves a preferred item that is omitted focuses the next visible entry.</summary>
    [TestMethod]
    public void NavigateWhenPreferredFocusIsHiddenFocusesNextVisibleEntry()
    {
        DirectoryListing listing = CreateListing(
            "C:\\root",
            ("a.txt", EntryVisibility.Normal),
            ("b.txt", EntryVisibility.Hidden),
            ("c.txt", EntryVisibility.Normal));

        PaneState state = Navigate(listing, listing.Entries[1].Path, HiddenItemVisibility.Hidden);

        Assert.AreSame(listing.Entries[2].Path, state.FocusItem);
        Assert.HasCount(2, state.VisibleEntries);
    }

    /// <summary>Proves an omitted preferred item with no later visible entry falls back to the earlier one.</summary>
    [TestMethod]
    public void NavigateWhenPreferredFocusIsHiddenAndLastFocusesPreviousVisibleEntry()
    {
        DirectoryListing listing = CreateListing(
            "C:\\root",
            ("a.txt", EntryVisibility.Normal),
            ("b.txt", EntryVisibility.Hidden));

        PaneState state = Navigate(listing, listing.Entries[1].Path, HiddenItemVisibility.Hidden);

        Assert.AreSame(listing.Entries[0].Path, state.FocusItem);
    }

    /// <summary>Proves the backward search skips omitted entries until it finds a visible one.</summary>
    [TestMethod]
    public void NavigateWhenPreferredFocusTrailsSeveralHiddenEntriesFocusesTheEarlierVisibleEntry()
    {
        DirectoryListing listing = CreateListing(
            "C:\\root",
            ("a.txt", EntryVisibility.Normal),
            ("b.txt", EntryVisibility.Hidden),
            ("c.txt", EntryVisibility.Hidden));

        PaneState state = Navigate(listing, listing.Entries[2].Path, HiddenItemVisibility.Hidden);

        Assert.AreSame(listing.Entries[0].Path, state.FocusItem);
        Assert.HasCount(1, state.VisibleEntries);
    }

    /// <summary>Proves a location whose entries are all omitted has no focus at all.</summary>
    [TestMethod]
    public void NavigateWhenEveryEntryIsHiddenHasNoFocus()
    {
        DirectoryListing listing = CreateListing(
            "C:\\root",
            ("a.txt", EntryVisibility.Hidden),
            ("b.txt", EntryVisibility.Hidden));

        PaneState state = Navigate(listing, listing.Entries[0].Path, HiddenItemVisibility.Hidden);

        Assert.IsNull(state.FocusItem);
        Assert.IsEmpty(state.VisibleEntries);
    }

    /// <summary>Proves the showing visibility lists every entry of the location.</summary>
    [TestMethod]
    public void NavigateWhenHiddenEntriesAreShownListsThem()
    {
        DirectoryListing listing = CreateListing(
            "C:\\root",
            ("a.txt", EntryVisibility.Normal),
            ("b.txt", EntryVisibility.Hidden));

        PaneState state = Navigate(listing, listing.Entries[1].Path, HiddenItemVisibility.Shown);

        Assert.HasCount(2, state.VisibleEntries);
        Assert.AreSame(listing.Entries[1].Path, state.FocusItem);
    }

    /// <summary>Proves an empty listing has no focus after navigation.</summary>
    [TestMethod]
    public void NavigateWhenListingIsEmptyHasNoFocus()
    {
        DirectoryListing listing = CreateListing("C:\\root");

        PaneState state = Navigate(listing, ParsePath("C:\\root\\missing"), HiddenItemVisibility.Hidden);

        Assert.IsNull(state.FocusItem);
        Assert.IsEmpty(state.VisibleEntries);
    }

    /// <summary>Proves a focus item that stays visible survives a visibility change untouched.</summary>
    [TestMethod]
    public void ApplyHiddenItemVisibilityWhenFocusStaysVisibleKeepsIt()
    {
        DirectoryEntry visible = Entry("one", EntryVisibility.Normal);
        DirectoryEntry hidden = Entry("two", EntryVisibility.Hidden);
        PaneState shown = CreateState([visible, hidden], 4, HiddenItemVisibility.Shown);

        PaneState omitted = PaneReducer.ApplyHiddenItemVisibility(shown, HiddenItemVisibility.Hidden);

        Assert.AreSame(visible.Path, omitted.FocusItem);
        Assert.HasCount(1, omitted.VisibleEntries);
        Assert.AreSame(HiddenItemVisibility.Hidden, omitted.HiddenItemVisibility);
    }

    /// <summary>Proves a focus item that becomes hidden moves to the next visible entry.</summary>
    [TestMethod]
    public void ApplyHiddenItemVisibilityWhenFocusBecomesHiddenFocusesNextVisibleEntry()
    {
        DirectoryEntry first = Entry("one", EntryVisibility.Normal);
        DirectoryEntry focused = Entry("two", EntryVisibility.Hidden);
        DirectoryEntry last = Entry("three", EntryVisibility.Normal);
        PaneState shown = CreateState([first, focused, last], 4, HiddenItemVisibility.Shown);
        PaneState onHidden = PaneReducer.Apply(shown, UserIntent.MoveNext);

        PaneState omitted = PaneReducer.ApplyHiddenItemVisibility(onHidden, HiddenItemVisibility.Hidden);

        Assert.AreSame(focused.Path, onHidden.FocusItem);
        Assert.AreSame(last.Path, omitted.FocusItem);
    }

    /// <summary>Proves a hidden focus item with no later visible entry moves to the earlier one.</summary>
    [TestMethod]
    public void ApplyHiddenItemVisibilityWhenFocusBecomesHiddenAndIsLastFocusesPreviousVisibleEntry()
    {
        DirectoryEntry first = Entry("one", EntryVisibility.Normal);
        DirectoryEntry focused = Entry("two", EntryVisibility.Hidden);
        PaneState shown = CreateState([first, focused], 4, HiddenItemVisibility.Shown);
        PaneState onHidden = PaneReducer.Apply(shown, UserIntent.MoveNext);

        PaneState omitted = PaneReducer.ApplyHiddenItemVisibility(onHidden, HiddenItemVisibility.Hidden);

        Assert.AreSame(first.Path, omitted.FocusItem);
    }

    /// <summary>Proves a pane whose every entry becomes hidden loses focus and selection.</summary>
    [TestMethod]
    public void ApplyHiddenItemVisibilityWhenEveryEntryBecomesHiddenClearsFocusAndSelection()
    {
        DirectoryEntry first = Entry("one", EntryVisibility.Hidden);
        DirectoryEntry second = Entry("two", EntryVisibility.Hidden);
        PaneState shown = CreateState([first, second], 4, HiddenItemVisibility.Shown);
        PaneState selected = PaneReducer.Apply(shown, UserIntent.ToggleSelection);

        PaneState omitted = PaneReducer.ApplyHiddenItemVisibility(selected, HiddenItemVisibility.Hidden);

        Assert.IsNull(omitted.FocusItem);
        Assert.IsEmpty(omitted.VisibleEntries);
        Assert.IsEmpty(omitted.Selection);
    }

    /// <summary>Proves only the selected items that became hidden leave the selection.</summary>
    [TestMethod]
    public void ApplyHiddenItemVisibilityWhenSelectedItemsAreHiddenDropsOnlyThose()
    {
        DirectoryEntry kept = Entry("one", EntryVisibility.Normal);
        DirectoryEntry dropped = Entry("two", EntryVisibility.Hidden);
        PaneState shown = CreateState([kept, dropped], 4, HiddenItemVisibility.Shown);
        PaneState selectedFirst = PaneReducer.Apply(shown, UserIntent.ToggleSelection);
        PaneState moved = PaneReducer.Apply(selectedFirst, UserIntent.MoveNext);
        PaneState selectedBoth = PaneReducer.Apply(moved, UserIntent.ToggleSelection);

        PaneState omitted = PaneReducer.ApplyHiddenItemVisibility(selectedBoth, HiddenItemVisibility.Hidden);

        Assert.HasCount(2, selectedBoth.Selection);
        Assert.HasCount(1, omitted.Selection);
        Assert.AreSame(kept.Path, omitted.Selection[0]);
    }

    /// <summary>Proves showing hidden entries reveals them without moving the focus item.</summary>
    [TestMethod]
    public void ApplyHiddenItemVisibilityWhenHiddenEntriesAreShownRevealsThemAndKeepsFocus()
    {
        DirectoryEntry visible = Entry("one", EntryVisibility.Normal);
        DirectoryEntry revealed = Entry("two", EntryVisibility.Hidden);
        PaneState omitted = CreateState([visible, revealed], 4);

        PaneState shown = PaneReducer.ApplyHiddenItemVisibility(omitted, HiddenItemVisibility.Shown);

        Assert.HasCount(2, shown.VisibleEntries);
        Assert.AreSame(visible.Path, shown.FocusItem);
        Assert.AreSame(HiddenItemVisibility.Shown, shown.HiddenItemVisibility);
    }

    /// <summary>Proves an empty pane stays focusless across a visibility change.</summary>
    [TestMethod]
    public void ApplyHiddenItemVisibilityWhenPaneIsEmptyKeepsNoFocus()
    {
        PaneState empty = CreateState([], 4);

        PaneState shown = PaneReducer.ApplyHiddenItemVisibility(empty, HiddenItemVisibility.Shown);

        Assert.IsNull(shown.FocusItem);
        Assert.IsEmpty(shown.VisibleEntries);
        Assert.AreSame(HiddenItemVisibility.Shown, shown.HiddenItemVisibility);
    }

    private static PaneState Navigate(
        DirectoryListing listing,
        FileSystemPath? preferredFocus,
        HiddenItemVisibility visibility)
    {
        return PaneReducer.Navigate(listing, CreateCapacity(3), preferredFocus, visibility);
    }

    private static DirectoryListing CreateListing(
        string location,
        params (string Name, EntryVisibility Visibility)[] entries)
    {
        FileSystemPath parsedLocation = ParsePath(location);
        DirectoryEntry[] built = new DirectoryEntry[entries.Length];
        for (int index = 0; index < entries.Length; index++)
        {
            built[index] = DirectoryEntry.Create(
                ParsePath(parsedLocation.CanonicalText + "\\" + entries[index].Name),
                entries[index].Name,
                DirectoryEntryKind.File,
                entries[index].Visibility);
        }
        DirectoryListingCreation creation = DirectoryListing.Create(
            parsedLocation,
            built,
            DirectoryListingCompleteness.Complete,
            0);
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(creation).Listing;
    }

    private static DirectoryEntry Entry(string name, EntryVisibility visibility)
    {
        return DirectoryEntry.Create(
            ParsePath("C:\\" + name),
            name,
            DirectoryEntryKind.File,
            visibility);
    }

    private static PaneState CreateState(IReadOnlyList<DirectoryEntry> entries, int capacity)
    {
        return CreateState(entries, capacity, HiddenItemVisibility.Hidden);
    }

    private static PaneState CreateState(
        IReadOnlyList<DirectoryEntry> entries,
        int capacity,
        HiddenItemVisibility visibility)
    {
        PaneStateCreation outcome = PaneState.Create(
            ParsePath("C:\\"),
            entries,
            CreateCapacity(capacity),
            visibility);
        return Assert.IsInstanceOfType<PaneStateAccepted>(outcome).State;
    }

    private static VisiblePageCapacity CreateCapacity(int value)
    {
        VisiblePageCapacityCreation outcome = VisiblePageCapacity.Create(value);
        return Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(outcome).Capacity;
    }

    private static FileSystemPath ParsePath(string input)
    {
        PathParseOutcome outcome = FileSystemPath.Parse(input);
        return Assert.IsInstanceOfType<PathParseSuccess>(outcome).Path;
    }
}
