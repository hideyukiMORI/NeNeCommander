using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Windowing;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves reported-scale conversion, bounded rectangles and minimum-size safety.</summary>
[TestClass]
public sealed class WindowTransferSizingTests
{
    /// <summary>A normal transfer preserves reported logical size and centers with two explicit rectangles.</summary>
    [TestMethod]
    public void PlanWhenScaleChangesChoosesCompensatedAndFinalRectangles()
    {
        WindowTransferPlan plan = WindowTransferTestContext.Plan;
        Assert.AreSame(WindowTransferTestContext.Original, plan.Source);
        Assert.AreSame(WindowTransferTestContext.Displays, plan.Displays);
        Assert.AreEqual(WindowBounds.Create(720, 280, 800, 600), plan.Geometry.Initial);
        Assert.AreEqual(WindowBounds.Create(720, 280, 1120, 840), plan.Geometry.Final);
    }

    /// <summary>Half pixels round away from zero while odd centering gaps floor toward the start.</summary>
    [TestMethod]
    public void PlanWhenSizesContainHalfPixelsRoundsAndCentersExactly()
    {
        WindowTransferPlan plan = Assert.IsInstanceOfType<WindowTransferPlan>(Plan(
            WindowBounds.Create(0, 0, 101, 103), WindowBounds.Create(-1000, -900, 1001, 999),
            WindowSizeConstraint.Create(999d, 0, 0), [1d, 1.5d]));
        Assert.AreEqual(WindowBounds.Create(-576, -478, 152, 155), plan.Geometry.Final);
        Assert.AreEqual(WindowBounds.Create(-576, -478, 101, 103), plan.Geometry.Initial);
    }

    /// <summary>Both final dimensions clip to the destination and the first request is compensated from that fit.</summary>
    [TestMethod]
    public void PlanWhenSourceExceedsTargetFitsBothFinalDimensions()
    {
        WindowTransferPlan plan = Assert.IsInstanceOfType<WindowTransferPlan>(Plan(
            WindowBounds.Create(0, 0, 4000, 3000), WindowBounds.Create(-1000, -800, 1000, 800),
            WindowSizeConstraint.Create(1d, 0, 0), [1.75d, 1.5d]));
        Assert.AreEqual(WindowBounds.Create(-1000, -800, 1000, 800), plan.Geometry.Final);
        Assert.AreEqual(WindowBounds.Create(-1000, -800, 1167, 933), plan.Geometry.Initial);
    }

    /// <summary>Declared minima stay physical and are checked for both the initial and final request.</summary>
    [TestMethod]
    public void PlanWhenDeclaredMinimumWouldBeViolatedRefusesEitherPhase()
    {
        WindowBounds bounds = WindowBounds.Create(0, 0, 600, 450);
        WindowBounds target = WindowBounds.Create(-1000, 0, 1000, 800);
        AssertCannotFit(Plan(bounds, target, WindowSizeConstraint.Create(1d, 500, 400), [1.75d, 1.25d]));
        AssertCannotFit(Plan(bounds, WindowBounds.Create(-600, 0, 600, 500), WindowSizeConstraint.Create(1d, 500, 400), [1.25d, 1.75d]));
        WindowTransferPlan exact = Assert.IsInstanceOfType<WindowTransferPlan>(Plan(
            WindowBounds.Create(0, 0, 500, 400), target, WindowSizeConstraint.Create(9d, 500, 400), [1d, 1d]));
        Assert.AreEqual(500, exact.Geometry.Final.Width);
        Assert.AreEqual(400, exact.Geometry.Initial.Height);
    }

    /// <summary>Undeclared minima use each native reported scale, independently in width and height.</summary>
    [TestMethod]
    [DataRow(31, 100)]
    [DataRow(100, 31)]
    public void PlanWhenUndeclaredMinimumFailsInEitherDimensionRefuses(int width, int height)
    {
        AssertCannotFit(Plan(WindowBounds.Create(0, 0, width, height), WindowBounds.Create(0, 0, 1000, 1000),
            WindowSizeConstraint.Create(0.01d, 0, 0), [1d, 2d]));
    }

