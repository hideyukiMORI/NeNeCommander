using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Commands;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves exact context acquisition precedes capture without reinterpreting command targets.</summary>
[TestClass]
public sealed class CommanderContextSelectionTests
{
    /// <summary>Proves a selected context row preserves the batch and anchor while changing focus.</summary>
    [TestMethod]
    [DataRow("left")]
    [DataRow("right")]
    public async Task HandleAsyncWhenSelectedRowOpensContextPreservesBatchAndAnchorAsync(string sideName)
    {
        PaneSide side = sideName == "left" ? PaneSide.Left : PaneSide.Right;
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        _ = await fixture.RowAsync(side, 0, PaneEntryAction.Select);
        _ = await fixture.RowAsync(side, 2, PaneEntryAction.ToggleSelection);
        CommanderSnapshot before = fixture.Session.Current;
        PaneState old = CommanderContextTestContext.StateOf(before.Panes.Of(side));

        CommanderSnapshot after = await fixture.RowAsync(side, 0, PaneEntryAction.OpenContextMenu);

        PaneState selected = CommanderContextTestContext.StateOf(after.Panes.Of(side));
        Assert.AreSame(side, after.Panes.ActiveSide);
        Assert.AreNotSame(before.Panes.Of(side), after.Panes.Of(side));
        Assert.AreSame(before.Panes.Of(side.Other), after.Panes.Of(side.Other));
        Assert.AreSame(old.VisibleEntries[0].Path, selected.FocusItem);
        Assert.AreSame(old.SelectionAnchor, selected.SelectionAnchor);
        CollectionAssert.AreEqual(old.Selection.ToArray(), selected.Selection.ToArray());
        Assert.AreSame(old.VisibleEntries[2].Path, old.FocusItem);
        Assert.AreSame(CommandSurface.ItemContext, CommanderContextTestContext.OpenOf(after).Surface);
    }

    /// <summary>Proves an unselected context row establishes one visibly selected target and anchor.</summary>
    [TestMethod]
    [DataRow("left")]
    [DataRow("right")]
    public async Task HandleAsyncWhenUnselectedRowOpensContextReplacesSelectionAsync(string sideName)
    {
        PaneSide side = sideName == "left" ? PaneSide.Left : PaneSide.Right;
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        _ = await fixture.RowAsync(side, 0, PaneEntryAction.Select);
        _ = await fixture.RowAsync(side, 2, PaneEntryAction.ToggleSelection);
        PaneState old = CommanderContextTestContext.StateOf(fixture.Session.Current.Panes.Of(side));

        CommanderSnapshot after = await fixture.RowAsync(side, 1, PaneEntryAction.OpenContextMenu);

        PaneState selected = CommanderContextTestContext.StateOf(after.Panes.Of(side));
        Assert.AreSame(side, after.Panes.ActiveSide);
        Assert.AreSame(old.VisibleEntries[1].Path, selected.FocusItem);
        Assert.AreSame(selected.FocusItem, selected.SelectionAnchor);
        Assert.HasCount(1, selected.Selection);
        Assert.AreSame(selected.FocusItem, selected.Selection[0]);
        Assert.HasCount(2, old.Selection);
        Assert.AreSame(CommandSurface.ItemContext, CommanderContextTestContext.OpenOf(after).Surface);
    }

    /// <summary>Proves blank/header acquisition preserves marks while exposing only folder commands.</summary>
    [TestMethod]
    [DataRow("left")]
    [DataRow("right")]
    public async Task HandleAsyncWhenFolderContextOpensPreservesModelAndOmitsItemCommandsAsync(string sideName)
    {
        PaneSide side = sideName == "left" ? PaneSide.Left : PaneSide.Right;
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        _ = await fixture.RowAsync(side, 2, PaneEntryAction.Select);
        _ = await fixture.HandleAsync(UserIntent.ActivateOtherPane);
        CommanderSnapshot before = fixture.Session.Current;

        CommanderSnapshot after = await fixture.FolderAsync(side);

        Assert.AreSame(side, after.Panes.ActiveSide);
        Assert.AreNotSame(before.Panes.Of(side), after.Panes.Of(side));
        Assert.AreSame(before.Panes.Of(side).Content, after.Panes.Of(side).Content);
        Assert.AreSame(before.Panes.Of(side).Activity, after.Panes.Of(side).Activity);
        CommandPaletteOpen open = CommanderContextTestContext.OpenOf(after);
        Assert.AreSame(CommandSurface.FolderContext, open.Surface);
        UserIntent[] expected = [UserIntent.Refresh, UserIntent.CreateDirectory,
            UserIntent.CreateTextFile, UserIntent.OpenBookmarks, UserIntent.RevealLocation];
        CollectionAssert.AreEqual(expected, open.Candidates.Select(candidate => candidate.Intent).ToArray());
    }

