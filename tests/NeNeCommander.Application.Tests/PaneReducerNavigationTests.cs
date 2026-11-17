using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves navigation restores visible focus by identity and commits bounded provider-aware history.</summary>
[TestClass]
public sealed class PaneReducerNavigationTests
{
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

    /// <summary>Proves forward recovery starts at a hidden preferred item in the first position.</summary>
    [TestMethod]
    public void NavigateWhenFirstPreferredFocusIsHiddenFocusesNextVisibleEntry()
    {
        DirectoryListing listing = CreateListing(
            "C:\\root",
            ("a.txt", EntryVisibility.Hidden),
            ("b.txt", EntryVisibility.Normal));

        PaneState state = Navigate(listing, listing.Entries[0].Path, HiddenItemVisibility.Hidden);

        Assert.AreSame(listing.Entries[1].Path, state.FocusItem);
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

    /// <summary>Proves backward recovery chooses the nearest earlier visible item.</summary>
    [TestMethod]
    public void NavigateWhenLastPreferredFocusIsHiddenFocusesNearestPreviousVisibleEntry()
    {
        DirectoryListing listing = CreateListing(
            "C:\\root",
            ("a.txt", EntryVisibility.Normal),
            ("b.txt", EntryVisibility.Normal),
            ("c.txt", EntryVisibility.Hidden));

        PaneState state = Navigate(listing, listing.Entries[2].Path, HiddenItemVisibility.Hidden);

        Assert.AreSame(listing.Entries[1].Path, state.FocusItem);
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

    /// <summary>Proves a new visit after Back removes the abandoned Forward suffix.</summary>
    [TestMethod]
    public void CommitNavigationWhenAppendingAfterBackTruncatesForwardLocations()
    {
        PaneState first = Navigate(CreateListing("C:\\first"), null, HiddenItemVisibility.Hidden);
        PaneState second = Commit(first, "C:\\second", PaneNavigationAction.Append);
        PaneState third = Commit(second, "C:\\third", PaneNavigationAction.Append);
        PaneState backed = Commit(third, "C:\\second", PaneNavigationAction.Back);

        PaneState branched = Commit(backed, "C:\\branch", PaneNavigationAction.Append);

        PaneNavigationHistory history = branched.NavigationHistory;
        Assert.HasCount(3, history.Locations);
        Assert.AreEqual("C:\\first", history.Locations[0].CanonicalText);
        Assert.AreEqual("C:\\second", history.Locations[1].CanonicalText);
        Assert.AreEqual("C:\\branch", history.Locations[2].CanonicalText);
        Assert.AreEqual(2, history.CurrentIndex);
        Assert.IsNull(history.ForwardTarget);
    }

    /// <summary>Proves history includes current, evicts its oldest entry, and never exceeds 100.</summary>
    [TestMethod]
    public void CommitNavigationWhenLocationLimitIsExceededEvictsOldestLocation()
    {
        PaneState state = Navigate(CreateListing("C:\\initial"), null, HiddenItemVisibility.Hidden);
        for (int index = 1; index <= PaneNavigationHistory.LocationLimit; index++)
        {
            state = Commit(state, "C:\\location" + index, PaneNavigationAction.Append);
        }

        PaneNavigationHistory history = state.NavigationHistory;
        Assert.HasCount(PaneNavigationHistory.LocationLimit, history.Locations);
        Assert.AreEqual("C:\\location1", history.Locations[0].CanonicalText);
        Assert.AreEqual("C:\\location100", history.Locations[^1].CanonicalText);
        Assert.AreEqual(PaneNavigationHistory.LocationLimit - 1, history.CurrentIndex);
    }

    /// <summary>Proves the canonical provider comparer decides whether a visit is a new location.</summary>
    [TestMethod]
    public void CommitNavigationWhenPathCaseVariesUsesProviderIdentityRules()
    {
        PaneState windows = Navigate(CreateListing("C:\\Root"), null, HiddenItemVisibility.Hidden);
        PaneState sameWindows = Commit(windows, "c:\\ROOT", PaneNavigationAction.Append);
        PaneState wsl = Navigate(
            CreateListing("\\\\wsl.localhost\\Ubuntu\\home\\Name"),
            null,
            HiddenItemVisibility.Hidden);

        PaneState distinctWsl = Commit(
            wsl,
            "\\\\wsl.localhost\\ubuntu\\home\\name",
            PaneNavigationAction.Append);

        Assert.HasCount(1, sameWindows.NavigationHistory.Locations);
        Assert.HasCount(2, distinctWsl.NavigationHistory.Locations);
    }

    /// <summary>Proves navigation commits reject absent required state and action values.</summary>
    [TestMethod]
    public void CommitNavigationWhenRequiredArgumentIsNullThrowsArgumentNullException()
    {
        PaneState state = Navigate(CreateListing("C:\\root"), null, HiddenItemVisibility.Hidden);

        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => PaneReducer.CommitNavigation(null, null!, PaneNavigationAction.Append));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => PaneReducer.CommitNavigation(null, state, null!));
    }

    private static PaneState Navigate(
        DirectoryListing listing,
        FileSystemPath? preferredFocus,
        HiddenItemVisibility visibility)
    {
        return PaneReducer.Navigate(listing, CreateCapacity(3), preferredFocus, visibility);
    }

    private static PaneState Commit(PaneState previous, string location, PaneNavigationAction action)
    {
        PaneState navigated = Navigate(CreateListing(location), null, previous.HiddenItemVisibility);
        return PaneReducer.CommitNavigation(previous, navigated, action);
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
                EntryMetadata.Unmeasured(entries[index].Visibility));
        }
        DirectoryListingCreation creation = DirectoryListing.Create(
            parsedLocation,
            built,
            DirectoryListingCompleteness.Complete,
            0);
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(creation).Listing;
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
