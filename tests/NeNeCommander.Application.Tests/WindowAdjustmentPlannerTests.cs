using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Windowing;

namespace NeNeCommander.Application.Tests;

/// <summary>
/// Proves the pure window-adjustment planner: the scale-dependent step, the caption rule at every
/// desktop edge and seam, the top-left anchored resize, and every presenter-state refusal.
/// </summary>
[TestClass]
public sealed class WindowAdjustmentPlannerTests
{
    private static readonly WindowBounds Desktop = WindowBounds.Create(0, 0, 1920, 1040);

    /// <summary>Proves one step is 32 device-independent pixels at every supported display scale.</summary>
    [TestMethod]
    [DataRow(1.0, 32)]
    [DataRow(1.25, 40)]
    [DataRow(1.5, 48)]
    [DataRow(1.75, 56)]
    [DataRow(2.0, 64)]
    [DataRow(3.0, 96)]
    public void PlanWhenScaleIsStandardMovesByThirtyTwoDeviceIndependentPixels(double scale, int step)
    {
        WindowBounds desktop = WindowBounds.Create(0, 0, 8000, 6000);
        WindowPlacement placement = Restored(WindowBounds.Create(1000, 1000, 800, 600), scale, desktop);

        AssertMovedTo(placement, WindowAdjustmentAction.MoveLeft, 1000 - step, 1000);
        AssertMovedTo(placement, WindowAdjustmentAction.MoveRight, 1000 + step, 1000);
        AssertMovedTo(placement, WindowAdjustmentAction.MoveUp, 1000, 1000 - step);
        AssertMovedTo(placement, WindowAdjustmentAction.MoveDown, 1000, 1000 + step);
        AssertResizedTo(placement, WindowAdjustmentAction.Enlarge, 800 + step, 600 + step);
        AssertResizedTo(placement, WindowAdjustmentAction.Shrink, 800 - step, 600 - step);
    }

    /// <summary>
    /// Proves a fractional step is rounded half away from zero: 32 × 1.015625 is exactly 32.5,
    /// which banker's rounding and truncation would both make 32, and 32 × 1.3 is 41.6.
    /// </summary>
    [TestMethod]
    [DataRow(1.015625, 33)]
    [DataRow(1.3, 42)]
    public void PlanWhenScaleIsNonStandardRoundsTheStepHalfAwayFromZero(double scale, int step)
    {
        WindowPlacement placement = Restored(WindowBounds.Create(500, 400, 800, 600), scale, Desktop);

        AssertMovedTo(placement, WindowAdjustmentAction.MoveRight, 500 + step, 400);
        AssertResizedTo(placement, WindowAdjustmentAction.Enlarge, 800 + step, 600 + step);
    }

    /// <summary>Proves every move keeps the window size and only the requested axis changes.</summary>
    [TestMethod]
    public void PlanWhenMoveIsAdmittedReturnsExactBoundsWithUnchangedSize()
    {
        WindowPlacement placement = Restored(WindowBounds.Create(300, 200, 640, 480), 1.0, Desktop);

        WindowMovePlan left = Assert.IsInstanceOfType<WindowMovePlan>(
            WindowAdjustmentPlanner.Plan(placement, WindowAdjustmentAction.MoveLeft));
        WindowMovePlan down = Assert.IsInstanceOfType<WindowMovePlan>(
            WindowAdjustmentPlanner.Plan(placement, WindowAdjustmentAction.MoveDown));

        Assert.AreEqual(WindowBounds.Create(268, 200, 640, 480), left.Bounds);
        Assert.AreEqual(WindowBounds.Create(300, 232, 640, 480), down.Bounds);
    }

    /// <summary>Proves the left desktop edge keeps exactly one step of caption and refuses one pixel less.</summary>
    [TestMethod]
    public void PlanWhenMoveReachesTheLeftEdgeKeepsOneStepOfCaption()
    {
        AssertMovedTo(Restored(WindowBounds.Create(-736, 100, 800, 600), 1.0, Desktop),
            WindowAdjustmentAction.MoveLeft, -768, 100);
        AssertRefused(Restored(WindowBounds.Create(-737, 100, 800, 600), 1.0, Desktop),
            WindowAdjustmentAction.MoveLeft, WindowAdjustmentRefusal.CaptionWouldLeaveDesktop);
    }

