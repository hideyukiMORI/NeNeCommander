using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves an exact native exit carries committed text before deferred text notification.</summary>
[TestClass]
public sealed class PaneFilterPendingTextTests
{
    /// <summary>Supplies the two exits which preserve committed native text.</summary>
    public static IEnumerable<object[]> PreservingExits()
    {
        yield return [PaneFilterEditAction.Commit];
        yield return [PaneFilterEditAction.Blur];
    }

    /// <summary>Proves pending native text is applied exactly once by the same reducer before closing.</summary>
    [TestMethod]
    [DynamicData(nameof(PreservingExits))]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-014")]
    public async Task HandleAsyncWhenExitPrecedesTextChangedAppliesItsCommittedNativeTextAsync(PaneFilterEditAction action)
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        _ = await fixture.HandleAsync(UserIntent.SelectAll);
        PaneFilterEditing editing = Assert.IsInstanceOfType<PaneFilterEditing>((await fixture.HandleAsync(UserIntent.OpenPaneFilter)).Scopes.NameFilter);

        CommanderSnapshot after = await fixture.HandleAsync(UserIntent.FinishPaneFilter(editing, action, " b.txt "));

        PaneState state = CommanderContextTestContext.StateOf(after.Panes.Left);
        Assert.AreEqual(" b.txt ", state.NameFilter.Text);
        Assert.HasCount(1, state.VisibleEntries);
        Assert.AreEqual("b.txt", state.VisibleEntries[0].Name);
        Assert.HasCount(1, state.Selection);
        PaneFilterClosed closed = Assert.IsInstanceOfType<PaneFilterClosed>(after.Scopes.NameFilter);
        Assert.AreSame(action == PaneFilterEditAction.Commit ? PaneSide.Left : null, closed.FileListFocusSide);
        Assert.HasCount(1, fixture.Panes.Left.Requests);
        CommanderSnapshot late = await fixture.HandleAsync(UserIntent.ChangePaneFilterText(editing, "late"));
        Assert.AreSame(after.Panes.Left, late.Panes.Left);
        Assert.AreSame(closed, late.Scopes.NameFilter);
    }

    /// <summary>Proves Escape ignores pending text and a late completion cannot modify a replacement editor.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenCancelPrecedesTextChangedClearsAndRejectsItsLateExitAsync()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        PaneFilterEditing old = Assert.IsInstanceOfType<PaneFilterEditing>((await fixture.HandleAsync(UserIntent.OpenPaneFilter)).Scopes.NameFilter);
        CommanderSnapshot cancelled = await fixture.HandleAsync(UserIntent.FinishPaneFilter(old, PaneFilterEditAction.Cancel, "pending"));
        PaneFilterEditing replacement = Assert.IsInstanceOfType<PaneFilterEditing>((await fixture.HandleAsync(UserIntent.OpenPaneFilter)).Scopes.NameFilter);

        CommanderSnapshot late = await fixture.HandleAsync(UserIntent.FinishPaneFilter(old, PaneFilterEditAction.Blur, "stale"));

        Assert.IsTrue(CommanderContextTestContext.StateOf(cancelled.Panes.Left).NameFilter.IsEmpty);
        Assert.AreSame(replacement, late.Scopes.NameFilter);
        Assert.AreSame(cancelled.Panes.Left, late.Panes.Left);
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => UserIntent.FinishPaneFilter(replacement, PaneFilterEditAction.Commit, null!));
    }
}
