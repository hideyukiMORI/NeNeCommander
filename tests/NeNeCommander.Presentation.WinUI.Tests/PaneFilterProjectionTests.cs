using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves filtered rows retain exact visible ownership when incremental projections are reused.</summary>
[TestClass]
public sealed class PaneFilterProjectionTests
{
    /// <summary>Proves equal-sized results with different members rebuild rows rather than exposing stale entries.</summary>
    [TestMethod]
    public async Task PresentWhenQueryChangesToDifferentSameSizeSetUsesCurrentOwnedRowsAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        PaneFilterEditing editor = Assert.IsInstanceOfType<PaneFilterEditing>(
            (await fixture.HandleAsync(UserIntent.OpenPaneFilter)).Scopes.NameFilter);
        CommanderSnapshot first = await fixture.HandleAsync(UserIntent.ChangePaneFilterText(editor, "a"));
        PanePresentation before = PaneListingTestContext.Present(first.Panes.Left);
        editor = Assert.IsInstanceOfType<PaneFilterEditing>(first.Scopes.NameFilter);

        CommanderSnapshot second = await fixture.HandleAsync(UserIntent.ChangePaneFilterText(editor, "b"));
        PanePresentation after = PaneListingPresenter.Present(second.Panes.Left, PaneFrame.Active, TestMetadataFormats.Utc, before);

        Assert.HasCount(1, before.Rows);
        Assert.HasCount(1, after.Rows);
        Assert.AreEqual("a", before.Rows[0].Entry.Name);
        Assert.AreEqual("b", after.Rows[0].Entry.Name);
        Assert.AreNotSame(before.Rows, after.Rows);
        PaneContentListed listed = Assert.IsInstanceOfType<PaneContentListed>(second.Panes.Left.Content);
        Assert.AreSame(listed.State.VisibleEntries[0], after.Rows[0].Entry);
        Assert.AreSame(after.Rows[0], after.FocusRow);
    }
}
