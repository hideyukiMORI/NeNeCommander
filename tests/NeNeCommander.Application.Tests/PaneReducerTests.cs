using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves immutable pane construction and canonical reducer transitions.</summary>
[TestClass]
public sealed class PaneReducerTests
{
    /// <summary>Proves valid input is owned and initially focused.</summary>
    [TestMethod]
    public void CreateWhenVisibleItemsAreValidFocusesFirstItemAndOwnsSnapshot()
    {
        FileSystemPath first = ParsePath("C:\\one");
        FileSystemPath second = ParsePath("C:\\two");
        List<FileSystemPath> inputs = [first, second];

        PaneState state = CreateState(inputs, 4);
        inputs.Clear();

        Assert.AreSame(first, state.FocusItem);
        Assert.HasCount(2, state.VisibleItems);
        Assert.IsEmpty(state.Selection);
    }

    /// <summary>Proves an empty pane has no focus.</summary>
    [TestMethod]
    public void CreateWhenVisibleItemsAreEmptyHasNoFocus()
    {
        PaneState state = CreateState([], 1);

        Assert.IsNull(state.FocusItem);
        Assert.IsEmpty(state.VisibleItems);
    }

    /// <summary>Proves duplicate visible identity is rejected.</summary>
    [TestMethod]
    public void CreateWhenVisibleItemsContainDuplicateDuplicateItemFailure()
    {
        FileSystemPath item = ParsePath("C:\\Same");
        FileSystemPath caseVariant = ParsePath("c:\\same");

        PaneStateCreation outcome = PaneState.Create(ParsePath("C:\\"), [item, caseVariant], CreateCapacity(2));

        PaneStateRejected rejected = Assert.IsInstanceOfType<PaneStateRejected>(outcome);
        Assert.AreSame(PaneStateFailureKind.DuplicateItem, rejected.Kind);
    }

    /// <summary>Proves a null visible identity is rejected.</summary>
    [TestMethod]
    public void CreateWhenVisibleItemsContainNullNullItemFailure()
    {
        FileSystemPath[] items = new FileSystemPath[1];

        PaneStateCreation outcome = PaneState.Create(ParsePath("C:\\"), items, CreateCapacity(1));

        PaneStateRejected rejected = Assert.IsInstanceOfType<PaneStateRejected>(outcome);
        Assert.AreSame(PaneStateFailureKind.NullItem, rejected.Kind);
    }

    /// <summary>Proves movement clamps focus while preserving explicit selection.</summary>
    [TestMethod]
    public void ApplyWhenMovingAndPagingClampsFocusAndPreservesSelection()
    {
        FileSystemPath first = ParsePath("C:\\one");
        FileSystemPath second = ParsePath("C:\\two");
        FileSystemPath third = ParsePath("C:\\three");
        PaneState state = CreateState([first, second, third], 4);
        state = PaneReducer.Apply(state, UserIntent.ToggleSelection);

        state = PaneReducer.Apply(state, UserIntent.MoveNext);
        Assert.AreSame(second, state.FocusItem);
        Assert.AreSame(first, state.Selection[0]);

        state = PaneReducer.Apply(state, UserIntent.MoveHalfPageDown);
        Assert.AreSame(third, state.FocusItem);

        state = PaneReducer.Apply(state, UserIntent.MovePrevious);
        Assert.AreSame(second, state.FocusItem);
        state = PaneReducer.Apply(state, UserIntent.MoveHalfPageUp);
        Assert.AreSame(first, state.FocusItem);

        state = PaneReducer.Apply(state, UserIntent.FocusLast);
        Assert.AreSame(third, state.FocusItem);
        state = PaneReducer.Apply(state, UserIntent.FocusFirst);
        Assert.AreSame(first, state.FocusItem);
    }

    /// <summary>Proves toggle and escape selection behavior.</summary>
    [TestMethod]
    public void ApplyWhenTogglingAndEscapingSelectionIsExact()
    {
        FileSystemPath first = ParsePath("C:\\one");
        PaneState state = CreateState([first], 1);

        state = PaneReducer.Apply(state, UserIntent.ToggleSelection);
        Assert.HasCount(1, state.Selection);
        state = PaneReducer.Apply(state, UserIntent.ToggleSelection);
        Assert.IsEmpty(state.Selection);
        state = PaneReducer.Apply(state, UserIntent.ToggleSelection);
        state = PaneReducer.Apply(state, UserIntent.Escape);
        Assert.IsEmpty(state.Selection);
        Assert.AreSame(first, state.FocusItem);
    }

