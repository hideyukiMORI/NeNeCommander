using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Commands;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Presentation.WinUI.Commands;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves captured context and actual command scope remain distinct.</summary>
[TestClass]
public sealed class CommandPaletteScopeTests
{
    /// <summary>Ordinary pane commands name their target to assistive readers without repeating visible scope.</summary>
    [TestMethod]
    public async Task PresentWhenPaneCommandIsAvailableUsesCommonVisibleContextAsync()
    {
        CommandPaletteViewState view = await CreateViewAsync(PaneSide.Left);
        CommandPaletteRow row = view.Rows.Single(candidate => candidate.Intent == UserIntent.OpenFocused);

        Assert.AreEqual(string.Empty, row.Detail);
        StringAssert.Contains(row.AutomationName, "<CommandPaletteTargetLeft>");
        Assert.IsFalse(row.AutomationName.Contains("Opposite", System.StringComparison.Ordinal));
    }

    /// <summary>Application, window and shared-catalog entries do not invent a pane target.</summary>
    [TestMethod]
    public async Task PresentWhenCommandIsGlobalNamesItsActualScopeAsync()
    {
        CommandPaletteViewState view = await CreateViewAsync(PaneSide.Right);
        UserIntent[] intents = [UserIntent.OpenSettings, UserIntent.OpenColorSchemes,
            UserIntent.OpenHelp, UserIntent.OpenWindowAdjustment, UserIntent.OpenBookmarks];

        foreach (UserIntent intent in intents)
        {
            CommandPaletteRow row = view.Rows.Single(candidate => candidate.Intent == intent);
            Assert.IsFalse(row.AutomationName.Contains("Target", System.StringComparison.Ordinal), row.Title);
            Assert.IsFalse(row.AutomationName.Contains("Opposite", System.StringComparison.Ordinal), row.Title);
            Assert.IsFalse(string.IsNullOrEmpty(row.Detail), row.Title);
            StringAssert.Contains(row.AutomationName, row.Detail);
        }
    }

    /// <summary>Transfer direction follows the captured side, preserving a missing-destination explanation.</summary>
    [TestMethod]
    public async Task PresentWhenTransferIsUnavailableNamesDirectionAndReasonAsync()
    {
        CommandPaletteViewState left = await CreateViewAsync(PaneSide.Left);
        CommandPaletteViewState right = await CreateViewAsync(PaneSide.Right);
        CommandPaletteRow copy = left.Rows.Single(candidate => candidate.Intent == UserIntent.Copy);
        CommandPaletteRow move = right.Rows.Single(candidate => candidate.Intent == UserIntent.Move);

        StringAssert.Contains(copy.Detail, "<CommandPaletteTransferLeftToRight>");
        StringAssert.Contains(move.Detail, "<CommandPaletteTransferRightToLeft>");
        StringAssert.Contains(copy.Detail, "<CommandUnavailablePassivePane>");
        StringAssert.Contains(move.AutomationName, "<CommandUnavailablePassivePane>");
        Assert.IsFalse(copy.IsAvailable);
        Assert.IsFalse(move.IsAvailable);
    }

    /// <summary>Changing active pane names its destination, not the currently active pane as a target.</summary>
    [TestMethod]
    public async Task PresentWhenActivatingOtherPaneNamesTheDestinationAsync()
    {
        CommandPaletteViewState left = await CreateViewAsync(PaneSide.Left);
        CommandPaletteViewState right = await CreateViewAsync(PaneSide.Right);

        Assert.AreEqual("<CommandPaletteActivateRight>", left.Rows.Single(row => row.Intent == UserIntent.ActivateOtherPane).Detail);
        Assert.AreEqual("<CommandPaletteActivateLeft>", right.Rows.Single(row => row.Intent == UserIntent.ActivateOtherPane).Detail);
    }

    /// <summary>The header does not depend on which candidate remains after filtering.</summary>
    [TestMethod]
    public async Task UpdateQueryWhenOnlyGlobalOrNoRowsRemainKeepsCapturedContextAsync()
    {
        CommandPaletteViewState view = await CreateViewAsync(PaneSide.Right);
        view.UpdateQuery("IntentLabelOpenSettings");
        Assert.HasCount(1, view.Rows);
        Assert.AreEqual("<CommandPaletteContextRight>", view.Context);
        view.UpdateQuery("not-a-command");
        Assert.IsEmpty(view.Rows);
        Assert.AreEqual("<CommandPaletteContextRight>", view.Context);
    }

    /// <summary>Scope presentation cannot hide an unavailable outcome even on a global candidate.</summary>
    [TestMethod]
    public async Task PresentWhenGlobalCandidateIsUnavailableRetainsTheReasonAsync()
    {
        CommandPaletteOpen template = await CommandPaletteTestContext.OpenPaletteAsync(null);
        CommandCandidate candidate = CommandPaletteTestContext.CreateCandidate(UserIntent.OpenColorSchemes,
            CommandPaletteTestContext.CreateUnavailable(CommandUnavailableReason.FocusRequired));
        CommandPaletteOpen open = CommandPaletteTestContext.CreateOpen(template, PaneSide.Left, [candidate]);
        CommandPaletteRow row = CommandPalettePresenter.Present(open, CommandPaletteTestContext.ProjectResource).Rows[0];

        Assert.AreEqual("<CommandUnavailableFocusRequired>", row.Reason);
        Assert.AreEqual("<CommandPaletteUnavailableMarker>", row.StateMarker);
        StringAssert.Contains(row.Detail, row.Reason);
        StringAssert.Contains(row.AutomationName, row.Reason);
    }

    private static async Task<CommandPaletteViewState> CreateViewAsync(PaneSide side)
    {
        CommandPaletteOpen template = await CommandPaletteTestContext.OpenPaletteAsync(null);
        CommandPaletteOpen open = CommandPaletteTestContext.CreateOpen(template, side, template.Candidates);
        return CommandPalettePresenter.Present(open, CommandPaletteTestContext.ProjectResource);
    }
}
