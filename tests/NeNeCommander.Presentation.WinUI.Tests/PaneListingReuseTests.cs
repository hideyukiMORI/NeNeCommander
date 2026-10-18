using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Drives;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves PaneListingReuse behavior and contracts.</summary>
[TestClass]
public sealed class PaneListingReuseTests
{
    /// <summary>Proves an activity-only update keeps the existing row source and row instances.</summary>
    [TestMethod]
    public async Task PresentWhenOnlyActivityChangesReusesRows()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = PaneListingTestContext.CreateListing(
            "C:\\projects",
            ["a.txt", "b.txt"],
            DirectoryListingCompleteness.Complete,
            0);
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, DriveKind.Network));
        TaskCompletionSource<DirectoryReadOutcome> pending = port.EnqueuePending();
        PaneSession session = PaneListingTestContext.CreateSession(port);
        PaneSnapshot listed = await session.NavigateAsync(listing.Location, CancellationToken.None);
        Task<PaneSnapshot> navigation = session.NavigateAsync(PaneListingTestContext.ParsePath("C:\\next"), CancellationToken.None);
        PaneSnapshot loading = session.Current;
        pending.SetResult(DirectoryReadOutcome.Cancelled());
        _ = await navigation;
        PanePresentation initial = PaneListingPresenter.Present(listed, PaneFrame.Active, TestMetadataFormats.Utc);

        PanePresentation updated = PaneListingPresenter.Present(loading, PaneFrame.Active, TestMetadataFormats.Utc, initial);

        Assert.AreSame(initial.Rows, updated.Rows);
        Assert.AreSame(initial.Rows[0], updated.Rows[0]);
        Assert.AreSame(initial.Rows[1], updated.Rows[1]);
        Assert.AreSame(PaneStatus.Loading, updated.Status);
        Assert.AreSame(PaneLocationStatus.NetworkDrive, updated.LocationStatus);
    }

    /// <summary>Proves focus movement replaces only the previous and current focus rows.</summary>
    [TestMethod]
    public async Task PresentWhenFocusMovesReplacesOnlyAffectedRows()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = PaneListingTestContext.CreateListing(
            "C:\\projects",
            ["a.txt", "b.txt", "c.txt"],
            DirectoryListingCompleteness.Complete,
            0);
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        PaneSession session = PaneListingTestContext.CreateSession(port);
        PaneSnapshot listed = await session.NavigateAsync(listing.Location, CancellationToken.None);
        PanePresentation initial = PaneListingPresenter.Present(listed, PaneFrame.Active, TestMetadataFormats.Utc);
        PaneRow priorFocus = initial.Rows[0];
        PaneRow priorNext = initial.Rows[1];
        PaneRow unaffected = initial.Rows[2];
        PaneSnapshot moved = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);

        PanePresentation updated = PaneListingPresenter.Present(moved, PaneFrame.Active, TestMetadataFormats.Utc, initial);

        Assert.AreSame(initial.Rows, updated.Rows);
        Assert.AreNotSame(priorFocus, updated.Rows[0]);
        Assert.AreNotSame(priorNext, updated.Rows[1]);
        Assert.AreSame(unaffected, updated.Rows[2]);
        Assert.AreSame(PaneRowMark.Unmarked, updated.Rows[0].Mark);
        Assert.AreSame(PaneRowMark.FocusInActivePane, updated.Rows[1].Mark);
    }

    /// <summary>
    /// Proves each row carries its size and local modification texts: a file shows its size, a
    /// directory shows no size, and an unmeasured file shows the unknown glyph for both.
    /// </summary>
    [TestMethod]
    public async Task PresentWhenEntriesCarryMetadataProjectsSizeAndModifiedTexts()
    {
        PaneSnapshot snapshot = await PaneListingTestContext.ListAsync(PaneListingTestContext.CreateMeasuredListing("C:\\projects"));
        EntryMetadataFormat format = TestMetadataFormats.Create(TestMetadataFormats.PlusNine);

        PanePresentation presentation = PaneListingPresenter.Present(snapshot, PaneFrame.Active, format);

        PaneRow file = PaneListingTestContext.RowNamed(presentation, "a.txt");
        PaneRow directory = PaneListingTestContext.RowNamed(presentation, "b");
        PaneRow unmeasured = PaneListingTestContext.RowNamed(presentation, "c.txt");
        Assert.AreEqual("812 B", file.SizeText);
        Assert.AreEqual("2026-10-09 23:59", file.ModifiedText);
        Assert.AreEqual(string.Empty, directory.SizeText);
        Assert.AreEqual("2026-10-09 23:59", directory.ModifiedText);
        Assert.AreEqual("\u2014", unmeasured.SizeText);
        Assert.AreEqual("\u2014", unmeasured.ModifiedText);
    }

    /// <summary>
    /// Proves a different metadata format re-projects every row of an unchanged snapshot with the
    /// new texts, while the same format still reuses the whole presentation.
    /// </summary>
    [TestMethod]
    public async Task PresentWhenFormatChangesReprojectsRowsWithNewTexts()
    {
        PaneSnapshot snapshot = await PaneListingTestContext.ListAsync(PaneListingTestContext.CreateMeasuredListing("C:\\projects"));
        EntryMetadataFormat utc = TestMetadataFormats.Utc;
        EntryMetadataFormat behind = TestMetadataFormats.Create(TestMetadataFormats.MinusFiveThirty);
        PanePresentation initial = PaneListingPresenter.Present(snapshot, PaneFrame.Active, utc);

        PanePresentation repeated = PaneListingPresenter.Present(snapshot, PaneFrame.Active, utc, initial);
        PanePresentation reformatted = PaneListingPresenter.Present(snapshot, PaneFrame.Active, behind, initial);

        Assert.AreSame(initial, repeated);
        Assert.AreNotSame(initial.Rows, reformatted.Rows);
        Assert.AreEqual("2026-10-09 14:59", PaneListingTestContext.RowNamed(initial, "a.txt").ModifiedText);
        Assert.AreEqual("2026-10-09 09:29", PaneListingTestContext.RowNamed(reformatted, "a.txt").ModifiedText);
        Assert.AreSame(PaneRowMark.FocusInActivePane, reformatted.Rows[0].Mark);
        Assert.AreSame(reformatted.Rows[0], reformatted.FocusRow);
    }

    /// <summary>
    /// Proves a pane without a listing builds a new empty row source for a different format and
    /// keeps its empty source for the same format.
    /// </summary>
    [TestMethod]
    public void PresentWhenNothingIsListedAndFormatChangesBuildsNewEmptyRows()
    {
        EntryMetadataFormat behind = TestMetadataFormats.Create(TestMetadataFormats.MinusFiveThirty);
        PanePresentation initial = PaneListingPresenter.Present(PaneSnapshot.Initial, PaneFrame.Active, TestMetadataFormats.Utc);

        PanePresentation passive = PaneListingPresenter.Present(PaneSnapshot.Initial, PaneFrame.Passive, TestMetadataFormats.Utc, initial);
        PanePresentation reformatted = PaneListingPresenter.Present(PaneSnapshot.Initial, PaneFrame.Active, behind, initial);

        Assert.AreSame(initial.Rows, passive.Rows);
        Assert.AreNotSame(initial.Rows, reformatted.Rows);
        Assert.IsEmpty(reformatted.Rows);
    }

    /// <summary>Proves a focus change keeps the size and modification texts of the replaced rows.</summary>
    [TestMethod]
    public async Task PresentWhenFocusMovesKeepsMetadataTextsOfReplacedRows()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = PaneListingTestContext.CreateMeasuredListing("C:\\projects");
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        PaneSession session = PaneListingTestContext.CreateSession(port);
        PaneSnapshot listed = await session.NavigateAsync(listing.Location, CancellationToken.None);
        PanePresentation initial = PaneListingPresenter.Present(listed, PaneFrame.Active, TestMetadataFormats.Utc);
        PaneRow[] before = [.. initial.Rows];
        PaneSnapshot moved = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);

        PanePresentation updated = PaneListingPresenter.Present(moved, PaneFrame.Active, TestMetadataFormats.Utc, initial);

        Assert.AreSame(initial.Rows, updated.Rows);
        Assert.AreNotSame(before[0], updated.Rows[0]);
        Assert.AreNotSame(before[1], updated.Rows[1]);
        for (int index = 0; index < before.Length; index++)
        {
            Assert.AreSame(before[index].Entry, updated.Rows[index].Entry);
            Assert.AreSame(before[index].Kind, updated.Rows[index].Kind);
            Assert.AreSame(before[index].Visibility, updated.Rows[index].Visibility);
            Assert.AreEqual(before[index].SizeText, updated.Rows[index].SizeText);
            Assert.AreEqual(before[index].ModifiedText, updated.Rows[index].ModifiedText);
        }
    }
}