    /// <summary>Proves the right desktop edge keeps exactly one step of caption and refuses one pixel less.</summary>
    [TestMethod]
    public void PlanWhenMoveReachesTheRightEdgeKeepsOneStepOfCaption()
    {
        AssertMovedTo(Restored(WindowBounds.Create(1856, 100, 800, 600), 1.0, Desktop),
            WindowAdjustmentAction.MoveRight, 1888, 100);
        AssertRefused(Restored(WindowBounds.Create(1857, 100, 800, 600), 1.0, Desktop),
            WindowAdjustmentAction.MoveRight, WindowAdjustmentRefusal.CaptionWouldLeaveDesktop);
    }

    /// <summary>Proves the caption row may sit on the top edge of the desktop but never above it.</summary>
    [TestMethod]
    public void PlanWhenMoveReachesTheTopEdgeKeepsTheCaptionRowOnTheDesktop()
    {
        AssertMovedTo(Restored(WindowBounds.Create(100, 32, 800, 600), 1.0, Desktop),
            WindowAdjustmentAction.MoveUp, 100, 0);
        AssertRefused(Restored(WindowBounds.Create(100, 31, 800, 600), 1.0, Desktop),
            WindowAdjustmentAction.MoveUp, WindowAdjustmentRefusal.CaptionWouldLeaveDesktop);
    }

    /// <summary>Proves the bottom desktop edge keeps exactly one step of caption height.</summary>
    [TestMethod]
    public void PlanWhenMoveReachesTheBottomEdgeKeepsOneStepOfCaptionHeight()
    {
        AssertMovedTo(Restored(WindowBounds.Create(100, 976, 800, 600), 1.0, Desktop),
            WindowAdjustmentAction.MoveDown, 100, 1008);
        AssertRefused(Restored(WindowBounds.Create(100, 977, 800, 600), 1.0, Desktop),
            WindowAdjustmentAction.MoveDown, WindowAdjustmentRefusal.CaptionWouldLeaveDesktop);
    }

    /// <summary>Proves a window narrower or shorter than one step needs only its own width or height.</summary>
    [TestMethod]
    public void PlanWhenWindowIsSmallerThanOneStepRequiresOnlyItsOwnExtent()
    {
        AssertMovedTo(Restored(WindowBounds.Create(1868, 100, 20, 600), 1.0, Desktop),
            WindowAdjustmentAction.MoveRight, 1900, 100);
        AssertRefused(Restored(WindowBounds.Create(1869, 100, 20, 600), 1.0, Desktop),
            WindowAdjustmentAction.MoveRight, WindowAdjustmentRefusal.CaptionWouldLeaveDesktop);
        AssertMovedTo(Restored(WindowBounds.Create(100, 988, 800, 20), 1.0, Desktop),
            WindowAdjustmentAction.MoveDown, 100, 1020);
        AssertRefused(Restored(WindowBounds.Create(100, 989, 800, 20), 1.0, Desktop),
            WindowAdjustmentAction.MoveDown, WindowAdjustmentRefusal.CaptionWouldLeaveDesktop);
    }

    /// <summary>
    /// Proves a vertical seam between side-by-side displays: the caption may leave the first display
    /// when the neighbouring display holds one step of it, and the window can be carried across.
    /// </summary>
    [TestMethod]
    public void PlanWhenMoveCrossesAVerticalSeamUsesTheNeighbouringDisplay()
    {
        WindowBounds right = WindowBounds.Create(1920, 0, 1920, 1040);
        WindowPlacement crossing = Restored(
            WindowBounds.Create(1870, 100, 800, 600), 1.0, Desktop, Desktop, right);
        WindowPlacement carried = Restored(
            WindowBounds.Create(1920, 100, 800, 600), 1.0, Desktop, Desktop, right);

        AssertMovedTo(crossing, WindowAdjustmentAction.MoveRight, 1902, 100);
        AssertMovedTo(carried, WindowAdjustmentAction.MoveRight, 1952, 100);
    }

    /// <summary>Proves a neighbouring display that does not contain the caption row cannot hold the caption.</summary>
    [TestMethod]
    public void PlanWhenVerticalSeamNeighbourIsBelowTheCaptionRowRefusesTheMove()
    {
        WindowBounds lower = WindowBounds.Create(1920, 300, 1920, 1040);
        WindowPlacement placement = Restored(
            WindowBounds.Create(1857, 100, 800, 600), 1.0, Desktop, Desktop, lower);

        AssertRefused(placement, WindowAdjustmentAction.MoveRight, WindowAdjustmentRefusal.CaptionWouldLeaveDesktop);
    }

