using System;
using System.Collections.Generic;

namespace NeNeCommander.Application.Windowing;

/// <summary>
/// Turns one freshly read placement and one action into a plan. It is pure, deterministic, and has
/// no operating-system dependency: all arithmetic is whole physical pixels, and the only boundary
/// rule is the caption rule, which keeps a pointer-reachable run of the window's caption on the
/// desktop across display seams.
/// </summary>
public static class WindowAdjustmentPlanner
{
    /// <summary>
    /// The one adjustment step in device-independent pixels. It lives here because the distance a
    /// key moves or resizes the window is behavior, not a visual token.
    /// </summary>
    private const double WindowAdjustmentStep = 32d;

    /// <summary>Plans one adjustment of the window described by a placement read a moment ago.</summary>
    /// <param name="placement">Placement the host read immediately before this decision.</param>
    /// <param name="action">Closed action the mode requested.</param>
    /// <returns>The plan the host applies exactly once, or a refusal it only renders.</returns>
    public static WindowAdjustmentPlan Plan(WindowPlacement placement, WindowAdjustmentAction action)
    {
        ArgumentNullException.ThrowIfNull(placement);
        ArgumentNullException.ThrowIfNull(action);
        WindowAdjustmentRefusal? blocked = BlockingRefusal(placement.PresenterState, action);
        return blocked is null
            ? PlanAdmittedAction(placement, action)
            : new WindowRefusedPlan(blocked);
    }

    /// <summary>
    /// Returns the refusal the presenter state imposes on one action, or absence when the state
    /// admits it. <c>m</c> and <c>r</c> are idempotent commands, so each is refused exactly in the
    /// state it would not change; every geometry action is refused in both non-normal states.
    /// </summary>
    private static WindowAdjustmentRefusal? BlockingRefusal(
        WindowPresenterState state,
        WindowAdjustmentAction action)
    {
        return state == WindowPresenterState.Unavailable
            ? WindowAdjustmentRefusal.PlacementUnavailable
            : state == WindowPresenterState.NotOverlapped
                ? WindowAdjustmentRefusal.PresenterIsNotOverlapped
                : ActionRefusal(state, action);
    }

    private static WindowAdjustmentRefusal? ActionRefusal(
        WindowPresenterState state,
        WindowAdjustmentAction action)
    {
        return action == WindowAdjustmentAction.Maximize
            ? MaximizeRefusal(state)
            : action == WindowAdjustmentAction.Restore
                ? RestoreRefusal(state)
                : BlockingGeometryRefusal(state);
    }

    private static WindowAdjustmentRefusal? MaximizeRefusal(WindowPresenterState state)
    {
        return state == WindowPresenterState.Maximized
            ? WindowAdjustmentRefusal.WindowIsMaximized
            : null;
    }

    private static WindowAdjustmentRefusal? RestoreRefusal(WindowPresenterState state)
    {
        return state == WindowPresenterState.Restored
            ? WindowAdjustmentRefusal.WindowIsRestored
            : null;
    }

    private static WindowAdjustmentRefusal? BlockingGeometryRefusal(WindowPresenterState state)
    {
        return state == WindowPresenterState.Maximized
            ? WindowAdjustmentRefusal.WindowIsMaximized
            : state == WindowPresenterState.Minimized
                ? WindowAdjustmentRefusal.WindowIsMinimized
                : null;
    }

    private static WindowAdjustmentPlan PlanAdmittedAction(
        WindowPlacement placement,
        WindowAdjustmentAction action)
    {
        return action == WindowAdjustmentAction.Maximize
            ? WindowAdjustmentPlan.Maximize
            : action == WindowAdjustmentAction.Restore
                ? WindowAdjustmentPlan.Restore
                : PlanGeometry(placement, action);
    }

    private static WindowAdjustmentPlan PlanGeometry(
        WindowPlacement placement,
        WindowAdjustmentAction action)
    {
        int step = StepOf(placement.SizeConstraint);
        return action == WindowAdjustmentAction.Enlarge
            ? PlanEnlarge(placement, step)
            : action == WindowAdjustmentAction.Shrink
                ? PlanShrink(placement, step)
                : PlanMove(placement, action, step);
    }

    /// <summary>
    /// Converts the device-independent step into whole physical pixels for the scale the placement
    /// reports, rounding half away from zero. Crossing onto a display with another scale therefore
    /// changes the step on the next keystroke, which is the documented consequence of reading fresh.
    /// </summary>
    private static int StepOf(WindowSizeConstraint constraint)
    {
        return (int)Math.Round(
            WindowAdjustmentStep * constraint.RasterizationScale,
            MidpointRounding.AwayFromZero);
    }

