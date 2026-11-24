using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Windowing;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves a transfer receipt can update only its one exact decided scope owner.</summary>
[TestClass]
public sealed class WindowTransferSessionTests
{
    /// <summary>Each effect classification replaces the planned owner once, preserving action and captured pane.</summary>
    [TestMethod]
    public void ReportTransferWhenOwnerIsCurrentRecordsEachEffectExactlyOnce()
    {
        foreach (WindowTransferEffect effect in new[] { WindowTransferEffect.NotApplied, WindowTransferEffect.Unconfirmed, WindowTransferEffect.Observed })
        {
            WindowAdjustmentSession session = new();
            WindowAdjustmentOpen incoming = Open(session);
            WindowAdjustmentPlanned planned = Adjust(session, incoming, WindowAdjustmentAction.NextDisplay);
            Assert.AreSame(planned.Owner, session.Current);
            Assert.AreNotSame(incoming, planned.Owner);

            WindowAdjustmentOpen reported = Assert.IsInstanceOfType<WindowAdjustmentOpen>(session.ReportTransfer(planned.Owner, effect));

            Assert.AreSame(PaneSide.Right, reported.ActiveSide);
            Assert.AreSame(effect, Assert.IsInstanceOfType<WindowTransferReported>(reported.Outcome).Effect);
            Assert.AreSame(WindowAdjustmentAction.NextDisplay, Assert.IsInstanceOfType<WindowTransferReported>(reported.Outcome).Action);
            Assert.AreSame(reported, session.ReportTransfer(planned.Owner, WindowTransferEffect.Observed));
            Assert.AreSame(reported, session.ReportTransfer(reported, WindowTransferEffect.Observed));
        }
    }

    /// <summary>Stale, value-equal and replaced owners cannot receive or redirect a result.</summary>
    [TestMethod]
    public void ReportTransferWhenOwnerIsStaleOrEqualButDifferentChangesNothing()
    {
        WindowAdjustmentSession session = new();
        WindowAdjustmentOpen incoming = Open(session);
        WindowAdjustmentPlanned first = Adjust(session, incoming, WindowAdjustmentAction.PreviousDisplay);
        WindowAdjustmentOpen equal = new(first.Owner.ActiveSide, first.Owner.Outcome);
        Assert.AreEqual(first.Owner, equal);
        Assert.AreSame(first.Owner, session.ReportTransfer(incoming, WindowTransferEffect.Observed));
        Assert.AreSame(first.Owner, session.ReportTransfer(equal, WindowTransferEffect.Observed));
        WindowAdjustmentPlanned next = Adjust(session, first.Owner, WindowAdjustmentAction.NextDisplay);
        Assert.AreSame(next.Owner, session.ReportTransfer(first.Owner, WindowTransferEffect.Unconfirmed));
        WindowAdjustmentOpen reported = Assert.IsInstanceOfType<WindowAdjustmentOpen>(session.ReportTransfer(next.Owner, WindowTransferEffect.NotApplied));
        Assert.AreSame(WindowAdjustmentAction.NextDisplay, Assert.IsInstanceOfType<WindowTransferReported>(reported.Outcome).Action);
    }

    /// <summary>Closing and reopening cannot let a late result restore a departed scope or its focus request.</summary>
    [TestMethod]
    public void ReportTransferWhenOwnerHasLeftDoesNotReopenOrReplaceNewScope()
    {
        WindowAdjustmentSession session = new();
        WindowAdjustmentPlanned planned = Adjust(session, Open(session), WindowAdjustmentAction.PreviousDisplay);
        WindowAdjustmentState closed = session.Leave(planned.Owner);
        Assert.AreSame(closed, session.ReportTransfer(planned.Owner, WindowTransferEffect.Unconfirmed));
        WindowAdjustmentOpen reopened = Open(session);
        Assert.AreSame(reopened, session.ReportTransfer(planned.Owner, WindowTransferEffect.Observed));
        Assert.AreSame(reopened, session.ReportTransfer(reopened, WindowTransferEffect.Observed));
    }