    /// <summary>
    /// Proves a horizontal seam: a caption row near the bottom of the upper display stays reachable
    /// when the display directly below continues it.
    /// </summary>
    [TestMethod]
    public void PlanWhenMoveCrossesAHorizontalSeamUsesTheDisplayBelow()
    {
        WindowBounds below = WindowBounds.Create(0, 1040, 1920, 1040);
        WindowPlacement placement = Restored(
            WindowBounds.Create(100, 990, 800, 600), 1.0, Desktop, Desktop, below);

        AssertMovedTo(placement, WindowAdjustmentAction.MoveDown, 100, 1022);
    }

    /// <summary>Proves a display below shallower than the missing caption height cannot hold the caption.</summary>
    [TestMethod]
    public void PlanWhenDisplayBelowIsShallowerThanTheMissingHeightRefusesTheMove()
    {
        WindowBounds shallow = WindowBounds.Create(0, 1040, 1920, 13);
        WindowBounds deep = WindowBounds.Create(0, 1040, 1920, 14);
        WindowPlacement refused = Restored(
            WindowBounds.Create(100, 990, 800, 600), 1.0, Desktop, Desktop, shallow);
        WindowPlacement accepted = Restored(
            WindowBounds.Create(100, 990, 800, 600), 1.0, Desktop, Desktop, deep);

        AssertRefused(refused, WindowAdjustmentAction.MoveDown, WindowAdjustmentRefusal.CaptionWouldLeaveDesktop);
        AssertMovedTo(accepted, WindowAdjustmentAction.MoveDown, 100, 1022);
    }

    /// <summary>Proves enlarge adds one step to both dimensions with the top-left corner fixed.</summary>
    [TestMethod]
    public void PlanWhenEnlargeIsAdmittedKeepsTheTopLeftCorner()
    {
        WindowPlacement placement = Restored(WindowBounds.Create(-50, 1000, 800, 600), 1.25, Desktop);

        WindowResizePlan plan = Assert.IsInstanceOfType<WindowResizePlan>(
            WindowAdjustmentPlanner.Plan(placement, WindowAdjustmentAction.Enlarge));

        Assert.AreEqual(WindowBounds.Create(-50, 1000, 840, 640), plan.Bounds);
    }

    /// <summary>Proves enlarge is refused only when the window already covers its work area in both dimensions.</summary>
    [TestMethod]
    public void PlanWhenWindowCoversItsWorkAreaRefusesEnlargeAtMaximumSize()
    {
        AssertRefused(Restored(WindowBounds.Create(-10, -10, 2000, 1200), 1.0, Desktop),
            WindowAdjustmentAction.Enlarge, WindowAdjustmentRefusal.AtMaximumSize);
        AssertRefused(Restored(WindowBounds.Create(0, 0, 1920, 1040), 1.0, Desktop),
            WindowAdjustmentAction.Enlarge, WindowAdjustmentRefusal.AtMaximumSize);
        AssertResizedTo(Restored(WindowBounds.Create(0, 0, 1920, 1039), 1.0, Desktop),
            WindowAdjustmentAction.Enlarge, 1952, 1071);
        AssertResizedTo(Restored(WindowBounds.Create(0, 0, 1919, 1040), 1.0, Desktop),
            WindowAdjustmentAction.Enlarge, 1951, 1072);
    }

    /// <summary>Proves enlarge compares with the work area of the window's own display, not another one.</summary>
    [TestMethod]
    public void PlanWhenAnotherDisplayIsLargerComparesWithTheCurrentWorkArea()
    {
        WindowBounds large = WindowBounds.Create(1920, 0, 3840, 2120);
        WindowPlacement placement = Restored(
            WindowBounds.Create(0, 0, 1920, 1040), 1.0, Desktop, Desktop, large);

        AssertRefused(placement, WindowAdjustmentAction.Enlarge, WindowAdjustmentRefusal.AtMaximumSize);
    }

    /// <summary>Proves shrink removes one step from both dimensions with the top-left corner fixed.</summary>
    [TestMethod]
    public void PlanWhenShrinkIsAdmittedKeepsTheTopLeftCorner()
    {
        WindowPlacement placement = Restored(WindowBounds.Create(120, -40, 800, 600), 1.5, Desktop);

        WindowResizePlan plan = Assert.IsInstanceOfType<WindowResizePlan>(
            WindowAdjustmentPlanner.Plan(placement, WindowAdjustmentAction.Shrink));

        Assert.AreEqual(WindowBounds.Create(120, -40, 752, 552), plan.Bounds);
    }