    private static WindowAdjustmentPlan PlanMove(
        WindowPlacement placement,
        WindowAdjustmentAction action,
        int step)
    {
        WindowBounds bounds = placement.Bounds;
        WindowBounds target = WindowBounds.Create(
            bounds.Left + HorizontalStep(action, step),
            bounds.Top + VerticalStep(action, step),
            bounds.Width,
            bounds.Height);
        return CaptionStaysReachable(target, placement.WorkAreas, step)
            ? new WindowMovePlan(target)
            : new WindowRefusedPlan(WindowAdjustmentRefusal.CaptionWouldLeaveDesktop);
    }

    private static int HorizontalStep(WindowAdjustmentAction action, int step)
    {
        return action == WindowAdjustmentAction.MoveLeft
            ? -step
            : action == WindowAdjustmentAction.MoveRight ? step : 0;
    }

    private static int VerticalStep(WindowAdjustmentAction action, int step)
    {
        return action == WindowAdjustmentAction.MoveUp
            ? -step
            : action == WindowAdjustmentAction.MoveDown ? step : 0;
    }

    /// <summary>
    /// Applies the caption rule to a requested rectangle. The rule holds when some attached work
    /// area contains a run of the window's top edge row at least one step long, and the desktop
    /// stays continuous under that run for at least one step of caption height, whether inside that
    /// work area alone or across a horizontal seam into the work area directly below it.
    /// </summary>
    private static bool CaptionStaysReachable(
        WindowBounds target,
        WindowWorkAreas workAreas,
        int step)
    {
        int requiredWidth = Math.Min(step, target.Width);
        int requiredBottom = target.Top + Math.Min(step, target.Height);
        foreach (WindowBounds area in workAreas.Attached)
        {
            int runLeft = Math.Max(area.Left, target.Left);
            int runRight = Math.Min(area.Right, target.Right);
            if (area.Top <= target.Top && target.Top < area.Bottom &&
                runRight - runLeft >= requiredWidth &&
                ReachableBottom(area, workAreas.Attached, runLeft, runRight) >= requiredBottom)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Returns how far down the desktop stays continuous under one run of the caption row: the work
    /// area's own bottom edge, extended by a work area whose top edge is exactly that bottom edge
    /// and which covers the whole run.
    /// </summary>
    private static int ReachableBottom(
        WindowBounds area,
        IReadOnlyList<WindowBounds> attached,
        int runLeft,
        int runRight)
    {
        int bottom = area.Bottom;
        foreach (WindowBounds neighbour in attached)
        {
            if (neighbour.Top == area.Bottom &&
                neighbour.Left <= runLeft &&
                neighbour.Right >= runRight &&
                neighbour.Bottom > bottom)
            {
                bottom = neighbour.Bottom;
            }
        }
        return bottom;
    }

    /// <summary>
    /// Enlarges by one step in both dimensions with the top-left corner fixed, so the caption never
    /// moves and no boundary check is needed.
    /// </summary>
    private static WindowAdjustmentPlan PlanEnlarge(WindowPlacement placement, int step)
    {
        WindowBounds bounds = placement.Bounds;
        WindowBounds workArea = placement.WorkAreas.Current;
        return bounds.Width >= workArea.Width && bounds.Height >= workArea.Height
            ? new WindowRefusedPlan(WindowAdjustmentRefusal.AtMaximumSize)
            : new WindowResizePlan(WindowBounds.Create(
                bounds.Left,
                bounds.Top,
                bounds.Width + step,
                bounds.Height + step));
    }

    /// <summary>
    /// Shrinks by one step in both dimensions with the top-left corner fixed, refusing before either
    /// dimension would fall below its effective minimum.
    /// </summary>
    private static WindowAdjustmentPlan PlanShrink(WindowPlacement placement, int step)
    {
        WindowBounds bounds = placement.Bounds;
        WindowSizeConstraint constraint = placement.SizeConstraint;
        int width = bounds.Width - step;
        int height = bounds.Height - step;
        return width < EffectiveMinimum(constraint.PreferredMinimumWidth, step) ||
            height < EffectiveMinimum(constraint.PreferredMinimumHeight, step)
                ? new WindowRefusedPlan(WindowAdjustmentRefusal.AtMinimumSize)
                : new WindowResizePlan(WindowBounds.Create(bounds.Left, bounds.Top, width, height));
    }

    /// <summary>
    /// Returns the floor of one dimension: the declared preferred minimum when the presenter
    /// declares one, otherwise one step, so that no dimension can reach zero.
    /// </summary>
    private static int EffectiveMinimum(int preferredMinimum, int step)
    {
        return preferredMinimum > 0 ? preferredMinimum : step;
    }
}