    /// <summary>Very small positive scales keep valid dimensions and the effective minimum at one.</summary>
    [TestMethod]
    public void PlanWhenScaleIsTinyKeepsPositiveOnePixelGeometry()
    {
        WindowTransferPlan plan = Assert.IsInstanceOfType<WindowTransferPlan>(Plan(
            WindowBounds.Create(0, 0, 1, 1), WindowBounds.Create(-2, -2, 2, 2),
            WindowSizeConstraint.Create(1d, 0, 0), [0.001d, 0.0015d]));
        Assert.AreEqual(2, plan.Geometry.Final.Width);
        Assert.AreEqual(1, plan.Geometry.Initial.Width);
    }

    /// <summary>Overflow, underflow and the compensated multiplication cannot be narrowed into bogus geometry.</summary>
    [TestMethod]
    [DataRow(double.Epsilon, 1d)]
    [DataRow(1d, double.Epsilon)]
    [DataRow(1e308d, 1e308d)]
    public void PlanWhenScaleArithmeticIsUnrepresentableRefuses(double sourceScale, double targetScale)
    {
        AssertCannotFit(Plan(WindowBounds.Create(0, 0, 800, 600), WindowBounds.Create(0, 0, 1000, 800),
            WindowSizeConstraint.Create(1d, 0, 0), [sourceScale, targetScale]));
    }

    /// <summary>Final endpoints outside the platform coordinate range are rejected before casting.</summary>
    [TestMethod]
    [DataRow(int.MaxValue - 100, 0)]
    [DataRow(0, int.MaxValue - 100)]
    public void PlanWhenPositiveEndpointOverflowsRefuses(int left, int top)
    {
        AssertCannotFit(Plan(WindowBounds.Create(0, 0, 600, 400), WindowBounds.Create(left, top, 1000, 800),
            WindowSizeConstraint.Create(1d, 0, 0), [1d, 1d]));
    }

    /// <summary>Signed minimum origins and exact maximum endpoints remain legal.</summary>
    [TestMethod]
    [DataRow(int.MinValue, int.MinValue)]
    [DataRow(int.MaxValue - 600, int.MaxValue - 400)]
    public void PlanWhenCoordinateBoundaryIsExactPreservesIt(int left, int top)
    {
        WindowBounds target = WindowBounds.Create(left, top, 600, 400);
        WindowTransferPlan plan = Assert.IsInstanceOfType<WindowTransferPlan>(Plan(
            WindowBounds.Create(0, 0, 600, 400), target, WindowSizeConstraint.Create(1d, 0, 0), [1d, 1d]));
        Assert.AreEqual(target, plan.Geometry.Final);
    }

    private static WindowAdjustmentPlan Plan(WindowBounds bounds, WindowBounds area, WindowSizeConstraint minimum, double[] scales)
    {
        WindowDisplay source = WindowTransferTestContext.Display(1, WindowBounds.Create(-4000, -3000, 4000, 3000), scales[0]);
        WindowDisplay target = WindowTransferTestContext.Display(2, area, scales[1]);
        WindowDisplaySnapshot snapshot = WindowDisplaySnapshot.Create(source, [source, target]);
        WindowPlacement placement = WindowTransferTestContext.Placement(bounds, snapshot, minimum, WindowPresenterState.Restored);
        return WindowAdjustmentPlanner.Plan(placement, WindowAdjustmentAction.NextDisplay, WindowTransferInput.Create(snapshot));
    }

    private static void AssertCannotFit(WindowAdjustmentPlan plan)
    {
        Assert.AreSame(WindowAdjustmentRefusal.TargetCannotFit, Assert.IsInstanceOfType<WindowRefusedPlan>(plan).Reason);
    }
}