    /// <summary>Proves a declared preferred minimum is reachable exactly and refuses one pixel below it.</summary>
    [TestMethod]
    public void PlanWhenShrinkMeetsTheDeclaredMinimumRefusesBelowIt()
    {
        AssertResizedTo(Constrained(800, 600, 768, 568), WindowAdjustmentAction.Shrink, 768, 568);
        AssertRefused(Constrained(800, 600, 769, 0), WindowAdjustmentAction.Shrink,
            WindowAdjustmentRefusal.AtMinimumSize);
        AssertRefused(Constrained(800, 600, 0, 569), WindowAdjustmentAction.Shrink,
            WindowAdjustmentRefusal.AtMinimumSize);
        AssertRefused(Constrained(41, 600, 10, 0), WindowAdjustmentAction.Shrink,
            WindowAdjustmentRefusal.AtMinimumSize);
        AssertResizedTo(Constrained(42, 600, 10, 0), WindowAdjustmentAction.Shrink, 10, 568);
    }

    /// <summary>Proves an undeclared minimum falls back to one step, so no dimension can reach zero.</summary>
    [TestMethod]
    public void PlanWhenNoMinimumIsDeclaredRefusesBelowOneStep()
    {
        AssertResizedTo(Constrained(64, 64, 0, 0), WindowAdjustmentAction.Shrink, 32, 32);
        AssertRefused(Constrained(63, 600, 0, 0), WindowAdjustmentAction.Shrink,
            WindowAdjustmentRefusal.AtMinimumSize);
        AssertRefused(Constrained(600, 63, 0, 0), WindowAdjustmentAction.Shrink,
            WindowAdjustmentRefusal.AtMinimumSize);
    }

    /// <summary>Proves maximize and restore are idempotent commands with their own refusals.</summary>
    [TestMethod]
    public void PlanWhenMaximizeOrRestoreArrivesIsIdempotentPerPresenterState()
    {
        Assert.AreSame(WindowAdjustmentPlan.Maximize, Plan(WindowPresenterState.Restored, WindowAdjustmentAction.Maximize));
        Assert.AreSame(WindowAdjustmentPlan.Maximize, Plan(WindowPresenterState.Minimized, WindowAdjustmentAction.Maximize));
        Assert.AreSame(WindowAdjustmentPlan.Restore, Plan(WindowPresenterState.Maximized, WindowAdjustmentAction.Restore));
        Assert.AreSame(WindowAdjustmentPlan.Restore, Plan(WindowPresenterState.Minimized, WindowAdjustmentAction.Restore));
        AssertReason(WindowAdjustmentRefusal.WindowIsMaximized,
            Plan(WindowPresenterState.Maximized, WindowAdjustmentAction.Maximize));
        AssertReason(WindowAdjustmentRefusal.WindowIsRestored,
            Plan(WindowPresenterState.Restored, WindowAdjustmentAction.Restore));
    }

    /// <summary>Proves move, enlarge, and shrink are refused on a maximized or minimized window.</summary>
    [TestMethod]
    public void PlanWhenWindowIsMaximizedOrMinimizedRefusesEveryGeometryAction()
    {
        foreach (WindowAdjustmentAction action in GeometryActions())
        {
            AssertReason(WindowAdjustmentRefusal.WindowIsMaximized, Plan(WindowPresenterState.Maximized, action));
            AssertReason(WindowAdjustmentRefusal.WindowIsMinimized, Plan(WindowPresenterState.Minimized, action));
        }
    }

    /// <summary>Proves every action is refused for a non-overlapped presenter or an unavailable placement.</summary>
    [TestMethod]
    public void PlanWhenPresenterIsNotOverlappedOrUnavailableRefusesEveryAction()
    {
        foreach (WindowAdjustmentAction action in AllActions())
        {
            AssertReason(WindowAdjustmentRefusal.PresenterIsNotOverlapped, Plan(WindowPresenterState.NotOverlapped, action));
            AssertReason(WindowAdjustmentRefusal.PlacementUnavailable, Plan(WindowPresenterState.Unavailable, action));
            AssertReason(
                WindowAdjustmentRefusal.PlacementUnavailable,
                WindowAdjustmentPlanner.Plan(WindowPlacement.Unavailable, action));
        }
    }

    /// <summary>Proves the published unavailable placement carries only the closed fallback facts.</summary>
    [TestMethod]
    public void UnavailableWhenReadDescribesAOnePixelUnscaledPlacement()
    {
        WindowPlacement unavailable = WindowPlacement.Unavailable;

        Assert.AreSame(WindowPresenterState.Unavailable, unavailable.PresenterState);
        Assert.AreEqual(WindowBounds.Create(0, 0, 1, 1), unavailable.Bounds);
        Assert.AreEqual(1d, unavailable.SizeConstraint.RasterizationScale);
        Assert.AreEqual(0, unavailable.SizeConstraint.PreferredMinimumWidth);
        Assert.AreEqual(0, unavailable.SizeConstraint.PreferredMinimumHeight);
        Assert.AreEqual(WindowBounds.Create(0, 0, 1, 1), unavailable.WorkAreas.Current);
        Assert.HasCount(1, unavailable.WorkAreas.Attached);
    }

