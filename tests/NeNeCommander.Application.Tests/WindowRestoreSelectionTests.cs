using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Windowing;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves startup display choice from overlap, distance and stable geometric ties.</summary>
[TestClass]
public sealed class WindowRestoreSelectionTests
{
    /// <summary>Proves first launch uses the current display even when the initial bounds overlap another.</summary>
    [TestMethod]
    public void PlanWhenNoPlacementExistsChoosesCurrentDisplay()
    {
        WindowBounds current = WindowBounds.Create(-1000, 40, 1000, 800);
        WindowBounds other = WindowBounds.Create(0, 0, 1920, 1080);

        SavedWindowPlacement plan = WindowRestorePlanner.Plan(null,
            WindowBounds.Create(100, 100, 600, 400), WindowWorkAreas.Create(current, [other, current]));

        Assert.AreEqual(WindowBounds.Create(-800, 240, 600, 400), plan.Bounds);
        Assert.AreSame(WindowPresenterState.Restored, plan.State);
    }

    /// <summary>Proves vertically spanning rectangles use actual intersecting height.</summary>
    [TestMethod]
    public void PlanWhenWindowSpansVerticalDisplaysUsesLargestOverlap()
    {
        AssertSelection(WindowBounds.Create(50, 600, 200, 400),
            WindowBounds.Create(50, 450, 200, 400),
            WindowBounds.Create(0, 0, 600, 600), WindowBounds.Create(0, 600, 600, 600));
    }

    /// <summary>Proves the intersection area combines both axes before clamping the selected size.</summary>
    [TestMethod]
    public void PlanWhenWindowSpansOffsetDisplaysUsesIntersectionArea()
    {
        AssertSelection(WindowBounds.Create(800, 300, 400, 500),
            WindowBounds.Create(600, 100, 400, 600),
            WindowBounds.Create(0, 0, 800, 300), WindowBounds.Create(800, 300, 400, 500));
    }

    /// <summary>Proves a diagonally disjoint work area cannot contribute a positive intersection.</summary>
    [TestMethod]
    public void PlanWhenAnotherDisplayIsDiagonallyDisjointKeepsContainingDisplay()
    {
        AssertSelection(WindowBounds.Create(100, 100, 200, 200),
            WindowBounds.Create(100, 100, 200, 200),
            WindowBounds.Create(0, 0, 800, 600), WindowBounds.Create(2000, 2000, 800, 600));
    }

    /// <summary>Proves both directional gaps contribute squared distance rather than one-axis proximity.</summary>
    [TestMethod]
    [DataRow(1, 1)]
    [DataRow(-1, 1)]
    [DataRow(1, -1)]
    [DataRow(-1, -1)]
    public void PlanWhenDisplaysAreDiagonalUsesSquaredRectangleDistance(int horizontal, int vertical)
    {
        AssertSelection(Reflect(WindowBounds.Create(300, 100, 100, 100), horizontal, vertical),
            Reflect(WindowBounds.Create(0, 0, 100, 100), horizontal, vertical),
            Reflect(WindowBounds.Create(300, 100, 400, 400), horizontal, vertical),
            Reflect(WindowBounds.Create(200, 300, 400, 400), horizontal, vertical));
    }

    /// <summary>Proves an equal-distance tie with the same left coordinate uses the lower top value.</summary>
    [TestMethod]
    public void PlanWhenDistanceAndLeftTieUsesTopOrder()
    {
        AssertSelection(WindowBounds.Create(50, -150, 50, 50),
            WindowBounds.Create(50, -25, 50, 50),
            WindowBounds.Create(0, 100, 200, 200), WindowBounds.Create(0, -300, 200, 200));
    }

    /// <summary>Proves equal-origin, equal-distance displays use width before height.</summary>
    [TestMethod]
    public void PlanWhenDistanceAndOriginTieUsesWidthOrder()
    {
        AssertSelection(WindowBounds.Create(0, 0, 100, 50),
            WindowBounds.Create(0, -100, 150, 50),
            WindowBounds.Create(0, 0, 200, 100), WindowBounds.Create(0, 0, 100, 200));
    }

    /// <summary>Proves the final height tie-break is observable independently of enumeration order.</summary>
    [TestMethod]
    public void PlanWhenDistanceOriginAndWidthTieUsesHeightOrder()
    {
        AssertSelection(WindowBounds.Create(0, 0, 50, 100),
            WindowBounds.Create(-100, 0, 50, 150),
            WindowBounds.Create(0, 0, 100, 200), WindowBounds.Create(0, 0, 100, 100));
    }

    private static void AssertSelection(WindowBounds expected, WindowBounds requested, WindowBounds first, WindowBounds second)
    {
        SavedWindowPlacement saved = SavedWindowPlacement.Create(requested, WindowPresenterState.Maximized);
        SavedWindowPlacement forward = WindowRestorePlanner.Plan(saved, first, WindowWorkAreas.Create(first, [first, second]));
        SavedWindowPlacement reverse = WindowRestorePlanner.Plan(saved, first, WindowWorkAreas.Create(first, [second, first]));

        Assert.AreEqual(expected, forward.Bounds);
        Assert.AreSame(WindowPresenterState.Maximized, forward.State);
        Assert.AreEqual(forward, reverse);
    }

    private static WindowBounds Reflect(WindowBounds bounds, int horizontal, int vertical)
    {
        return WindowBounds.Create(
            horizontal == 1 ? bounds.Left : -bounds.Left - bounds.Width,
            vertical == 1 ? bounds.Top : -bounds.Top - bounds.Height,
            bounds.Width, bounds.Height);
    }
}
