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
        string detail = leftActive ? "<CommandPaletteOpenOtherRightDetail>" : "<CommandPaletteOpenOtherLeftDetail>";
        Assert.IsTrue(row.IsAvailable);
        Assert.AreEqual(detail, row.Detail);
        Assert.AreEqual("<IntentLabelOpenCurrentLocationInOtherPane>; <KeyLabelCtrlE>; " + detail, row.AutomationName);
        CommandPaletteRow ordinary = view.Rows.Single(item => item.Intent == UserIntent.Refresh);
        Assert.AreEqual(string.Empty, ordinary.Detail);
        StringAssert.Contains(ordinary.AutomationName, leftActive ? "<CommandPaletteTargetLeft>" : "<CommandPaletteTargetRight>");
        Assert.AreEqual(leftActive ? "<CommandPaletteContextLeft>" : "<CommandPaletteContextRight>", view.Context);
    }
    /// <summary>Proves selection and filtering preserve captured context while the selected detail names the destination.</summary>
    [TestMethod]
    public Task ContextWhenSelectionOrQueryChangesKeepsContextAndNamesDestinationLeftAsync()
    {
        return AssertWhenSelectionOrQueryChangesKeepsContextAndNamesDestinationAsync(PaneSide.Left);
    }

    /// <summary>Proves the same boundary for the other explicitly named case.</summary>
    [TestMethod]
    public Task ContextWhenSelectionOrQueryChangesKeepsContextAndNamesDestinationRightAsync()
    {
        return AssertWhenSelectionOrQueryChangesKeepsContextAndNamesDestinationAsync(PaneSide.Right);
    }

    private static async Task AssertWhenSelectionOrQueryChangesKeepsContextAndNamesDestinationAsync(PaneSide activeSide)
    {
        bool leftActive = activeSide == PaneSide.Left;
        CommandPaletteOpen template = await CommandPaletteTestContext.OpenPaletteAsync(null);
        CommandPaletteOpen open = CommandPaletteTestContext.CreateOpen(template, leftActive ? PaneSide.Left : PaneSide.Right, template.Candidates);
        CommandPaletteViewState view = CommandPalettePresenter.Present(open, CommandPaletteTestContext.ProjectResource);
        CommandPaletteRow other = view.Rows.Single(item => item.Intent == UserIntent.OpenCurrentLocationInOtherPane);
        string activeContext = view.Context;
        string otherDetail = leftActive ? "<CommandPaletteOpenOtherRightDetail>" : "<CommandPaletteOpenOtherLeftDetail>";

        Assert.IsTrue(view.Select(other));
        Assert.AreEqual(activeContext, view.Context);
        Assert.AreEqual(otherDetail, view.SelectedRow!.Detail);
        view.MovePrevious();
        Assert.AreEqual(activeContext, view.Context);
        Assert.AreNotSame(other, view.SelectedRow);
        view.MoveNext();
        Assert.AreSame(other, view.SelectedRow);
        Assert.AreEqual(otherDetail, view.SelectedRow!.Detail);
        Assert.AreEqual(activeContext, view.Context);
        view.UpdateQuery("KeyLabelCtrlE");
        Assert.HasCount(1, view.Rows);
        Assert.AreSame(other, view.SelectedRow);
        Assert.AreEqual(otherDetail, view.SelectedRow!.Detail);
        Assert.AreEqual(activeContext, view.Context);
        view.UpdateQuery("no-result");
        Assert.IsNull(view.SelectedRow);
        Assert.AreEqual(activeContext, view.Context);
        view.UpdateQuery(string.Empty);
        Assert.AreEqual(activeContext, view.Context);
    }
}