    /// <summary>Proves irrelevant intents retain snapshot identity.</summary>
    [TestMethod]
    public void ApplyWhenIntentDoesNotChangePaneReturnsSameSnapshot()
    {
        PaneState empty = CreateState([], 1);
        PaneState populated = CreateState([ParsePath("C:\\one")], 1);

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
        FileSystemPath[] items = [
            ParsePath("C:\\one"),
            ParsePath("C:\\two"),
            ParsePath("C:\\three"),
            ParsePath("C:\\four"),
            ParsePath("C:\\five"),
            ParsePath("C:\\six"),
        ];
        PaneState state = CreateState(items, 4);

        PaneState moved = PaneReducer.Apply(state, UserIntent.MoveHalfPageDown);

        Assert.AreSame(items[2], moved.FocusItem);
        Assert.AreEqual(4, moved.VisiblePageCapacity.Value);
    }

    /// <summary>Proves a location change focuses the first entry and clears selection.</summary>
    [TestMethod]
    public void NavigateWhenPreferredFocusIsAbsentFocusesFirstEntryWithEmptySelection()
    {
        DirectoryListing listing = CreateListing("C:\\root", "b.txt", "a.txt");

        PaneState state = PaneReducer.Navigate(listing, CreateCapacity(3), null);

        Assert.AreSame(listing.Location, state.Location);
        Assert.AreSame(listing.Entries[0].Path, state.FocusItem);
        Assert.AreEqual("a.txt", listing.Entries[0].Name);
        Assert.HasCount(2, state.VisibleItems);
        Assert.IsEmpty(state.Selection);
        Assert.AreEqual(3, state.VisiblePageCapacity.Value);
    }

    /// <summary>Proves a preferred item is focused by provider identity when present.</summary>
    [TestMethod]
    public void NavigateWhenPreferredFocusIsPresentFocusesItByIdentity()
    {
        DirectoryListing listing = CreateListing("C:\\root", "a.txt", "Docs");

        PaneState state = PaneReducer.Navigate(listing, CreateCapacity(3), ParsePath("c:\\root\\docs"));

        Assert.AreSame(listing.Entries[1].Path, state.FocusItem);
    }

    /// <summary>Proves a preferred item outside the listing falls back to the first entry.</summary>
    [TestMethod]
    public void NavigateWhenPreferredFocusIsMissingFocusesFirstEntry()
    {
        DirectoryListing listing = CreateListing("C:\\root", "a.txt");

        PaneState state = PaneReducer.Navigate(listing, CreateCapacity(3), ParsePath("C:\\root\\missing"));

        Assert.AreSame(listing.Entries[0].Path, state.FocusItem);
    }

    /// <summary>Proves an empty listing has no focus after navigation.</summary>
    [TestMethod]
    public void NavigateWhenListingIsEmptyHasNoFocus()
    {
        DirectoryListing listing = CreateListing("C:\\root");

        PaneState state = PaneReducer.Navigate(listing, CreateCapacity(3), ParsePath("C:\\root\\missing"));

        Assert.IsNull(state.FocusItem);
        Assert.IsEmpty(state.VisibleItems);
    }

    private static DirectoryListing CreateListing(string location, params string[] names)
    {
        FileSystemPath parsedLocation = ParsePath(location);
        DirectoryEntry[] entries = new DirectoryEntry[names.Length];
        for (int index = 0; index < names.Length; index++)
        {
            entries[index] = DirectoryEntry.Create(
                ParsePath(parsedLocation.CanonicalText + "\\" + names[index]),
                names[index],
                DirectoryEntryKind.File);
        }
        DirectoryListingCreation creation = DirectoryListing.Create(
            parsedLocation,
            entries,
            DirectoryListingCompleteness.Complete,
            0);
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(creation).Listing;
    }

    private static PaneState CreateState(IReadOnlyList<FileSystemPath> items, int capacity)
    {
        PaneStateCreation outcome = PaneState.Create(ParsePath("C:\\"), items, CreateCapacity(capacity));
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
