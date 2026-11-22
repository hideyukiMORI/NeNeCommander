using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves direct counts, snapshot reuse, selection and visibility through the pane presenter.</summary>
[TestClass]
public sealed class PaneCountsTests
{
    /// <summary>Proves provider kinds are counted without recursively interpreting folder metadata.</summary>
    [TestMethod]
    public async Task PresentAsyncWhenKindsAreMixedCountsOnlyDirectEntries()
    {
        PaneSnapshot snapshot = await PaneListingTestContext.ListAsync(
            PaneListingTestContext.CreateMeasuredListing("C:\\root"));

        PanePresentation presentation = PaneListingTestContext.Present(snapshot);

        PaneCounts counts = Assert.IsInstanceOfType<PaneCounts>(presentation.Counts);
        Assert.AreEqual(2, counts.Visible.Files);
        Assert.AreEqual(1, counts.Visible.Directories);
        Assert.AreEqual(3, counts.Visible.Total);
        Assert.AreSame(counts.Visible, counts.Loaded);
        Assert.AreEqual(0, counts.Selected);
    }

    /// <summary>Proves movement and activation reuse the already-counted immutable entries.</summary>
    [TestMethod]
    public async Task PresentAsyncWhenFocusOrActivationChangesReusesCounts()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = PaneListingTestContext.CreateMeasuredListing("C:\\root");
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        PaneSession session = PaneListingTestContext.CreateSession(port);
        PaneSnapshot listed = await session.NavigateAsync(listing.Location, CancellationToken.None);
        PanePresentation first = PaneListingTestContext.Present(listed);
        PaneSnapshot moved = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);

        PanePresentation next = PaneListingPresenter.Present(moved, PaneFrame.Active, TestMetadataFormats.Utc, first);
        PanePresentation passive = PaneListingPresenter.Present(moved, PaneFrame.Passive, TestMetadataFormats.Utc, next);
        PanePresentation repeated = PaneListingPresenter.Present(moved, PaneFrame.Passive, TestMetadataFormats.Utc, passive);

        Assert.AreSame(first.Counts, next.Counts);
        Assert.AreSame(next.Counts, passive.Counts);
        Assert.AreSame(passive, repeated);
        Assert.AreSame(first.Rows, passive.Rows);
    }

    /// <summary>Proves selection changes its own count without re-counting files and folders.</summary>
    [TestMethod]
    public async Task PresentAsyncWhenSelectionChangesRetainsEntryCounts()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = PaneListingTestContext.CreateMeasuredListing("C:\\root");
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        PaneSession session = PaneListingTestContext.CreateSession(port);
        PanePresentation first = PaneListingTestContext.Present(await session.NavigateAsync(listing.Location, CancellationToken.None));
        PaneSnapshot selected = await session.HandleAsync(UserIntent.ToggleSelection, CancellationToken.None);

        PanePresentation next = PaneListingPresenter.Present(selected, PaneFrame.Active, TestMetadataFormats.Utc, first);

        PaneCounts before = Assert.IsInstanceOfType<PaneCounts>(first.Counts);
        PaneCounts after = Assert.IsInstanceOfType<PaneCounts>(next.Counts);
        Assert.AreEqual(0, before.Selected);
        Assert.AreEqual(1, after.Selected);
        Assert.AreSame(before.Visible, after.Visible);
        Assert.AreSame(before.Loaded, after.Loaded);
        Assert.AreNotSame(before, after);
    }

    /// <summary>Proves hidden rows count only among loaded entries until the reducer shows them.</summary>
    [TestMethod]
    public async Task PresentAsyncWhenVisibilityChangesSeparatesShownAndLoadedCounts()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = PaneListingTestContext.CreateMixedListing("C:\\root");
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        PaneSession session = PaneListingTestContext.CreateSession(port);
        PanePresentation first = PaneListingTestContext.Present(await session.NavigateAsync(listing.Location, CancellationToken.None));
        PaneSnapshot shown = await session.HandleAsync(UserIntent.ToggleHiddenItems, CancellationToken.None);

        PanePresentation next = PaneListingPresenter.Present(shown, PaneFrame.Active, TestMetadataFormats.Utc, first);
        PaneSnapshot hidden = await session.HandleAsync(UserIntent.ToggleHiddenItems, CancellationToken.None);
        PanePresentation final = PaneListingPresenter.Present(hidden, PaneFrame.Active, TestMetadataFormats.Utc, next);

        Assert.AreEqual(1, Assert.IsInstanceOfType<PaneCounts>(first.Counts).Visible.Files);
        Assert.AreEqual(2, Assert.IsInstanceOfType<PaneCounts>(first.Counts).Loaded.Files);
        Assert.AreEqual(2, Assert.IsInstanceOfType<PaneCounts>(next.Counts).Visible.Files);
        Assert.AreEqual(1, Assert.IsInstanceOfType<PaneCounts>(final.Counts).Visible.Files);
        Assert.AreEqual(2, Assert.IsInstanceOfType<PaneCounts>(final.Counts).Loaded.Files);
    }

    /// <summary>Proves equal-length replacement listings cannot reuse counts from a different kind mix.</summary>
    [TestMethod]
    public async Task PresentAsyncWhenListingChangesRecountsKinds()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing before = PaneListingTestContext.CreateMeasuredListing("C:\\root");
        DirectoryListing after = PaneListingTestContext.CreateListing("C:\\root", ["a", "b", "c"], DirectoryListingCompleteness.Complete, 0);
        port.Enqueue(DirectoryReadOutcome.Succeeded(before, null));
        port.Enqueue(DirectoryReadOutcome.Succeeded(after, null));
        PaneSession session = PaneListingTestContext.CreateSession(port);
        PanePresentation first = PaneListingTestContext.Present(await session.NavigateAsync(before.Location, CancellationToken.None));

        PaneSnapshot refreshed = await session.RefreshAsync(CancellationToken.None);
        PanePresentation next = PaneListingPresenter.Present(refreshed, PaneFrame.Active, TestMetadataFormats.Utc, first);

        Assert.AreEqual(2, Assert.IsInstanceOfType<PaneCounts>(first.Counts).Visible.Files);
        Assert.AreEqual(1, Assert.IsInstanceOfType<PaneCounts>(first.Counts).Visible.Directories);
        Assert.AreEqual(3, Assert.IsInstanceOfType<PaneCounts>(next.Counts).Visible.Files);
        Assert.AreEqual(0, Assert.IsInstanceOfType<PaneCounts>(next.Counts).Visible.Directories);
        Assert.AreNotSame(first.Counts, next.Counts);
    }

    /// <summary>Proves absence before a read is distinct from an empty successful listing.</summary>
    [TestMethod]
    public async Task PresentAsyncWhenListingIsAbsentDoesNotInventZeroCounts()
    {
        PanePresentation absent = PaneListingTestContext.Present(PaneSnapshot.Initial);
        PaneSnapshot empty = await PaneListingTestContext.ListAsync(
            PaneListingTestContext.CreateListing("C:\\empty", [], DirectoryListingCompleteness.Complete, 0));

        PanePresentation listed = PaneListingPresenter.Present(empty, PaneFrame.Active, TestMetadataFormats.Utc, absent);

        Assert.IsNull(absent.Counts);
        PaneCounts counts = Assert.IsInstanceOfType<PaneCounts>(listed.Counts);
        Assert.AreEqual(0, counts.Visible.Total);
        Assert.AreEqual(0, counts.Loaded.Total);
        Assert.AreEqual(0, counts.Selected);
    }

    /// <summary>Proves projecting the other pane cannot alter a prior pane's counts.</summary>
    [TestMethod]
    public async Task PresentAsyncWhenPanesDifferKeepsCountsIndependent()
    {
        PanePresentation left = PaneListingTestContext.Present(await PaneListingTestContext.ListAsync(
            PaneListingTestContext.CreateMeasuredListing("C:\\left")));
        PaneSnapshot rightSnapshot = await PaneListingTestContext.ListAsync(
            PaneListingTestContext.CreateListing("C:\\right", ["only"], DirectoryListingCompleteness.Complete, 0));

        PanePresentation right = PaneListingPresenter.Present(rightSnapshot, PaneFrame.Passive, TestMetadataFormats.Utc);

        Assert.AreEqual(3, Assert.IsInstanceOfType<PaneCounts>(left.Counts).Visible.Total);
        Assert.AreEqual(1, Assert.IsInstanceOfType<PaneCounts>(right.Counts).Visible.Total);
        Assert.AreNotSame(left.Counts, right.Counts);
    }

    /// <summary>Proves factory absence is rejected before count projection.</summary>
    [TestMethod]
    public void CreateWhenRequiredSourceIsNullThrowsArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => PaneEntryCounts.Create(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => PaneCounts.Create(null!, null));
    }
}
