using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves public entry reads cannot invalidate the retained row projection.</summary>
[TestClass]
public sealed class PaneVisibleEntryPresentationTests
{
    /// <summary>Proves visible entry ownership preserves row reuse and focus updates.</summary>
    [TestMethod]
    public async Task PresentAsyncWhenEntriesAreInspectedRetainsRowsAndMovesFocus()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = PaneListingTestContext.CreateListing("C:\\visible",
            ["a.txt", "b.txt", "c.txt"], DirectoryListingCompleteness.Complete, 0);
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        PaneSession session = PaneListingTestContext.CreateSession(port);
        PaneSnapshot original = await session.NavigateAsync(listing.Location, CancellationToken.None);
        PaneState state = Assert.IsInstanceOfType<PaneContentListed>(original.Content).State;
        PanePresentation initial = PaneListingPresenter.Present(original, PaneFrame.Active, TestMetadataFormats.Utc);
        PaneRow first = initial.Rows[0];
        PaneRow second = initial.Rows[1];
        PaneRow third = initial.Rows[2];

        if (state.VisibleEntries is ICollection collection &&
            collection.SyncRoot is ICollection<DirectoryEntry> mutable && !mutable.IsReadOnly)
        {
            mutable.Clear();
        }
        PaneSnapshot moved = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);
        PanePresentation updated = PaneListingPresenter.Present(moved, PaneFrame.Active, TestMetadataFormats.Utc, initial);

        Assert.HasCount(3, state.VisibleEntries);
        Assert.HasCount(3, updated.Rows);
        Assert.AreSame(initial.Rows, updated.Rows);
        Assert.AreNotSame(first, updated.Rows[0]);
        Assert.AreNotSame(second, updated.Rows[1]);
        Assert.AreSame(third, updated.Rows[2]);
        Assert.AreSame(PaneRowMark.Unmarked, updated.Rows[0].Mark);
        Assert.AreSame(PaneRowMark.FocusInActivePane, updated.Rows[1].Mark);
        Assert.AreSame(updated.Rows[1], updated.FocusRow);
        Assert.AreSame(listing.Entries[0].Path, state.FocusItem);
    }
}
