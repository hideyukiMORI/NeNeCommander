using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Windowing;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves initial and remembered physical placement stays reachable after desktop changes.</summary>
[TestClass]
public sealed class WindowRestorePlannerTests
{
    /// <summary>Proves a first launch centers on the current negative-coordinate display.</summary>
    [TestMethod]
    public void PlanWhenNoPlacementExistsCentersInitialSize()
    {
        WindowBounds area = WindowBounds.Create(-1920, 40, 1920, 1040);
        SavedWindowPlacement plan = WindowRestorePlanner.Plan(null,
            WindowBounds.Create(99, 99, 800, 600), WindowWorkAreas.Create(area, [area]));

        Assert.AreEqual(WindowBounds.Create(-1360, 260, 800, 600), plan.Bounds);
        Assert.AreSame(WindowPresenterState.Restored, plan.State);
    }

    /// <summary>Proves oversized defaults shrink independently in both dimensions.</summary>
    [TestMethod]
    [DataRow(2400, 1400)]
    [DataRow(int.MaxValue, int.MaxValue)]
    public void PlanWhenInitialSizeIsOversizedFitsEntireWorkArea(int width, int height)
    {
        WindowBounds area = WindowBounds.Create(50, -480, 640, 480);
        SavedWindowPlacement plan = WindowRestorePlanner.Plan(null,
            WindowBounds.Create(int.MaxValue, int.MinValue, width, height), WindowWorkAreas.Create(area, [area]));

        Assert.AreEqual(area, plan.Bounds);
    }

    /// <summary>Proves a saved rectangle uses its own display and is not recentered.</summary>
    [TestMethod]
    public void PlanWhenSavedRectangleFitsKeepsExactPhysicalPlacementAndState()
    {
        WindowBounds primary = WindowBounds.Create(0, 0, 1920, 1040);
        WindowBounds other = WindowBounds.Create(-2560, -100, 2560, 1400);
        SavedWindowPlacement saved = SavedWindowPlacement.Create(
            WindowBounds.Create(-2100, 50, 1500, 1000), WindowPresenterState.Maximized);

        SavedWindowPlacement plan = WindowRestorePlanner.Plan(saved, primary,
            WindowWorkAreas.Create(primary, [primary, other]));

        Assert.AreEqual(saved, plan);
    }

    /// <summary>Proves disconnected-display placement selects the nearest surviving area.</summary>
    [TestMethod]
    public void PlanWhenSavedDisplayIsDisconnectedUsesNearestSurvivor()
    {
        WindowBounds left = WindowBounds.Create(-1920, 0, 1920, 1040);
        WindowBounds right = WindowBounds.Create(0, 0, 1920, 1040);
        SavedWindowPlacement saved = SavedWindowPlacement.Create(
            WindowBounds.Create(5000, 200, 800, 600), WindowPresenterState.Restored);

        SavedWindowPlacement plan = WindowRestorePlanner.Plan(saved, left, WindowWorkAreas.Create(left, [left, right]));

        Assert.AreEqual(WindowBounds.Create(1120, 200, 800, 600), plan.Bounds);
    }

    /// <summary>Proves overlap takes priority over proximity and reduces a spanning window to one display.</summary>
    [TestMethod]
    public void PlanWhenWindowSpansDisplaysUsesLargestOverlap()
    {
        WindowBounds left = WindowBounds.Create(-1920, 0, 1920, 1040);
        WindowBounds right = WindowBounds.Create(0, 0, 1920, 1040);
        SavedWindowPlacement saved = SavedWindowPlacement.Create(
            WindowBounds.Create(-1200, 50, 2000, 900), WindowPresenterState.Restored);

        SavedWindowPlacement plan = WindowRestorePlanner.Plan(saved, right, WindowWorkAreas.Create(right, [right, left]));

        Assert.AreEqual(WindowBounds.Create(-1920, 50, 1920, 900), plan.Bounds);
    }

    /// <summary>Proves equal-distance display choice is stable across enumeration order.</summary>
    [TestMethod]
    public void PlanWhenTwoDisplaysTieDoesNotDependOnEnumerationOrder()
    {
        WindowBounds left = WindowBounds.Create(-1000, 0, 800, 600);
        WindowBounds right = WindowBounds.Create(200, 0, 800, 600);
        SavedWindowPlacement saved = SavedWindowPlacement.Create(
            WindowBounds.Create(-50, 200, 100, 100), WindowPresenterState.Restored);

        SavedWindowPlacement first = WindowRestorePlanner.Plan(saved, right, WindowWorkAreas.Create(right, [right, left]));
        SavedWindowPlacement second = WindowRestorePlanner.Plan(saved, right, WindowWorkAreas.Create(right, [left, right]));

        Assert.AreEqual(WindowBounds.Create(-300, 200, 100, 100), first.Bounds);
        Assert.AreEqual(first, second);
    }

    /// <summary>Proves extreme saved coordinates and overflowing far edges clamp without wrapping.</summary>
    [TestMethod]
    [DataRow(int.MinValue, int.MinValue)]
    [DataRow(int.MinValue, int.MaxValue)]
    [DataRow(int.MaxValue, int.MinValue)]
    [DataRow(int.MaxValue, int.MaxValue)]
    public void PlanWhenSavedValuesAreExtremeFitsCurrentArea(int left, int top)
    {
        WindowBounds area = WindowBounds.Create(-640, -480, 640, 480);
        SavedWindowPlacement saved = SavedWindowPlacement.Create(
            WindowBounds.Create(left, top, int.MaxValue, int.MaxValue), WindowPresenterState.Restored);

        SavedWindowPlacement plan = WindowRestorePlanner.Plan(saved, area, WindowWorkAreas.Create(area, [area]));

        Assert.AreEqual(area, plan.Bounds);
    }

    /// <summary>Proves the startup decision becomes the existing single bounds operation.</summary>
    [TestMethod]
    public void PlanNormalBoundsWhenPlacementIsDecidedUsesExistingResizePlan()
    {
        SavedWindowPlacement placement = SavedWindowPlacement.Create(
            WindowBounds.Create(-100, 50, 800, 600), WindowPresenterState.Maximized);

        WindowResizePlan plan = Assert.IsInstanceOfType<WindowResizePlan>(WindowRestorePlanner.PlanNormalBounds(placement));

        Assert.AreSame(placement.Bounds, plan.Bounds);
    }

    /// <summary>Proves even a one-pixel work area remains a positive reachable rectangle.</summary>
    [TestMethod]
    public void PlanWhenWorkAreaIsMinimalKeepsPositiveDimensions()
    {
        WindowBounds area = WindowBounds.Create(17, -31, 1, 1);
        SavedWindowPlacement plan = WindowRestorePlanner.Plan(null,
            WindowBounds.Create(0, 0, 1000, 700), WindowWorkAreas.Create(area, [area]));

        Assert.AreEqual(area, plan.Bounds);
    }
}
