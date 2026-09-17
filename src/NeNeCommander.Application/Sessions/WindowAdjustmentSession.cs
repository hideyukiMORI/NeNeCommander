using System;
using System.Threading;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Windowing;

namespace NeNeCommander.Application.Sessions;

/// <summary>
/// Owns the window-adjustment transient scope: its closed state under one lock, its admission rule,
/// the expected-state validation of one qualified action, and the expected-state validation of one
/// qualified leave. It knows no other owner, holds no freeze predicate, performs no pane effect,
/// never reads or writes the window, and dispatches nothing. Every operation is total: on a state
/// it does not expect it changes nothing.
/// </summary>
public sealed class WindowAdjustmentSession
{
    private readonly Lock _sync = new();
    private WindowAdjustmentState _state = WindowAdjustmentState.Closed;

    /// <summary>Gets the current immutable window-adjustment state.</summary>
    public WindowAdjustmentState Current
    {
        get
        {
            lock (_sync)
            {
                return _state;
            }
        }
    }

    /// <summary>
    /// Opens the mode over the pane that is active at entry when this scope may own input. Listed
    /// pane content is not required: a pane whose last read failed does not prevent moving the
    /// window. Opening an already open mode changes nothing.
    /// </summary>
    /// <param name="active">Pane side active at entry, captured for the one-time focus return.</param>
    /// <param name="ownership">Input ownership the session derived for this scope.</param>
    /// <returns>The state current after this admission decision.</returns>
    public WindowAdjustmentState Open(PaneSide active, InteractionOwnership ownership)
    {
        ArgumentNullException.ThrowIfNull(active);
        ArgumentNullException.ThrowIfNull(ownership);
        lock (_sync)
        {
            if (ownership == InteractionOwnership.ScopeOwnsInput && _state is not WindowAdjustmentOpen)
            {
                _state = new WindowAdjustmentOpen(active, WindowAdjustmentOutcome.None);
            }
            return _state;
        }
    }

    /// <summary>
    /// Decides one qualified window action. The planner call and the outcome update happen inside
    /// this lock, so the plan the caller receives always matches the outcome the scope recorded.
    /// </summary>
    /// <param name="request">Action qualified by the expected open state and a fresh placement.</param>
    /// <returns>The plan the host applies exactly once, or nothing to apply for a stale request.</returns>
    public WindowAdjustmentDecision Adjust(WindowAdjustmentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        lock (_sync)
        {
            if (!ReferenceEquals(_state, request.ExpectedState))
            {
                return WindowAdjustmentDecision.NothingToApply;
            }
            WindowAdjustmentPlan plan = WindowAdjustmentPlanner.Plan(request.Placement, request.Action);
            _state = new WindowAdjustmentOpen(
                request.ExpectedState.ActiveSide,
                OutcomeOf(plan, request.Action));
            return new WindowAdjustmentPlanned(plan);
        }
    }

    /// <summary>
    /// Leaves one qualified open mode and requests the one-time focus return to the pane captured
    /// at entry. Leaving reads no placement and can be refused by no placement state, so the mode
    /// never traps the user; a stale leave changes nothing.
    /// </summary>
    /// <param name="expected">Exact open state the caller last rendered.</param>
    /// <returns>The state current after this decision.</returns>
    public WindowAdjustmentState Leave(WindowAdjustmentOpen expected)
    {
        ArgumentNullException.ThrowIfNull(expected);
        lock (_sync)
        {
            if (ReferenceEquals(_state, expected))
            {
                _state = WindowAdjustmentState.CloseFocusing(expected.ActiveSide);
            }
            return _state;
        }
    }

    private static WindowAdjustmentOutcome OutcomeOf(
        WindowAdjustmentPlan plan,
        WindowAdjustmentAction action)
    {
        return plan is WindowRefusedPlan refused
            ? new WindowActionRefused(refused.Reason)
            : new WindowActionPlanned(action);
    }
}
