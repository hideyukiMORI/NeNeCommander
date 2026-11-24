using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Windowing;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves geometric cycling, duplicate grouping and refusal independently of enumeration order.</summary>
[TestClass]
public sealed class WindowTransferPlannerTests
{
    /// <summary>Permutations, negative coordinates and a staggered display preserve the same full cycle.</summary>
    [TestMethod]
    public void PlanWhenEnumerationChangesKeepsGeometricCycleAndWrap()
    {
        WindowDisplay left = WindowTransferTestContext.Source;
        WindowDisplay middle = WindowTransferTestContext.Target;
        WindowDisplay upper = WindowTransferTestContext.Display(30, WindowBounds.Create(0, -1440, 2560, 1440), 1.5d);
        WindowDisplay[] order = [left, upper, middle];
        foreach (WindowDisplay[] enumeration in Permutations(order))
        {
            for (int index = 0; index < order.Length; index++)
            {
                WindowDisplaySnapshot snapshot = WindowDisplaySnapshot.Create(order[index], enumeration);
                WindowPlacement placement = WindowTransferTestContext.At(snapshot, WindowBounds.Create(0, 0, 600, 400));
                WindowTransferPlan next = WindowTransferTestContext.Transfer(placement, WindowAdjustmentAction.NextDisplay, snapshot);
                WindowTransferPlan previous = WindowTransferTestContext.Transfer(placement, WindowAdjustmentAction.PreviousDisplay, snapshot);
                Assert.AreSame(order[(index + 1) % 3], next.Target);
                Assert.AreSame(order[(index + 2) % 3], previous.Target);
            }
        }
    }

    /// <summary>Co-located distinct areas use width then height; exact duplicates use the lowest ID.</summary>
    [TestMethod]
    public void PlanWhenAreasShareOriginUsesDimensionsAndLowestDuplicateIdentity()
    {
        WindowDisplay small = WindowTransferTestContext.Display(40, WindowBounds.Create(0, 0, 800, 600), 1d);
        WindowDisplay tall = WindowTransferTestContext.Display(30, WindowBounds.Create(0, 0, 800, 900), 1d);
        WindowDisplay wide = WindowTransferTestContext.Display(20, WindowBounds.Create(0, 0, 1000, 600), 1d);
        WindowDisplay duplicate = WindowTransferTestContext.Display(10, tall.WorkArea, 1.5d);
        WindowDisplaySnapshot snapshot = WindowDisplaySnapshot.Create(small, [wide, tall, small, duplicate]);

        WindowTransferPlan plan = WindowTransferTestContext.Transfer(
            WindowTransferTestContext.At(snapshot, WindowBounds.Create(100, 100, 400, 300)), WindowAdjustmentAction.NextDisplay, snapshot);

        Assert.AreSame(duplicate, plan.Target);
        WindowDisplaySnapshot fromDuplicate = WindowDisplaySnapshot.Create(tall, snapshot);
        WindowTransferPlan afterDuplicate = WindowTransferTestContext.Transfer(
            WindowTransferTestContext.At(fromDuplicate, plan.Geometry.Final), WindowAdjustmentAction.NextDisplay, fromDuplicate);
        Assert.AreSame(wide, afterDuplicate.Target);
    }

    /// <summary>Mirrored displays are one destination even with different IDs and scale observations.</summary>
    [TestMethod]
    public void PlanWhenOnlyOneDistinctAreaExistsRefusesWithoutGeometry()
    {
        WindowDisplay source = WindowTransferTestContext.Source;
        WindowDisplay duplicate = WindowTransferTestContext.Display(99, source.WorkArea, 2d);
        WindowDisplaySnapshot snapshot = WindowDisplaySnapshot.Create(duplicate, [duplicate, source]);
        AssertRefused(WindowTransferTestContext.At(snapshot, WindowBounds.Create(-1000, 0, 400, 300)),
            WindowTransferInput.Create(snapshot), WindowAdjustmentRefusal.NoOtherDisplay);
    }

