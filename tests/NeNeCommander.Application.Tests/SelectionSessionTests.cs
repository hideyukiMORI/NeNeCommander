using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves keyboard selection retains pointer, ordering and read ownership contracts.</summary>
[TestClass]
public sealed class SelectionSessionTests
{
    /// <summary>Proves pointer and keyboard ranges share one anchor and never change the passive pane.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPointerAndKeyboardAlternateUsesSameAnchorAndActivePaneAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing listing = Listing("C:\\selection", "a", "b", "c");
        await fixture.ListBothAsync(listing, listing);
        PaneSnapshot passive = fixture.Panes.Current.Right;
        _ = await HandleAsync(fixture, UserIntent.InteractWithPaneEntry(PaneSide.Left, fixture.Panes.Current.Left,
            listing.Entries[1], PaneEntryAction.Select));

        PaneState ranged = await HandleAsync(fixture, UserIntent.SelectRangeLast);
        PaneState clicked = await HandleAsync(fixture, UserIntent.InteractWithPaneEntry(PaneSide.Left, fixture.Panes.Current.Left,
            listing.Entries[0], PaneEntryAction.SelectRange));

        Assert.AreSame(listing.Entries[1].Path, ranged.SelectionAnchor);
        Assert.AreSame(ranged.SelectionAnchor, clicked.SelectionAnchor);
        CollectionAssert.AreEqual(new[] { listing.Entries[1].Path, listing.Entries[2].Path }, ranged.Selection.ToArray());
        CollectionAssert.AreEqual(new[] { listing.Entries[0].Path, listing.Entries[1].Path }, clicked.Selection.ToArray());
        Assert.AreSame(passive, fixture.Panes.Current.Right);
        Assert.AreSame(PaneSide.Left, fixture.Panes.Current.ActiveSide);
        Assert.HasCount(1, fixture.Left.Requests);
        Assert.HasCount(1, fixture.Right.Requests);
    }

    /// <summary>Proves a sorted range uses the current projected order and the preserved anchor.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenSortChangesOrderRangesOverNewOrderAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing listing = Listing("C:\\selection", "a", "b", "c");
        await fixture.ListBothAsync(listing, listing);
        _ = await HandleAsync(fixture, UserIntent.SelectRangeNext);
        _ = await HandleAsync(fixture, UserIntent.SortByName);

        PaneState ranged = await HandleAsync(fixture, UserIntent.SelectRangeFirst);

        Assert.AreSame(listing.Entries[0].Path, ranged.SelectionAnchor);
        Assert.AreSame(listing.Entries[2].Path, ranged.FocusItem);
        CollectionAssert.AreEqual(listing.Entries.Reverse().Select(entry => entry.Path).ToArray(), ranged.Selection.ToArray());
    }

    /// <summary>Proves hiding an anchor discards it before the next keyboard range establishes a visible fallback.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenHiddenAnchorDisappearsUsesCurrentVisibleFocusAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing listing = DualPaneSessionTestContext.ListingWithVisibility("C:\\selection",
            ("a", DirectoryEntryKind.File, EntryVisibility.Normal), ("b", DirectoryEntryKind.File, EntryVisibility.Hidden),
            ("c", DirectoryEntryKind.File, EntryVisibility.Normal));
        await fixture.ListBothAsync(listing, listing);
        _ = await HandleAsync(fixture, UserIntent.ToggleHiddenItems);
        _ = await HandleAsync(fixture, UserIntent.MoveNext);
        _ = await HandleAsync(fixture, UserIntent.SelectRangeLast);
        PaneState hidden = await HandleAsync(fixture, UserIntent.ToggleHiddenItems);

        PaneState ranged = await HandleAsync(fixture, UserIntent.SelectRangeFirst);

        Assert.IsNull(hidden.SelectionAnchor);
        Assert.AreSame(listing.Entries[2].Path, ranged.SelectionAnchor);
        CollectionAssert.AreEqual(new[] { listing.Entries[0].Path, listing.Entries[2].Path }, ranged.Selection.ToArray());
    }

    /// <summary>Proves refresh clears marks and rebinds keyboard anchors with provider-specific case identity.</summary>
    [TestMethod]
    [DataRow("C:\\selection", "C:\\SELECTION", "b")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home\\selection", "\\\\wsl.localhost\\Ubuntu\\home\\selection", null)]
    public async Task HandleAsyncWhenRefreshChangesPathCaseRebindsOnlyEqualAnchorAsync(string firstLocation, string secondLocation, string? expectedAnchorName)
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing first = Listing(firstLocation, "A", "B", "C");
        DirectoryListing next = Listing(secondLocation, "a", "b", "c");
        await fixture.ListBothAsync(first, first);
        _ = await HandleAsync(fixture, UserIntent.MoveNext);
        _ = await HandleAsync(fixture, UserIntent.SelectRangeLast);
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(next, null));

        PaneState refreshed = await HandleAsync(fixture, UserIntent.Refresh);
        PaneState ranged = await HandleAsync(fixture, UserIntent.SelectRangeFirst);

        Assert.IsEmpty(refreshed.Selection);
        if (expectedAnchorName is not null)
        {
            Assert.AreSame(next.Entries.Single(entry => entry.Name == expectedAnchorName).Path, refreshed.SelectionAnchor);
            CollectionAssert.AreEqual(new[] { next.Entries[0].Path, next.Entries[1].Path }, ranged.Selection.ToArray());
        }
        else
        {
            Assert.IsNull(refreshed.SelectionAnchor);
            Assert.AreSame(refreshed.FocusItem, ranged.SelectionAnchor);
        }
    }

    /// <summary>Proves range selection is committed to either active pane without touching the other.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenRightPaneIsActiveSelectsOnlyRightThenNavigationClearsItAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing listing = Listing("C:\\selection", "a", "b", "c");
        await fixture.ListBothAsync(listing, listing);
        PaneSnapshot left = fixture.Panes.Current.Left;
        _ = await fixture.Panes.HandleAsync(UserIntent.ActivateOtherPane, RecordingDualPaneObserver.Create(), CancellationToken.None);

        DualPaneSnapshot selected = await fixture.Panes.HandleAsync(UserIntent.SelectRangeLast, RecordingDualPaneObserver.Create(), CancellationToken.None);
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        DualPaneSnapshot navigated = await fixture.Panes.NavigateAsync(PaneSide.Right, listing.Location, CancellationToken.None);

        Assert.AreSame(left, selected.Left);
        Assert.HasCount(3, Assert.IsInstanceOfType<PaneContentListed>(selected.Right.Content).State.Selection);
        PaneState state = Assert.IsInstanceOfType<PaneContentListed>(navigated.Right.Content).State;
        Assert.IsEmpty(state.Selection);
        Assert.IsNull(state.SelectionAnchor);
    }

    private static DirectoryListing Listing(string location, string first, string second, string third)
    {
        return DualPaneSessionTestContext.Listing(location,
            (first, DirectoryEntryKind.File), (second, DirectoryEntryKind.File), (third, DirectoryEntryKind.File));
    }

    private static async Task<PaneState> HandleAsync(DualPaneSessionTestContext fixture, UserIntent intent)
    {
        DualPaneSnapshot after = await fixture.Panes.HandleAsync(intent, RecordingDualPaneObserver.Create(), CancellationToken.None);
        return Assert.IsInstanceOfType<PaneContentListed>(after.Left.Content).State;
    }
}
