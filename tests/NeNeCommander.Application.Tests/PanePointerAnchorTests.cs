using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves the range anchor follows visible provider identity across ordering and reads.</summary>
[TestClass]
public sealed class PanePointerAnchorTests
{
    /// <summary>Proves a reversed sort retains the anchor and ranges in the new visible order.</summary>
    [TestMethod]
    public async Task SortRetainsAnchorButRangeUsesCurrentVisibleOrder()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing listing = Listing("C:\\pointer", "a", "b", "c");
        await fixture.ListBothAsync(listing, listing);
        _ = await HandleAsync(fixture, Select(fixture, listing.Entries[2], PaneEntryAction.Select));
        _ = await HandleAsync(fixture, UserIntent.SortByName);

        PaneState ranged = await HandleAsync(fixture, Select(fixture, listing.Entries[0], PaneEntryAction.SelectRange));

        Assert.AreSame(listing.Entries[2].Path, ranged.SelectionAnchor);
        CollectionAssert.AreEqual(listing.Entries.Reverse().Select(entry => entry.Path).ToArray(), ranged.Selection.ToArray());
    }

    /// <summary>Proves an omitted anchor disappears and the next range commits its current-focus fallback.</summary>
    [TestMethod]
    public async Task HidingAnchorRemovesItAndNextRangeCommitsNewVisibleFallback()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing listing = DualPaneSessionTestContext.ListingWithVisibility("C:\\pointer", ("a", DirectoryEntryKind.File, EntryVisibility.Normal), ("b", DirectoryEntryKind.File, EntryVisibility.Hidden), ("c", DirectoryEntryKind.File, EntryVisibility.Normal));
        await fixture.ListBothAsync(listing, listing);
        _ = await HandleAsync(fixture, UserIntent.ToggleHiddenItems);
        _ = await HandleAsync(fixture, Select(fixture, listing.Entries[1], PaneEntryAction.Select));

        PaneState hidden = await HandleAsync(fixture, UserIntent.ToggleHiddenItems);
        PaneState ranged = await HandleAsync(fixture, Select(fixture, listing.Entries[0], PaneEntryAction.SelectRange));

        Assert.IsNull(hidden.SelectionAnchor);
        Assert.IsEmpty(hidden.Selection);
        Assert.AreSame(listing.Entries[2].Path, hidden.FocusItem);
        Assert.AreSame(listing.Entries[2].Path, ranged.SelectionAnchor);
        CollectionAssert.AreEqual(new[] { listing.Entries[0].Path, listing.Entries[2].Path }, ranged.Selection.ToArray());
    }

    /// <summary>Proves refresh rebinds only provider-equal identities to the newly owned visible path.</summary>
    [TestMethod]
    [DataRow("C:\\pointer", "C:\\POINTER", "b")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home\\pointer", "\\\\wsl.localhost\\Ubuntu\\home\\pointer", null)]
    public async Task RefreshRebindsAnchorUsingProviderCaseRules(string firstLocation, string nextLocation, string? expectedAnchorName)
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing first = Listing(firstLocation, "A", "B", "C");
        DirectoryListing next = Listing(nextLocation, "a", "b", "c");
        await fixture.ListBothAsync(first, first);
        PaneState selected = await HandleAsync(fixture, Select(fixture, first.Entries[1], PaneEntryAction.Select));
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(next, null));

        PaneState refreshed = await HandleAsync(fixture, UserIntent.Refresh);

        Assert.IsEmpty(refreshed.Selection);
        Assert.AreSame(first.Entries[1].Path, selected.SelectionAnchor);
        if (expectedAnchorName is not null)
        {
            Assert.AreSame(next.Entries.Single(entry => entry.Name == expectedAnchorName).Path, refreshed.SelectionAnchor);
            Assert.AreNotSame(selected.SelectionAnchor, refreshed.SelectionAnchor);
        }
        else
        {
            Assert.IsNull(refreshed.SelectionAnchor);
        }
    }

    /// <summary>Proves a removed anchor is not recreated by neighbouring focus recovery.</summary>
    [TestMethod]
    public async Task RefreshThatRemovesAnchorLeavesItAbsent()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing first = Listing("C:\\pointer", "a", "b", "c");
        DirectoryListing next = Listing("C:\\pointer", "a", "c", "d");
        await fixture.ListBothAsync(first, first);
        _ = await HandleAsync(fixture, Select(fixture, first.Entries[1], PaneEntryAction.Select));
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(next, null));

        PaneState refreshed = await HandleAsync(fixture, UserIntent.Refresh);

        Assert.IsNull(refreshed.SelectionAnchor);
        Assert.IsEmpty(refreshed.Selection);
        Assert.AreSame(next.Entries[1].Path, refreshed.FocusItem);
    }

    /// <summary>Proves even an explicit same-location navigation clears the old range anchor.</summary>
    [TestMethod]
    public async Task SuccessfulNavigationClearsAnchorEvenWhenThePathIsTheSame()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing listing = Listing("C:\\pointer", "a", "b", "c");
        await fixture.ListBothAsync(listing, listing);
        _ = await HandleAsync(fixture, Select(fixture, listing.Entries[2], PaneEntryAction.Select));
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));

        DualPaneSnapshot navigated = await fixture.Panes.NavigateAsync(PaneSide.Left, listing.Location, CancellationToken.None);

        PaneState state = Assert.IsInstanceOfType<PaneContentListed>(navigated.Left.Content).State;
        Assert.IsNull(state.SelectionAnchor);
        Assert.IsEmpty(state.Selection);
        Assert.AreSame(listing.Entries[0].Path, state.FocusItem);
    }

    private static DirectoryListing Listing(string location, string first, string second, string third)
    {
        return DualPaneSessionTestContext.Listing(location, (first, DirectoryEntryKind.File), (second, DirectoryEntryKind.File), (third, DirectoryEntryKind.File));
    }

    private static UserIntent Select(DualPaneSessionTestContext fixture, DirectoryEntry entry, PaneEntryAction action)
    {
        return UserIntent.InteractWithPaneEntry(PaneSide.Left, fixture.Panes.Current.Left, entry, action);
    }

    private static async Task<PaneState> HandleAsync(DualPaneSessionTestContext fixture, UserIntent intent)
    {
        DualPaneSnapshot after = await fixture.Panes.HandleAsync(intent, RecordingDualPaneObserver.Create(), CancellationToken.None);
        return Assert.IsInstanceOfType<PaneContentListed>(after.Left.Content).State;
    }
}
