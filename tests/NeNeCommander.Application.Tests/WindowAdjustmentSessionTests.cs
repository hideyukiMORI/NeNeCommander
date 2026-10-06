using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Windowing;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves the window-adjustment scope owner: admission, qualified decisions, and qualified leave.</summary>
[TestClass]
public sealed class WindowAdjustmentSessionTests
{
    /// <summary>Proves the initial state is the shared closed state with no focus request.</summary>
    [TestMethod]
    public void CurrentWhenNothingHappenedIsClosedWithoutFocusRequest()
    {
        WindowAdjustmentSession session = new();

        Assert.AreSame(WindowAdjustmentState.Closed, session.Current);
        Assert.IsNull(Assert.IsInstanceOfType<WindowAdjustmentClosed>(session.Current).FileListFocusSide);
    }

    /// <summary>Proves an owned open captures only the active side and starts with no outcome.</summary>
    [TestMethod]
    public void OpenWhenScopeOwnsInputCapturesActiveSideWithNoOutcome()
    {
        WindowAdjustmentSession session = new();

        WindowAdjustmentOpen open = Assert.IsInstanceOfType<WindowAdjustmentOpen>(
            session.Open(PaneSide.Right, InteractionOwnership.ScopeOwnsInput));

        Assert.AreSame(open, session.Current);
        Assert.AreSame(PaneSide.Right, open.ActiveSide);
        Assert.AreSame(WindowAdjustmentOutcome.None, open.Outcome);
    }

    /// <summary>Proves an open while another scope owns input leaves the mode closed.</summary>
    [TestMethod]
    public void OpenWhenAnotherScopeOwnsInputStaysClosed()
    {
        WindowAdjustmentSession session = new();

        WindowAdjustmentState refused = session.Open(PaneSide.Left, InteractionOwnership.AnotherScopeOwnsInput);

        Assert.AreSame(WindowAdjustmentState.Closed, refused);
        Assert.AreSame(WindowAdjustmentState.Closed, session.Current);
    }

    /// <summary>Proves a second open keeps the current instance and its outcome instead of replacing them.</summary>
    [TestMethod]
    public void OpenWhenAlreadyOpenKeepsTheCurrentInstance()
    {
        WindowAdjustmentSession session = new();
        WindowAdjustmentOpen first = Opened(session, PaneSide.Left);
        _ = session.Adjust(Request(first, WindowAdjustmentAction.MoveLeft));
        WindowAdjustmentState adjusted = session.Current;

        WindowAdjustmentState again = session.Open(PaneSide.Right, InteractionOwnership.ScopeOwnsInput);

        Assert.AreSame(adjusted, again);
        Assert.AreSame(PaneSide.Left, Assert.IsInstanceOfType<WindowAdjustmentOpen>(again).ActiveSide);
    }

    /// <summary>Proves a qualified action returns the planner's plan and records it as the new outcome.</summary>
    [TestMethod]
    public void AdjustWhenExpectedStateIsCurrentReturnsThePlanAndRecordsItsOutcome()
    {
        WindowAdjustmentSession session = new();
        WindowAdjustmentOpen open = Opened(session, PaneSide.Right);
        WindowPlacement placement = Placement(WindowPresenterState.Restored);

        WindowAdjustmentPlanned planned = Assert.IsInstanceOfType<WindowAdjustmentPlanned>(
            session.Adjust(WindowAdjustmentRequest.Create(open, WindowAdjustmentAction.MoveRight, placement)));

        WindowMovePlan move = Assert.IsInstanceOfType<WindowMovePlan>(planned.Plan);
        Assert.AreEqual(WindowBounds.Create(132, 100, 800, 600), move.Bounds);
        WindowAdjustmentOpen next = Assert.IsInstanceOfType<WindowAdjustmentOpen>(session.Current);
        Assert.AreNotSame(open, next);
        Assert.AreSame(PaneSide.Right, next.ActiveSide);
        Assert.AreSame(
            WindowAdjustmentAction.MoveRight,
            Assert.IsInstanceOfType<WindowActionPlanned>(next.Outcome).Action);
    }

    /// <summary>Proves a refused plan is returned as a decision and recorded as a refused outcome.</summary>
    [TestMethod]
    public void AdjustWhenPlannerRefusesRecordsTheRefusalReason()
    {
        WindowAdjustmentSession session = new();
        WindowAdjustmentOpen open = Opened(session, PaneSide.Left);

        WindowAdjustmentPlanned planned = Assert.IsInstanceOfType<WindowAdjustmentPlanned>(session.Adjust(
            WindowAdjustmentRequest.Create(
                open,
                WindowAdjustmentAction.Restore,
                Placement(WindowPresenterState.Restored))));

        Assert.AreSame(
            WindowAdjustmentRefusal.WindowIsRestored,
            Assert.IsInstanceOfType<WindowRefusedPlan>(planned.Plan).Reason);
        WindowAdjustmentOpen next = Assert.IsInstanceOfType<WindowAdjustmentOpen>(session.Current);
        Assert.AreSame(
            WindowAdjustmentRefusal.WindowIsRestored,
            Assert.IsInstanceOfType<WindowActionRefused>(next.Outcome).Reason);
    }

