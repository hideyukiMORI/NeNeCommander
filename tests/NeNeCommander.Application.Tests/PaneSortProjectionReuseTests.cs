using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves equal sort orders reuse only projections while the reducer still owns focus recovery.</summary>
[TestClass]
public sealed class PaneSortProjectionReuseTests
{
    /// <summary>Proves fresh equal order values retain owned sequences while still repairing the requested focus.</summary>
    [TestMethod]
    [DataRow("Visible")]
    [DataRow("Hidden")]
    [DataRow("Missing")]
    [DataRow("Absent")]
    public void ApplySortOrderWhenOrderIsEqualReusesProjectionAndRecoversFocus(string targetKind)
    {
        DirectoryListing listing = PaneSessionTestContext.HiddenListing("C:\\root",
            ("file1.txt", EntryVisibility.Normal),
            ("file2.txt", EntryVisibility.Hidden),
            ("file3.txt", EntryVisibility.Normal));
        PaneState state = PaneReducer.Navigate(listing, PaneSessionTestContext.Capacity(4), null, HiddenItemVisibility.Hidden);
        state = PaneReducer.Apply(state, UserIntent.ToggleSelection);
        FileSystemPath? target = targetKind switch
        {
            "Visible" => PaneSessionTestContext.ParsePath("c:\\ROOT\\FILE3.TXT"),
            "Hidden" => listing.Entries[1].Path,
            "Missing" => PaneSessionTestContext.ParsePath("C:\\root\\missing.txt"),
            _ => null,
        };
        PaneSortOrder equal = PaneSortOrder.Create(SortKey.Name, SortDirection.Ascending);

        PaneState after = PaneReducer.ApplySortOrder(state, equal, target);

        Assert.AreNotSame(state, after);
        Assert.AreSame(state.Entries, after.Entries);
        Assert.AreSame(state.VisibleEntries, after.VisibleEntries);
        Assert.AreSame(state.Selection, after.Selection);
        Assert.AreSame(state.NavigationHistory, after.NavigationHistory);
        Assert.AreEqual(equal, after.SortOrder);
        Assert.AreSame(state.VisibleEntries[targetKind is "Visible" or "Hidden" ? 1 : 0].Path, after.FocusItem);
        Assert.AreSame(state.VisibleEntries[0].Path, state.FocusItem);
    }

    /// <summary>Proves equal nondefault order values reuse projections without changing descending semantics.</summary>
    [TestMethod]
    public void ApplySortOrderWhenNondefaultOrderIsEqualRetainsProjection()
    {
        DirectoryListing listing = PaneSessionTestContext.Listing("C:\\root",
            ("a.zip", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
        PaneState initial = PaneReducer.Navigate(listing, PaneSessionTestContext.Capacity(4), null, HiddenItemVisibility.Shown);
        PaneSortOrder order = PaneSortOrder.Create(SortKey.Extension, SortDirection.Descending);
        PaneState state = PaneReducer.ApplySortOrder(initial, order, null);
        PaneSortOrder equal = PaneSortOrder.Create(order.Key, order.Direction);

        PaneState after = PaneReducer.ApplySortOrder(state, equal, state.VisibleEntries[1].Path);

        Assert.AreSame(state.VisibleEntries, after.VisibleEntries);
        Assert.AreSame(state.Entries, after.Entries);
        Assert.AreSame(state.VisibleEntries[1].Path, after.FocusItem);
        Assert.AreEqual("a.zip", after.VisibleEntries[0].Name);
    }

    /// <summary>Proves an empty projection can be retained with an absent focus.</summary>
    [TestMethod]
    public void ApplySortOrderWhenEmptyAndEqualRetainsEmptyProjection()
    {
        PaneState state = PaneReducer.Navigate(PaneSessionTestContext.Listing("C:\\root"),
            PaneSessionTestContext.Capacity(4), null, HiddenItemVisibility.Hidden);

        PaneState after = PaneReducer.ApplySortOrder(state,
            PaneSortOrder.Create(SortKey.Name, SortDirection.Ascending), null);

        Assert.AreSame(state.VisibleEntries, after.VisibleEntries);
        Assert.AreSame(state.Entries, after.Entries);
        Assert.IsNull(after.FocusItem);
        Assert.IsEmpty(after.Selection);
    }

    /// <summary>Proves a changed direction still rebuilds ordering while retaining selection and visible focus.</summary>
    [TestMethod]
    public void ApplySortOrderWhenDirectionChangesRebuildsProjection()
    {
        DirectoryListing listing = PaneSessionTestContext.Listing("C:\\root",
            ("file2.txt", DirectoryEntryKind.File), ("file10.txt", DirectoryEntryKind.File));
        PaneState state = PaneReducer.Navigate(listing, PaneSessionTestContext.Capacity(4), null, HiddenItemVisibility.Hidden);
        state = PaneReducer.Apply(state, UserIntent.ToggleSelection);
        PaneSortOrder descending = PaneSortOrder.Create(SortKey.Name, SortDirection.Descending);

        PaneState after = PaneReducer.ApplySortOrder(state, descending, state.FocusItem);

        Assert.AreNotSame(state.VisibleEntries, after.VisibleEntries);
        Assert.AreNotSame(state.Entries, after.Entries);
        Assert.AreSame(state.Selection, after.Selection);
        Assert.AreSame(state.FocusItem, after.FocusItem);
        Assert.AreEqual("file10.txt", after.VisibleEntries[0].Name);
        Assert.AreEqual("file2.txt", after.VisibleEntries[1].Name);
    }

    /// <summary>Proves a refreshed listing owns new entries even when its carried sort order is unchanged.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenRefreshKeepsOrderUsesFreshListing()
    {
        DirectoryListing initial = PaneSessionTestContext.Listing("C:\\root", ("old.txt", DirectoryEntryKind.File));
        DirectoryListing refreshed = PaneSessionTestContext.Listing("C:\\root", ("fresh.txt", DirectoryEntryKind.File));
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(initial, null));
        port.Enqueue(DirectoryReadOutcome.Succeeded(refreshed, null));
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        PaneSnapshot before = await session.NavigateAsync(initial.Location, CancellationToken.None);
        PaneContentListed original = Assert.IsInstanceOfType<PaneContentListed>(before.Content);

        PaneSnapshot after = await session.HandleAsync(UserIntent.Refresh, CancellationToken.None);

        PaneContentListed result = Assert.IsInstanceOfType<PaneContentListed>(after.Content);
        Assert.AreSame(refreshed.Entries[0], result.State.VisibleEntries[0]);
        Assert.AreSame(refreshed.Entries[0].Path, result.State.FocusItem);
        Assert.AreNotSame(original.State.VisibleEntries, result.State.VisibleEntries);
        Assert.AreEqual(original.State.SortOrder, result.State.SortOrder);
        Assert.AreSame(original.State.NavigationHistory, result.State.NavigationHistory);
        Assert.HasCount(2, port.Requests);
    }
}
