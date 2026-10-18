using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Drives;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves PaneListingPresenter behavior and contracts.</summary>
[TestClass]
public sealed class PaneListingPresenterTests
{
    /// <summary>Proves a listed location shows its ordered rows, focus entry, and address.</summary>
    [TestMethod]
    public async Task PresentWhenLocationIsListedShowsRowsFocusAndAddress()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = PaneListingTestContext.CreateListing("C:\\projects", ["file10.txt", "file2.txt"], DirectoryListingCompleteness.Complete, 0);
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, DriveKind.Network));
        PaneSession session = PaneListingTestContext.CreateSession(port);
        PaneSnapshot snapshot = await session.NavigateAsync(listing.Location, CancellationToken.None);
        PaneSnapshot moved = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);

        PanePresentation initial = PaneListingPresenter.Present(snapshot, PaneFrame.Active, TestMetadataFormats.Utc);
        PanePresentation afterMove = PaneListingPresenter.Present(moved, PaneFrame.Active, TestMetadataFormats.Utc);

        Assert.HasCount(2, initial.Rows);
        Assert.AreEqual("file10.txt", listing.Entries[0].Name);
        Assert.AreSame(listing.Entries[1], initial.Rows[0].Entry);
        Assert.AreSame(listing.Entries[0], initial.Rows[1].Entry);
        Assert.AreSame(initial.Rows[0], initial.FocusRow);
        Assert.AreSame(listing.Entries[0], afterMove.FocusRow?.Entry);
        Assert.AreSame(PaneRowMark.FocusInActivePane, initial.Rows[0].Mark);
        Assert.AreSame(PaneRowMark.Unmarked, initial.Rows[1].Mark);
        Assert.AreSame(PaneRowKind.File, initial.Rows[0].Kind);
        Assert.AreSame(PaneStatus.Complete, initial.Status);
        Assert.AreSame(PaneLocationStatus.NetworkDrive, initial.LocationStatus);
        Assert.AreSame(PaneLocationStatus.NetworkDrive, afterMove.LocationStatus);
        Assert.AreEqual("C:\\projects", initial.AddressText);
    }

    /// <summary>Proves selected rows carry the selected mark by provider identity and escape clears it.</summary>
    [TestMethod]
    public async Task PresentWhenItemsAreSelectedMarksOnlyThoseRows()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = PaneListingTestContext.CreateListing("C:\\projects", ["a.txt", "b.txt", "c.txt"], DirectoryListingCompleteness.Complete, 0);
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        PaneSession session = PaneListingTestContext.CreateSession(port);
        _ = await session.NavigateAsync(listing.Location, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.ToggleSelection, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);
        PaneSnapshot selected = await session.HandleAsync(UserIntent.ToggleSelection, CancellationToken.None);
        PaneSnapshot cleared = await session.HandleAsync(UserIntent.Escape, CancellationToken.None);

        PanePresentation marked = PaneListingPresenter.Present(selected, PaneFrame.Active, TestMetadataFormats.Utc);
        PanePresentation unmarked = PaneListingPresenter.Present(cleared, PaneFrame.Active, TestMetadataFormats.Utc);

        Assert.AreSame(PaneRowMark.Selected, marked.Rows[0].Mark);
        Assert.AreSame(PaneRowMark.Unmarked, marked.Rows[1].Mark);
        Assert.AreSame(PaneRowMark.FocusInActivePane, marked.Rows[2].Mark);
        Assert.AreSame(marked.Rows[2], marked.FocusRow);
        Assert.AreSame(PaneRowMark.Unmarked, unmarked.Rows[0].Mark);
        Assert.AreSame(PaneRowMark.FocusInActivePane, unmarked.Rows[2].Mark);
        Assert.AreSame(unmarked.Rows[2], unmarked.FocusRow);
    }

    /// <summary>Proves an empty listing has no focus entry.</summary>
    [TestMethod]
    public async Task PresentWhenListingIsEmptyHasNoFocusEntry()
    {
        PaneSnapshot snapshot = await PaneListingTestContext.ListAsync(PaneListingTestContext.CreateListing("C:\\projects", [], DirectoryListingCompleteness.Complete, 0));

        PanePresentation presentation = PaneListingPresenter.Present(snapshot, PaneFrame.Active, TestMetadataFormats.Utc);

        Assert.IsNull(presentation.FocusRow);
        Assert.IsEmpty(presentation.Rows);
    }

    /// <summary>Proves the passive pane marks its focus row differently and selection outranks it.</summary>
    [TestMethod]
    public async Task PresentWhenPaneIsPassiveMarksFocusRowWithoutTheActiveTreatment()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = PaneListingTestContext.CreateListing("C:\\projects", ["a.txt", "b.txt"], DirectoryListingCompleteness.Complete, 0);
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        PaneSession session = PaneListingTestContext.CreateSession(port);
        _ = await session.NavigateAsync(listing.Location, CancellationToken.None);
        PaneSnapshot focused = await session.HandleAsync(UserIntent.ToggleSelection, CancellationToken.None);

        PanePresentation passive = PaneListingPresenter.Present(focused, PaneFrame.Passive, TestMetadataFormats.Utc);
        PanePresentation active = PaneListingPresenter.Present(focused, PaneFrame.Active, TestMetadataFormats.Utc);

        Assert.AreSame(PaneRowMark.Selected, passive.Rows[0].Mark);
        Assert.AreSame(PaneRowMark.FocusInActivePane, active.Rows[0].Mark);
        Assert.AreSame(passive.Rows[0], passive.FocusRow);
    }

    /// <summary>Proves an unselected focus row of a passive pane keeps the passive focus mark.</summary>
    [TestMethod]
    public async Task PresentWhenPassivePaneFocusIsUnselectedMarksPassiveFocus()
    {
        PaneSnapshot snapshot = await PaneListingTestContext.ListAsync(
            PaneListingTestContext.CreateListing("C:\\projects", ["a.txt", "b.txt"], DirectoryListingCompleteness.Complete, 0));

        PanePresentation passive = PaneListingPresenter.Present(snapshot, PaneFrame.Passive, TestMetadataFormats.Utc);

        Assert.AreSame(PaneRowMark.FocusInPassivePane, passive.Rows[0].Mark);
        Assert.AreSame(PaneRowMark.Unmarked, passive.Rows[1].Mark);
    }

    /// <summary>
    /// Proves a new sort order over the same listing rebuilds the rows in the projected order with
    /// focus kept, and the next focus movement reuses those rebuilt rows again.
    /// </summary>
    [TestMethod]
    public async Task PresentWhenSortOrderChangesRebuildsRowsInProjectedOrder()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = PaneListingTestContext.CreateListing(
            "C:\\projects",
            ["file2.zip", "file10.txt", "file1.md"],
            DirectoryListingCompleteness.Complete,
            0);
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        PaneSession session = PaneListingTestContext.CreateSession(port);
        PaneSnapshot listed = await session.NavigateAsync(listing.Location, CancellationToken.None);
        PanePresentation initial = PaneListingPresenter.Present(listed, PaneFrame.Active, TestMetadataFormats.Utc);
        PaneSnapshot sorted = await session.HandleAsync(UserIntent.SortByExtension, CancellationToken.None);
        PaneSnapshot moved = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);

        PanePresentation reordered = PaneListingPresenter.Present(sorted, PaneFrame.Active, TestMetadataFormats.Utc, initial);
        PaneRow[] reorderedRows = [.. reordered.Rows];
        PanePresentation afterMove = PaneListingPresenter.Present(moved, PaneFrame.Active, TestMetadataFormats.Utc, reordered);

        Assert.AreNotSame(initial.Rows, reordered.Rows);
        Assert.AreSame(listing.Entries[0], initial.Rows[0].Entry);
        Assert.AreSame(listing.Entries[2], initial.Rows[1].Entry);
        Assert.AreSame(listing.Entries[1], initial.Rows[2].Entry);
        Assert.AreSame(listing.Entries[0], reorderedRows[0].Entry);
        Assert.AreSame(listing.Entries[1], reorderedRows[1].Entry);
        Assert.AreSame(listing.Entries[2], reorderedRows[2].Entry);
        Assert.AreSame(reorderedRows[0], reordered.FocusRow);
        Assert.AreSame(PaneRowMark.FocusInActivePane, reorderedRows[0].Mark);
        Assert.AreSame(PaneSortStatus.ExtensionAscending, reordered.SortStatus);
        Assert.AreSame(PaneSortStatus.NameAscending, initial.SortStatus);
        Assert.AreSame(reordered.Rows, afterMove.Rows);
        Assert.AreNotSame(reorderedRows[0], afterMove.Rows[0]);
        Assert.AreSame(reorderedRows[2], afterMove.Rows[2]);
        Assert.AreSame(listing.Entries[1], afterMove.FocusRow?.Entry);
    }

    /// <summary>Proves an omitted entry produces no row at all while its listing still holds it.</summary>
    [TestMethod]
    public async Task PresentWhenHiddenEntriesAreOmittedProjectsOnlyTheVisibleRows()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = PaneListingTestContext.CreateMixedListing("C:\\projects");
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        PaneSession session = PaneListingTestContext.CreateSession(port, HiddenItemVisibility.Hidden);
        PaneSnapshot snapshot = await session.NavigateAsync(listing.Location, CancellationToken.None);

        PanePresentation presentation = PaneListingPresenter.Present(snapshot, PaneFrame.Active, TestMetadataFormats.Utc);

        Assert.HasCount(2, listing.Entries);
        Assert.HasCount(1, presentation.Rows);
        Assert.AreSame(listing.Entries[0], presentation.Rows[0].Entry);
        Assert.AreSame(PaneRowVisibility.Normal, presentation.Rows[0].Visibility);
        Assert.AreSame(presentation.Rows[0], presentation.FocusRow);
        Assert.AreSame(PaneStatus.Complete, presentation.Status);
    }

    /// <summary>Proves a shown hidden entry keeps its row and is rendered with the hidden text brush.</summary>
    [TestMethod]
    public async Task PresentWhenHiddenEntriesAreShownRendersThemWithTheHiddenBrush()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = PaneListingTestContext.CreateMixedListing("C:\\projects");
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        PaneSession session = PaneListingTestContext.CreateSession(port, HiddenItemVisibility.Shown);
        PaneSnapshot snapshot = await session.NavigateAsync(listing.Location, CancellationToken.None);

        PanePresentation presentation = PaneListingPresenter.Present(snapshot, PaneFrame.Active, TestMetadataFormats.Utc);

        Assert.HasCount(2, presentation.Rows);
        Assert.AreSame(PaneRowVisibility.Normal, presentation.Rows[0].Visibility);
        Assert.AreSame(PaneRowVisibility.Hidden, presentation.Rows[1].Visibility);
        Assert.AreEqual("TextPrimaryBrush", presentation.Rows[0].Visibility.NameBrushResourceKey);
        Assert.AreEqual("TextHiddenBrush", presentation.Rows[1].Visibility.NameBrushResourceKey);
    }
}
