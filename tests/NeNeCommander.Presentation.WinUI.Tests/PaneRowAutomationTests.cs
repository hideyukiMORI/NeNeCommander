using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves file-list-scoped row identifiers survive presentation transitions without changing entry names.</summary>
[TestClass]
public sealed class PaneRowAutomationTests
{
    /// <summary>Proves exact names remain distinguishable, including case-sensitive WSL siblings and punctuation.</summary>
    [TestMethod]
    [DataRow("C:\\root")]
    [DataRow("\\\\server\\share\\root")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home")]
    public async Task PresentAsyncWhenNamesContainUnicodeAndSymbolsPreservesExactIdentifiers(string location)
    {
        DirectoryListing listing = CreateListing(location, ["Report.TXT", "\u8cc7\u6599 [1] #\u00e9.txt"]);
        PaneSession session = CreateSession(listing);

        PanePresentation presentation = Present(await session.NavigateAsync(listing.Location, CancellationToken.None));

        Assert.AreEqual("FileListRow_Report.TXT", RowNamed(presentation, "Report.TXT").AutomationId);
        Assert.AreEqual("FileListRow_\u8cc7\u6599 [1] #\u00e9.txt", RowNamed(presentation, "\u8cc7\u6599 [1] #\u00e9.txt").AutomationId);
    }

    /// <summary>Proves case-sensitive WSL siblings receive different identifiers in the same file list.</summary>
    [TestMethod]
    public async Task PresentAsyncWhenWslNamesDifferOnlyByCaseKeepsDistinctIdentifiers()
    {
        DirectoryListing listing = CreateListing("\\\\wsl.localhost\\Ubuntu\\home", ["Name", "name"]);
        PaneSession session = CreateSession(listing);

        PanePresentation presentation = Present(await session.NavigateAsync(listing.Location, CancellationToken.None));

        Assert.HasCount(2, presentation.Rows);
        Assert.AreEqual("FileListRow_Name", RowNamed(presentation, "Name").AutomationId);
        Assert.AreEqual("FileListRow_name", RowNamed(presentation, "name").AutomationId);
    }

    /// <summary>Proves incremental focus, selection and activation updates preserve identifiers while replacing marked rows.</summary>
    [TestMethod]
    public async Task PresentAsyncWhenMarksChangeKeepsIdentifiersOnReplacedAndReusedRows()
    {
        DirectoryListing listing = CreateListing("C:\\root", ["a.txt", "b.txt", "c.txt"]);
        PaneSession session = CreateSession(listing);
        PanePresentation initial = Present(await session.NavigateAsync(listing.Location, CancellationToken.None));
        PaneRow original = initial.Rows[0];
        PaneRow untouched = initial.Rows[2];

        PaneSnapshot moved = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);
        PanePresentation focused = PaneListingPresenter.Present(moved, PaneFrame.Active, TestMetadataFormats.Utc, initial);
        PaneSnapshot selected = await session.HandleAsync(UserIntent.ToggleSelection, CancellationToken.None);
        PanePresentation marked = PaneListingPresenter.Present(selected, PaneFrame.Active, TestMetadataFormats.Utc, focused);
        PanePresentation passive = PaneListingPresenter.Present(selected, PaneFrame.Passive, TestMetadataFormats.Utc, marked);

        Assert.AreSame(initial.Rows, passive.Rows);
        Assert.AreNotSame(original, passive.Rows[0]);
        Assert.AreSame(untouched, passive.Rows[2]);
        Assert.AreEqual(original.AutomationId, passive.Rows[0].AutomationId);
        Assert.AreEqual("FileListRow_b.txt", passive.Rows[1].AutomationId);
        Assert.AreSame(PaneRowMark.Selected, passive.Rows[1].Mark);
        Assert.AreSame(PaneRowMark.Unmarked, passive.Rows[0].Mark);
    }

    /// <summary>Proves changing sort and refreshing rebuild rows without changing their identifiers or entry names.</summary>
    [TestMethod]
    public async Task PresentAsyncWhenSortAndListingChangeKeepsIdentifiersWithEntries()
    {
        DirectoryListing listing = CreateListing("C:\\root", ["a.zip", "b.txt"]);
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing));
        port.Enqueue(DirectoryReadOutcome.Succeeded(CreateListing("C:\\root", ["b.txt", "a.zip"])));
        PaneSession session = CreateSession(port);
        PanePresentation initial = Present(await session.NavigateAsync(listing.Location, CancellationToken.None));
        PaneRow original = RowNamed(initial, "a.zip");

        PaneSnapshot sorted = await session.HandleAsync(UserIntent.SortByExtension, CancellationToken.None);
        PanePresentation reordered = PaneListingPresenter.Present(sorted, PaneFrame.Active, TestMetadataFormats.Utc, initial);
        PanePresentation refreshed = PaneListingPresenter.Present(
            await session.RefreshAsync(CancellationToken.None), PaneFrame.Active, TestMetadataFormats.Utc, reordered);

        Assert.AreNotSame(initial.Rows, reordered.Rows);
        Assert.AreNotSame(reordered.Rows, refreshed.Rows);
        Assert.AreEqual("b.txt", reordered.Rows[0].Entry.Name);
        Assert.AreEqual(original.AutomationId, RowNamed(reordered, "a.zip").AutomationId);
        Assert.AreEqual(original.AutomationId, RowNamed(refreshed, "a.zip").AutomationId);
        Assert.AreEqual("a.zip", RowNamed(refreshed, "a.zip").Entry.Name);
    }

    private static PanePresentation Present(PaneSnapshot snapshot)
    {
        return PaneListingPresenter.Present(snapshot, PaneFrame.Active, TestMetadataFormats.Utc);
    }

    private static PaneRow RowNamed(PanePresentation presentation, string name)
    {
        return presentation.Rows.Single(row => row.Entry.Name == name);
    }

    private static PaneSession CreateSession(DirectoryListing listing)
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing));
        return CreateSession(port);
    }

    private static PaneSession CreateSession(IDirectoryReadPort port)
    {
        VisiblePageCapacity capacity = Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(
            VisiblePageCapacity.Create(4)).Capacity;
        return new PaneSession(port, new AcceptedFileLauncher(), PaneSessionConfiguration.Create(
            capacity, DirectoryListing.EntryBoundaryLimit, HiddenItemVisibility.Hidden, PaneSortOrder.Default));
    }

    private static DirectoryListing CreateListing(string location, string[] names)
    {
        FileSystemPath root = Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(location)).Path;
        DirectoryEntry[] entries = [.. names.Select(name => DirectoryEntry.Create(
            Assert.IsInstanceOfType<PathParseSuccess>(root.Child(name)).Path,
            name,
            DirectoryEntryKind.File,
            EntryMetadata.Unmeasured(EntryVisibility.Normal)))];
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(DirectoryListing.Create(
            root, entries, DirectoryListingCompleteness.Complete, 0)).Listing;
    }
}
