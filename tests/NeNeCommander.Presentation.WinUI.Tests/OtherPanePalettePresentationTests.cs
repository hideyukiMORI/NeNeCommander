using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Presentation.WinUI.Commands;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves opposite-pane palette rows identify their actual destination in visible and accessible text.</summary>
[TestClass]
public sealed class OtherPanePalettePresentationTests
{
    /// <summary>Proves both directions name the destination without changing the target wording of other commands.</summary>
    [TestMethod]
    public Task PresentWhenOppositeCommandIsListedNamesTheOtherTargetLeftAsync()
    {
        return AssertWhenOppositeCommandIsListedNamesTheOtherTargetAsync(PaneSide.Left);
    }

    /// <summary>Proves the same boundary for the other explicitly named case.</summary>
    [TestMethod]
    public Task PresentWhenOppositeCommandIsListedNamesTheOtherTargetRightAsync()
    {
        return AssertWhenOppositeCommandIsListedNamesTheOtherTargetAsync(PaneSide.Right);
    }

    private static async Task AssertWhenOppositeCommandIsListedNamesTheOtherTargetAsync(PaneSide activeSide)
    {
        bool leftActive = activeSide == PaneSide.Left;
        CommandPaletteOpen template = await CommandPaletteTestContext.OpenPaletteAsync(null);
        CommandPaletteOpen open = CommandPaletteTestContext.CreateOpen(template, leftActive ? PaneSide.Left : PaneSide.Right, template.Candidates);

        CommandPaletteViewState view = CommandPalettePresenter.Present(open, CommandPaletteTestContext.ProjectResource);

        CommandPaletteRow row = view.Rows.Single(item => item.Intent == UserIntent.OpenCurrentLocationInOtherPane);
        string target = leftActive ? "<CommandPaletteTargetRight>" : "<CommandPaletteTargetLeft>";
        string opposite = leftActive ? "<CommandPaletteOppositeLeft>" : "<CommandPaletteOppositeRight>";
        Assert.IsTrue(row.IsAvailable);
        Assert.AreEqual(target, row.Target);
        Assert.AreEqual(opposite, row.Opposite);
        Assert.AreEqual(target + " · " + opposite, row.Detail);
        Assert.AreEqual("<IntentLabelOpenCurrentLocationInOtherPane>; <KeyLabelCtrlE>; " + target + "; " + opposite, row.AutomationName);
        CommandPaletteRow ordinary = view.Rows.Single(item => item.Intent == UserIntent.Refresh);
        Assert.AreEqual(leftActive ? "<CommandPaletteTargetLeft>" : "<CommandPaletteTargetRight>", ordinary.Target);
        Assert.AreEqual(ordinary.Target, view.Target);
    }
    /// <summary>Proves selection, movement and filtering keep the palette header aligned with the actual command target.</summary>
    [TestMethod]
    public Task ContextWhenSelectionOrQueryChangesFollowsSelectedCommandTargetLeftAsync()
    {
        return AssertWhenSelectionOrQueryChangesFollowsSelectedCommandTargetAsync(PaneSide.Left);
    }

    /// <summary>Proves the same boundary for the other explicitly named case.</summary>
    [TestMethod]
    public Task ContextWhenSelectionOrQueryChangesFollowsSelectedCommandTargetRightAsync()
    {
        return AssertWhenSelectionOrQueryChangesFollowsSelectedCommandTargetAsync(PaneSide.Right);
    }

    private static async Task AssertWhenSelectionOrQueryChangesFollowsSelectedCommandTargetAsync(PaneSide activeSide)
    {
        bool leftActive = activeSide == PaneSide.Left;
        CommandPaletteOpen template = await CommandPaletteTestContext.OpenPaletteAsync(null);
        CommandPaletteOpen open = CommandPaletteTestContext.CreateOpen(template, leftActive ? PaneSide.Left : PaneSide.Right, template.Candidates);
        CommandPaletteViewState view = CommandPalettePresenter.Present(open, CommandPaletteTestContext.ProjectResource);
        CommandPaletteRow other = view.Rows.Single(item => item.Intent == UserIntent.OpenCurrentLocationInOtherPane);
        string activeContext = view.Context;
        string otherContext = other.Target + " · " + other.Opposite;

        Assert.IsTrue(view.Select(other));
        Assert.AreEqual(otherContext, view.Context);
        Assert.AreEqual(other.Target, view.Target);
        Assert.AreEqual(other.Opposite, view.Opposite);
        view.MovePrevious();
        Assert.AreEqual(activeContext, view.Context);
        view.MoveNext();
        Assert.AreEqual(otherContext, view.Context);
        view.UpdateQuery("KeyLabelCtrlE");
        Assert.HasCount(1, view.Rows);
        Assert.AreSame(other, view.SelectedRow);
        Assert.AreEqual(otherContext, view.Context);
        view.UpdateQuery("no-result");
        Assert.IsNull(view.SelectedRow);
        Assert.AreEqual(activeContext, view.Context);
        view.UpdateQuery(string.Empty);
        Assert.AreEqual(activeContext, view.Context);
    }
}
