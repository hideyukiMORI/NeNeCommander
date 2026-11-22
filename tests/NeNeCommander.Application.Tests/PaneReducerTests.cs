using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves immutable pane construction, movement, selection, and visibility transitions.</summary>
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
            EntryMetadata.Unmeasured(EntryVisibility.Normal));

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

    /// <summary>Proves movement targets and paging boundaries preserve explicit selection.</summary>
    [TestMethod]
    public void ApplyWhenMovingAndPagingPreservesTargetsAndSelection()
    {
        DirectoryEntry first = Entry("a-one", EntryVisibility.Normal);
        DirectoryEntry second = Entry("b-two", EntryVisibility.Normal);
        DirectoryEntry third = Entry("c-three", EntryVisibility.Normal);
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
        DirectoryEntry first = Entry("a-one", EntryVisibility.Normal);
        DirectoryEntry skipped = Entry("b-two", EntryVisibility.Hidden);
        DirectoryEntry last = Entry("c-three", EntryVisibility.Normal);
        PaneState state = CreateState([first, skipped, last], 4);

        PaneState next = PaneReducer.Apply(state, UserIntent.MoveNext);
        PaneState beyond = PaneReducer.Apply(next, UserIntent.MoveNext);
        PaneState toLast = PaneReducer.Apply(state, UserIntent.FocusLast);

        Assert.AreSame(last.Path, next.FocusItem);
        Assert.AreSame(first.Path, beyond.FocusItem);
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
            Entry("a-one", EntryVisibility.Normal),
            Entry("b-two", EntryVisibility.Normal),
            Entry("c-three", EntryVisibility.Normal),
            Entry("d-four", EntryVisibility.Normal),
            Entry("e-five", EntryVisibility.Normal),
            Entry("f-six", EntryVisibility.Normal),
        ];
        PaneState state = CreateState(entries, 4);

        PaneState moved = PaneReducer.Apply(state, UserIntent.MoveHalfPageDown);

        Assert.AreSame(entries[2].Path, moved.FocusItem);
        Assert.AreEqual(4, moved.VisiblePageCapacity.Value);
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
        DirectoryEntry first = Entry("a-one", EntryVisibility.Normal);
        DirectoryEntry focused = Entry("b-two", EntryVisibility.Hidden);
        DirectoryEntry last = Entry("c-three", EntryVisibility.Normal);
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

    /// <summary>Proves revealing an all-hidden pane restores focus before movement.</summary>
    [TestMethod]
    public void ApplyHiddenItemVisibilityWhenAllHiddenEntriesAreRevealedRestoresFocusAndMoves()
    {
        DirectoryEntry first = Entry("one", EntryVisibility.Hidden);
        DirectoryEntry second = Entry("two", EntryVisibility.Hidden);
        PaneState shown = CreateState([first, second], 4, HiddenItemVisibility.Shown);
        PaneState omitted = PaneReducer.ApplyHiddenItemVisibility(shown, HiddenItemVisibility.Hidden);

        PaneState revealed = PaneReducer.ApplyHiddenItemVisibility(omitted, HiddenItemVisibility.Shown);
        PaneState moved = PaneReducer.Apply(revealed, UserIntent.MoveNext);

        Assert.AreSame(first.Path, revealed.FocusItem);
        Assert.AreSame(second.Path, moved.FocusItem);
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

    /// <summary>Proves the toggle intent uses the reducer visibility transition in both directions.</summary>
    [TestMethod]
    public void ApplyWhenToggleHiddenItemsFlipsVisibilityAndVisibleEntries()
    {
        DirectoryEntry hidden = Entry("hidden.txt", EntryVisibility.Hidden);
        DirectoryEntry normal = Entry("normal.txt", EntryVisibility.Normal);
        PaneState state = CreateState([hidden, normal], 4, HiddenItemVisibility.Hidden);

        PaneState shown = PaneReducer.Apply(state, UserIntent.ToggleHiddenItems);
        PaneState hiddenAgain = PaneReducer.Apply(shown, UserIntent.ToggleHiddenItems);

        Assert.AreSame(HiddenItemVisibility.Shown, shown.HiddenItemVisibility);
        Assert.HasCount(2, shown.VisibleEntries);
        Assert.AreSame(HiddenItemVisibility.Hidden, hiddenAgain.HiddenItemVisibility);
        Assert.HasCount(1, hiddenAgain.VisibleEntries);
        Assert.AreSame(normal.Path, hiddenAgain.FocusItem);
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

    private static DirectoryEntry Entry(string name, EntryVisibility visibility)
    {
        return DirectoryEntry.Create(
            ParsePath("C:\\" + name),
            name,
            DirectoryEntryKind.File,
            EntryMetadata.Unmeasured(visibility));
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