    /// <summary>Ordinary decisions and transfer refusals never accept an apply receipt.</summary>
    [TestMethod]
    public void ReportTransferWhenActionIsOrdinaryOrRefusedChangesNothing()
    {
        WindowAdjustmentSession session = new();
        WindowAdjustmentPlanned ordinary = Adjust(session, Open(session), WindowAdjustmentAction.MoveRight);
        Assert.AreSame(ordinary.Owner, session.ReportTransfer(ordinary.Owner, WindowTransferEffect.Observed));
        WindowAdjustmentPlanned refused = Assert.IsInstanceOfType<WindowAdjustmentPlanned>(session.Adjust(
            WindowAdjustmentRequest.Create(ordinary.Owner, WindowAdjustmentAction.NextDisplay,
                WindowTransferTestContext.Original, WindowTransferInput.Unavailable)));
        Assert.AreSame(refused.Owner, session.ReportTransfer(refused.Owner, WindowTransferEffect.NotApplied));
    }

    /// <summary>Qualified reporting through Commander preserves both pane snapshots and all unrelated scopes.</summary>
    [TestMethod]
    public async Task ReportWindowTransferWhenCommanderOwnsModePreservesPaneAndEditorStateAsync()
    {
        using CommanderWindowAdjustmentTestContext fixture = await CommanderWindowAdjustmentTestContext.ListedAsync();
        _ = await fixture.HandleAsync(UserIntent.OpenWindowAdjustment);
        CommanderSnapshot before = fixture.Session.Current;
        WindowAdjustmentPlanned planned = Assert.IsInstanceOfType<WindowAdjustmentPlanned>(fixture.Session.AdjustWindow(
            WindowAdjustmentRequest.Create(fixture.CurrentOpen(), WindowAdjustmentAction.PreviousDisplay,
                WindowTransferTestContext.Original, WindowTransferInput.Create(WindowTransferTestContext.Displays))));

        WindowAdjustmentOpen reported = Assert.IsInstanceOfType<WindowAdjustmentOpen>(
            fixture.Session.ReportWindowTransfer(planned.Owner, WindowTransferEffect.Observed));

        Assert.AreSame(WindowAdjustmentAction.PreviousDisplay, Assert.IsInstanceOfType<WindowTransferReported>(reported.Outcome).Action);
        CommanderWindowAdjustmentTestContext.AssertUnchanged(before, fixture.Session.Current);
        Assert.AreSame(reported, fixture.CurrentOpen());
    }

    /// <summary>The request and reporting entry reject missing expected owners, facts and effects.</summary>
    [TestMethod]
    public void ReportTransferWhenRequiredArgumentIsAbsentRejectsIt()
    {
        WindowAdjustmentSession session = new();
        WindowAdjustmentOpen open = Open(session);
        NullGuardAssertions.AssertInstanceNullGuard(session, "ReportTransfer", [null, WindowTransferEffect.Observed]);
        NullGuardAssertions.AssertInstanceNullGuard(session, "ReportTransfer", [open, null]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(WindowAdjustmentRequest), "Create",
            [open, WindowAdjustmentAction.NextDisplay, WindowTransferTestContext.Original, null]);
    }

    private static WindowAdjustmentOpen Open(WindowAdjustmentSession session)
    {
        return Assert.IsInstanceOfType<WindowAdjustmentOpen>(session.Open(PaneSide.Right, InteractionOwnership.ScopeOwnsInput));
    }

    private static WindowAdjustmentPlanned Adjust(WindowAdjustmentSession session, WindowAdjustmentOpen open, WindowAdjustmentAction action)
    {
        return Assert.IsInstanceOfType<WindowAdjustmentPlanned>(session.Adjust(WindowAdjustmentRequest.Create(
            open, action, WindowTransferTestContext.Original, WindowTransferInput.Create(WindowTransferTestContext.Displays))));
    }
}