    /// <summary>Proves the planner rejects an absent placement or action.</summary>
    [TestMethod]
    public void PlanWhenAnArgumentIsNullThrowsArgumentNullException()
    {
        WindowPlacement placement = Restored(WindowBounds.Create(0, 0, 100, 100), 1.0, Desktop);

        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => WindowAdjustmentPlanner.Plan(null!, WindowAdjustmentAction.MoveLeft));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => WindowAdjustmentPlanner.Plan(placement, null!));
    }

    internal static WindowPlacement Restored(
        WindowBounds bounds,
        double scale,
        WindowBounds current,
        params WindowBounds[] attached)
    {
        return WindowPlacement.Create(
            bounds,
            WindowPresenterState.Restored,
            WindowSizeConstraint.Create(scale, 0, 0),
            WindowWorkAreas.Create(current, attached.Length == 0 ? [current] : attached));
    }

    internal static WindowPlacement InState(WindowPresenterState state)
    {
        return WindowPlacement.Create(
            WindowBounds.Create(100, 100, 800, 600),
            state,
            WindowSizeConstraint.Create(1d, 0, 0),
            WindowWorkAreas.Create(Desktop, [Desktop]));
    }

    internal static WindowAdjustmentAction[] AllActions()
    {
        return
        [
            WindowAdjustmentAction.MoveLeft,
            WindowAdjustmentAction.MoveDown,
            WindowAdjustmentAction.MoveUp,
            WindowAdjustmentAction.MoveRight,
            WindowAdjustmentAction.Enlarge,
            WindowAdjustmentAction.Shrink,
            WindowAdjustmentAction.Maximize,
            WindowAdjustmentAction.Restore,
        ];
    }

    private static WindowAdjustmentAction[] GeometryActions()
    {
        return
        [
            WindowAdjustmentAction.MoveLeft,
            WindowAdjustmentAction.MoveDown,
            WindowAdjustmentAction.MoveUp,
            WindowAdjustmentAction.MoveRight,
            WindowAdjustmentAction.Enlarge,
            WindowAdjustmentAction.Shrink,
        ];
    }

    private static WindowAdjustmentPlan Plan(WindowPresenterState state, WindowAdjustmentAction action)
    {
        return WindowAdjustmentPlanner.Plan(InState(state), action);
    }

    private static WindowPlacement Constrained(int width, int height, int minimumWidth, int minimumHeight)
    {
        return WindowPlacement.Create(
            WindowBounds.Create(100, 100, width, height),
            WindowPresenterState.Restored,
            WindowSizeConstraint.Create(1d, minimumWidth, minimumHeight),
            WindowWorkAreas.Create(Desktop, [Desktop]));
    }

    private static void AssertMovedTo(
        WindowPlacement placement,
        WindowAdjustmentAction action,
        int left,
        int top)
    {
        WindowMovePlan plan = Assert.IsInstanceOfType<WindowMovePlan>(
            WindowAdjustmentPlanner.Plan(placement, action));
        Assert.AreEqual(
            WindowBounds.Create(left, top, placement.Bounds.Width, placement.Bounds.Height),
            plan.Bounds);
    }

    private static void AssertResizedTo(
        WindowPlacement placement,
        WindowAdjustmentAction action,
        int width,
        int height)
    {
        WindowResizePlan plan = Assert.IsInstanceOfType<WindowResizePlan>(
            WindowAdjustmentPlanner.Plan(placement, action));
        Assert.AreEqual(
            WindowBounds.Create(placement.Bounds.Left, placement.Bounds.Top, width, height),
            plan.Bounds);
    }

    private static void AssertRefused(
        WindowPlacement placement,
        WindowAdjustmentAction action,
        WindowAdjustmentRefusal expected)
    {
        AssertReason(expected, WindowAdjustmentPlanner.Plan(placement, action));
    }

    private static void AssertReason(WindowAdjustmentRefusal expected, WindowAdjustmentPlan plan)
    {
        Assert.AreSame(expected, Assert.IsInstanceOfType<WindowRefusedPlan>(plan).Reason);
    }
}
