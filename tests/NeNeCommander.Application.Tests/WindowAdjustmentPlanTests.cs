using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Windowing;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves closure preserves the planner's existing record values and immutable payload.</summary>
[TestClass]
public sealed class WindowAdjustmentPlanTests
{
    /// <summary>Proves root and concrete copies preserve the exact planned bounds and equality.</summary>
    [TestMethod]
    public void WithWhenPlanCarriesBoundsPreservesRootAndConcretePayload()
    {
        WindowBounds desktop = WindowBounds.Create(0, 0, 1920, 1080);
        WindowPlacement placement = WindowPlacement.Create(WindowBounds.Create(100, 100, 800, 600),
            WindowPresenterState.Restored, WindowSizeConstraint.Create(1, 0, 0), WindowWorkAreas.Create(desktop, [desktop]));
        WindowAdjustmentPlan original = WindowAdjustmentPlanner.Plan(placement, WindowAdjustmentAction.MoveLeft);
        WindowMovePlan concrete = Assert.IsInstanceOfType<WindowMovePlan>(original);

        WindowAdjustmentPlan copied = original with { };
        WindowMovePlan concreteCopy = concrete with { };

        Assert.AreNotSame(original, copied);
        Assert.AreEqual(original, copied);
        Assert.AreEqual(original.GetHashCode(), copied.GetHashCode());
        Assert.AreSame(concrete.Bounds, Assert.IsInstanceOfType<WindowMovePlan>(copied).Bounds);
        Assert.AreEqual(concrete, concreteCopy);
        Assert.AreNotSame(concrete, concreteCopy);
        Assert.AreSame(concrete.Bounds, concreteCopy.Bounds);
        Assert.AreEqual(68, concreteCopy.Bounds.Left);
        Assert.AreNotEqual(original, WindowAdjustmentPlanner.Plan(placement, WindowAdjustmentAction.MoveRight));
    }

    /// <summary>Proves private nested cases keep their distinct types and value equality when copied.</summary>
    [TestMethod]
    public void WithWhenPlanHasPrivateCasePreservesCaseDistinction()
    {
        WindowAdjustmentPlan original = WindowAdjustmentPlan.Maximize;

        WindowAdjustmentPlan copied = original with { };

        Assert.AreNotSame(original, copied);
        Assert.AreEqual(original.GetType(), copied.GetType());
        Assert.AreEqual(original, copied);
        Assert.AreEqual(original.GetHashCode(), copied.GetHashCode());
        Assert.AreNotEqual(WindowAdjustmentPlan.Restore, copied);
    }
}