    /// <summary>Proves keyboard context captures distinct focus and batch without selecting the focus.</summary>
    [TestMethod]
    [DataRow("left")]
    [DataRow("right")]
    public async Task HandleAsyncWhenKeyboardFocusIsOutsideBatchPreservesBothTargetsAsync(string sideName)
    {
        PaneSide side = sideName == "left" ? PaneSide.Left : PaneSide.Right;
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        _ = await fixture.RowAsync(side, 0, PaneEntryAction.Select);
        _ = await fixture.HandleAsync(UserIntent.MoveNext);
        CommanderSnapshot before = fixture.Session.Current;

        CommanderSnapshot after = await fixture.HandleAsync(UserIntent.OpenContextMenu);

        Assert.AreSame(side, after.Panes.ActiveSide);
        Assert.AreNotSame(before.Panes.Of(side), after.Panes.Of(side));
        Assert.AreSame(before.Panes.Of(side).Content, after.Panes.Of(side).Content);
        PaneState state = CommanderContextTestContext.StateOf(after.Panes.Of(side));
        Assert.AreSame(state.VisibleEntries[1].Path, state.FocusItem);
        Assert.AreSame(state.VisibleEntries[0].Path, state.Selection.Single());
        Assert.AreSame(CommandSurface.ItemContext, CommanderContextTestContext.OpenOf(after).Surface);
    }

    /// <summary>Proves an empty listed folder admits keyboard folder context without an invented focus.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenEmptyListedFolderOpensKeyboardContextUsesFolderSurfaceAsync()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        DirectoryListing listing = CommanderSessionTestValues.EmptyListing("C:\\empty");
        await fixture.Panes.ListBothAsync(listing, listing);

        CommanderSnapshot after = await fixture.HandleAsync(UserIntent.OpenContextMenu);

        Assert.AreSame(CommandSurface.FolderContext, CommanderContextTestContext.OpenOf(after).Surface);
        Assert.IsNull(CommanderContextTestContext.StateOf(after.Panes.Left).FocusItem);
    }

    /// <summary>Proves absent listings refuse every opening without activation or fabricated receipts.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenUnlistedRefusesKeyboardAndFolderContextAsync()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        CommanderSnapshot before = fixture.Session.Current;

        _ = await fixture.HandleAsync(UserIntent.OpenContextMenu);
        CommanderSnapshot after = await fixture.FolderAsync(PaneSide.Right);

        Assert.AreSame(before.Panes.Left, after.Panes.Left);
        Assert.AreSame(before.Panes.Right, after.Panes.Right);
        Assert.AreSame(PaneSide.Left, after.Panes.ActiveSide);
        Assert.AreSame(CommandPaletteState.Closed, after.Scopes.CommandPalette);
        Assert.IsEmpty(fixture.Panes.Left.Requests);
        Assert.IsEmpty(fixture.Panes.Right.Requests);
    }

    /// <summary>Proves retained listed cancellation survives context acquisition with no implicit read.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenReadWasCancelledRetainsActivityAndAllowsListedContextAsync()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        fixture.Panes.Right.Enqueue(DirectoryReadOutcome.Cancelled());
        _ = await fixture.Panes.RightSession.RefreshAsync(CancellationToken.None);
        PaneSnapshot before = fixture.Session.Current.Panes.Right;

        CommanderSnapshot after = await fixture.FolderAsync(PaneSide.Right);

        _ = Assert.IsInstanceOfType<PaneReadCancelled>(before.Activity);
        Assert.AreSame(before.Activity, after.Panes.Right.Activity);
        Assert.AreSame(before.Content, after.Panes.Right.Content);
        Assert.AreSame(CommandSurface.FolderContext, CommanderContextTestContext.OpenOf(after).Surface);
        Assert.HasCount(2, fixture.Panes.Right.Requests);
    }
}
