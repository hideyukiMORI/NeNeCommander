using System;
using System.Runtime.InteropServices;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Windowing;

namespace NeNeCommander.App.Windowing;

/// <summary>
/// Carries the window-adjustment mode's declared synchronous input route (ADR-0050): each action
/// reads a fresh placement, asks the session for a decision, and applies the returned plan exactly
/// once. It runs on the UI thread outside <c>AsyncWorkOwner</c> so key repeat is never dropped,
/// and it holds no geometry between keystrokes.
/// </summary>
internal sealed class WindowAdjustmentRoute
{
    private readonly CommanderSession _session;
    private readonly AppWindowPlacementAdapter _adapter;
    private readonly Action<Exception> _defectObserver;

    /// <summary>Initializes the route over the session, the window adapter, and the defect observer.</summary>
    /// <param name="session">Coordinator that decides every window action.</param>
    /// <param name="adapter">Translator over the window's own placement.</param>
    /// <param name="defectObserver">Host callback that publishes an unexpected platform defect.</param>
    internal WindowAdjustmentRoute(
        CommanderSession session,
        AppWindowPlacementAdapter adapter,
        Action<Exception> defectObserver)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(adapter);
        ArgumentNullException.ThrowIfNull(defectObserver);
        _session = session;
        _adapter = adapter;
        _defectObserver = defectObserver;
    }

    /// <summary>Gets the window-adjustment state the session holds now.</summary>
    internal WindowAdjustmentState Current => _session.Current.Scopes.WindowAdjustment;

    /// <summary>Decides one qualified action against a fresh placement and applies its plan once.</summary>
    /// <param name="expected">Open state the host last rendered.</param>
    /// <param name="action">Window action the key map produced.</param>
    internal void Adjust(WindowAdjustmentOpen expected, WindowAdjustmentAction action)
    {
        WindowAdjustmentRequest request = WindowAdjustmentRequest.Create(expected, action, _adapter.Read());
        if (_session.AdjustWindow(request) is WindowAdjustmentPlanned planned)
        {
            Apply(planned.Plan);
        }
    }

    /// <summary>Leaves one qualified open mode; it reads no placement and cannot be refused.</summary>
    /// <param name="expected">Open state the host last rendered.</param>
    internal void Leave(WindowAdjustmentOpen expected)
    {
        _ = _session.LeaveWindowAdjustment(expected);
    }

    /// <summary>
    /// Applies the plan; a platform failure is a defect, reported through the existing observer
    /// like every other host defect. The session state is not rolled back, because the recorded
    /// outcome names the decision and not its effect.
    /// </summary>
    private void Apply(WindowAdjustmentPlan plan)
    {
        try
        {
            _adapter.Apply(plan);
        }
        catch (COMException defect)
        {
            _defectObserver(defect);
        }
        catch (InvalidOperationException defect)
        {
            _defectObserver(defect);
        }
        catch (ArgumentException defect)
        {
            _defectObserver(defect);
        }
    }
}