    /// <summary>Missing, hidden or inconsistent information cannot silently choose a guessed display.</summary>
    [TestMethod]
    public void PlanWhenTransferFactsAreIncompleteOrInconsistentRefuses()
    {
        WindowPlacement original = WindowTransferTestContext.Original;
        AssertRefused(original, WindowTransferInput.Hidden, WindowAdjustmentRefusal.WindowIsHidden);
        AssertRefused(original, WindowTransferInput.Unavailable, WindowAdjustmentRefusal.DisplayInformationUnavailable);
        AssertRefused(original, WindowTransferInput.NotRequested, WindowAdjustmentRefusal.DisplayInformationUnavailable);
        WindowDisplaySnapshot otherCurrent = WindowDisplaySnapshot.Create(WindowTransferTestContext.Target, WindowTransferTestContext.Displays);
        AssertRefused(original, WindowTransferInput.Create(otherCurrent), WindowAdjustmentRefusal.DisplayInformationUnavailable);
        WindowDisplaySnapshot missingTarget = WindowDisplaySnapshot.Create(WindowTransferTestContext.Source, [WindowTransferTestContext.Source]);
        AssertRefused(original, WindowTransferInput.Create(missingTarget), WindowAdjustmentRefusal.DisplayInformationUnavailable);
    }

    /// <summary>All non-normal presenter states retain the existing refusal precedence.</summary>
    [TestMethod]
    public void PlanWhenPresenterIsNotRestoredKeepsItsExplicitRefusal()
    {
        AssertPresenterRefused(WindowPresenterState.Maximized, WindowAdjustmentRefusal.WindowIsMaximized);
        AssertPresenterRefused(WindowPresenterState.Minimized, WindowAdjustmentRefusal.WindowIsMinimized);
        AssertPresenterRefused(WindowPresenterState.NotOverlapped, WindowAdjustmentRefusal.PresenterIsNotOverlapped);
        AssertPresenterRefused(WindowPresenterState.Unavailable, WindowAdjustmentRefusal.PlacementUnavailable);
    }

    /// <summary>Ordinary movement never requires the transfer capability or visibility input.</summary>
    [TestMethod]
    public void PlanWhenActionIsOrdinaryIgnoresTransferOnlyRefusals()
    {
        WindowAdjustmentPlan ordinary = WindowAdjustmentPlanner.Plan(WindowTransferTestContext.Original,
            WindowAdjustmentAction.MoveRight, WindowTransferInput.NotRequested);
        Assert.AreEqual(ordinary, WindowAdjustmentPlanner.Plan(WindowTransferTestContext.Original,
            WindowAdjustmentAction.MoveRight, WindowTransferInput.Hidden));
        Assert.AreEqual(ordinary, WindowAdjustmentPlanner.Plan(WindowTransferTestContext.Original,
            WindowAdjustmentAction.MoveRight, WindowTransferInput.Unavailable));
        NullGuardAssertions.AssertStaticNullGuard(typeof(WindowAdjustmentPlanner), "Plan",
            [WindowTransferTestContext.Original, WindowAdjustmentAction.NextDisplay, null]);
    }

    private static IEnumerable<WindowDisplay[]> Permutations(WindowDisplay[] order)
    {
        foreach (WindowDisplay first in order)
        {
            foreach (WindowDisplay second in order.Where(display => display != first))
            {
                yield return [first, second, order.Single(display => display != first && display != second)];
            }
        }
    }

    private static void AssertPresenterRefused(WindowPresenterState state, WindowAdjustmentRefusal refusal)
    {
        WindowPlacement placement = WindowTransferTestContext.Placement(WindowTransferTestContext.Original.Bounds,
            WindowTransferTestContext.Displays, WindowTransferTestContext.Original.SizeConstraint, state);
        AssertRefused(placement, WindowTransferInput.Create(WindowTransferTestContext.Displays), refusal);
    }

    private static void AssertRefused(WindowPlacement placement, WindowTransferInput input, WindowAdjustmentRefusal reason)
    {
        Assert.AreSame(reason, Assert.IsInstanceOfType<WindowRefusedPlan>(WindowAdjustmentPlanner.Plan(
            placement, WindowAdjustmentAction.NextDisplay, input)).Reason);
    }
}
