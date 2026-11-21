using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves stable selection storage still updates focus-dependent row marks.</summary>
[TestClass]
public sealed class PaneSelectionPresentationTests
{
    /// <summary>Proves selected focus movement updates both affected rows while retaining other rows.</summary>
    [TestMethod]
    public async Task PresentWhenSelectionIsSharedUpdatesFocusMarks()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = PaneListingTestContext.CreateListing("C:\\selection",
            ["a.txt", "b.txt", "c.txt"], DirectoryListingCompleteness.Complete, 0);
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        PaneSession session = PaneListingTestContext.CreateSession(port);
        _ = await session.NavigateAsync(listing.Location, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.ToggleSelection, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.ToggleSelection, CancellationToken.None);
        PaneSnapshot selected = await session.HandleAsync(UserIntent.MovePrevious, CancellationToken.None);
        PanePresentation initial = PaneListingPresenter.Present(selected, PaneFrame.Active, TestMetadataFormats.Utc);
        PaneRow first = initial.Rows[0];
        PaneRow second = initial.Rows[1];
        PaneRow third = initial.Rows[2];
        PaneSnapshot moved = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);

        PanePresentation updated = PaneListingPresenter.Present(moved, PaneFrame.Active, TestMetadataFormats.Utc, initial);

        PaneState priorState = Assert.IsInstanceOfType<PaneContentListed>(selected.Content).State;
        PaneState movedState = Assert.IsInstanceOfType<PaneContentListed>(moved.Content).State;
        Assert.AreSame(priorState.Selection, movedState.Selection);
        Assert.HasCount(2, priorState.Selection);
        Assert.AreSame(initial.Rows, updated.Rows);
        Assert.AreNotSame(first, updated.Rows[0]);
        Assert.AreNotSame(second, updated.Rows[1]);
        Assert.AreSame(third, updated.Rows[2]);
        Assert.AreSame(PaneRowMark.Selected, updated.Rows[0].Mark);
        Assert.AreSame(PaneRowMark.FocusInActivePane, updated.Rows[1].Mark);
        Assert.AreSame(PaneRowMark.Unmarked, updated.Rows[2].Mark);
        Assert.AreSame(updated.Rows[1], updated.FocusRow);
    }
}