    /// <summary>Proves an action qualified by an earlier instance is a no-op that keeps the current mode.</summary>
    [TestMethod]
    public void AdjustWhenExpectedStateIsStaleReturnsNothingToApplyAndKeepsTheMode()
    {
        WindowAdjustmentSession session = new();
        WindowAdjustmentOpen first = Opened(session, PaneSide.Left);
        _ = session.Adjust(Request(first, WindowAdjustmentAction.MoveDown));
        WindowAdjustmentState current = session.Current;

        WindowAdjustmentDecision stale = session.Adjust(Request(first, WindowAdjustmentAction.Maximize));

        Assert.AreSame(WindowAdjustmentDecision.NothingToApply, stale);
        Assert.AreSame(current, session.Current);
    }

    /// <summary>Proves an action after leaving is a no-op that does not reopen the mode.</summary>
    [TestMethod]
    public void AdjustWhenModeIsClosedReturnsNothingToApply()
    {
        WindowAdjustmentSession session = new();
        WindowAdjustmentOpen open = Opened(session, PaneSide.Left);
        WindowAdjustmentState closed = session.Leave(open);

        WindowAdjustmentDecision decision = session.Adjust(Request(open, WindowAdjustmentAction.MoveUp));

        Assert.AreSame(WindowAdjustmentDecision.NothingToApply, decision);
        Assert.AreSame(closed, session.Current);
    }

    /// <summary>Proves a qualified leave closes the mode and requests focus for the side captured at entry.</summary>
    [TestMethod]
    public void LeaveWhenExpectedStateIsCurrentClosesAndRequestsFocusForTheCapturedSide()
    {
        WindowAdjustmentSession session = new();
        WindowAdjustmentOpen open = Opened(session, PaneSide.Right);
        _ = session.Adjust(Request(open, WindowAdjustmentAction.Shrink));
        WindowAdjustmentOpen adjusted = Assert.IsInstanceOfType<WindowAdjustmentOpen>(session.Current);

        WindowAdjustmentState left = session.Leave(adjusted);

        WindowAdjustmentClosed closed = Assert.IsInstanceOfType<WindowAdjustmentClosed>(left);
        Assert.AreSame(PaneSide.Right, closed.FileListFocusSide);
        Assert.AreSame(closed, session.Current);
        Assert.AreNotSame(WindowAdjustmentState.Closed, closed);
    }

    /// <summary>Proves a leave qualified by an earlier instance neither closes nor replaces the mode.</summary>
    [TestMethod]
    public void LeaveWhenExpectedStateIsStaleKeepsTheCurrentMode()
    {
        WindowAdjustmentSession session = new();
        WindowAdjustmentOpen first = Opened(session, PaneSide.Left);
        _ = session.Adjust(Request(first, WindowAdjustmentAction.Enlarge));
        WindowAdjustmentState current = session.Current;

        WindowAdjustmentState result = session.Leave(first);

        Assert.AreSame(current, result);
        Assert.AreSame(current, session.Current);
    }

    /// <summary>Proves a repeated leave keeps the first closed state and its single focus request.</summary>
    [TestMethod]
    public void LeaveWhenAlreadyClosedKeepsTheFirstClosedState()
    {
        WindowAdjustmentSession session = new();
        WindowAdjustmentOpen open = Opened(session, PaneSide.Left);
        WindowAdjustmentState closed = session.Leave(open);

        WindowAdjustmentState again = session.Leave(open);

        Assert.AreSame(closed, again);
    }

    /// <summary>Proves the owner and its values reject every absent part.</summary>
    [TestMethod]
    public void InvokeWhenAPartIsNullThrowsArgumentNullException()
    {
        WindowAdjustmentSession session = new();
        WindowAdjustmentOpen open = Opened(session, PaneSide.Left);
        WindowPlacement placement = Placement(WindowPresenterState.Restored);

        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => session.Open(null!, InteractionOwnership.ScopeOwnsInput));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => session.Open(PaneSide.Left, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => session.Adjust(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => session.Leave(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => WindowAdjustmentRequest.Create(null!, WindowAdjustmentAction.MoveLeft, placement));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => WindowAdjustmentRequest.Create(open, null!, placement));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => WindowAdjustmentRequest.Create(open, WindowAdjustmentAction.MoveLeft, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => new WindowAdjustmentOpen(null!, WindowAdjustmentOutcome.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WindowAdjustmentOpen(PaneSide.Left, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WindowActionPlanned(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WindowActionRefused(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WindowAdjustmentPlanned(null!));
    }

    /// <summary>Proves a request keeps exactly the parts it qualifies.</summary>
    [TestMethod]
    public void CreateRequestWhenPartsArePresentKeepsThem()
    {
        WindowAdjustmentSession session = new();
        WindowAdjustmentOpen open = Opened(session, PaneSide.Left);
        WindowPlacement placement = Placement(WindowPresenterState.Minimized);

        WindowAdjustmentRequest request = WindowAdjustmentRequest.Create(
            open,
            WindowAdjustmentAction.Shrink,
            placement);

        Assert.AreSame(request.ExpectedState, open);
        Assert.AreSame(WindowAdjustmentAction.Shrink, request.Action);
        Assert.AreSame(placement, request.Placement);
    }

    internal static WindowPlacement Placement(WindowPresenterState state)
    {
        return WindowAdjustmentPlannerTests.InState(state);
    }

    private static WindowAdjustmentOpen Opened(WindowAdjustmentSession session, PaneSide side)
    {
        return Assert.IsInstanceOfType<WindowAdjustmentOpen>(
            session.Open(side, InteractionOwnership.ScopeOwnsInput));
    }

    private static WindowAdjustmentRequest Request(WindowAdjustmentOpen expected, WindowAdjustmentAction action)
    {
        return WindowAdjustmentRequest.Create(expected, action, Placement(WindowPresenterState.Restored));
    }
}
