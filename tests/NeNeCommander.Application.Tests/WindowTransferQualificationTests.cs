using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Windowing;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves phase-specific immutable qualifications without OS calls or cached XAML scale assumptions.</summary>
[TestClass]
public sealed class WindowTransferQualificationTests
{
    /// <summary>The first call requires complete equal facts, but enumeration order and XAML scale do not matter.</summary>
    [TestMethod]
    public void CanStartWhenFactsMatchIgnoresOrderAndStaleXamlScale()
    {
        WindowTransferPlan plan = WindowTransferTestContext.Plan;
        WindowDisplaySnapshot reordered = WindowDisplaySnapshot.Create(plan.Displays.Current, [plan.Displays[1], plan.Displays[0]]);
        WindowPlacement placement = WindowTransferTestContext.Placement(plan.Source.Bounds, reordered,
            WindowSizeConstraint.Create(17d, 0, 0), WindowPresenterState.Restored);

        Assert.IsTrue(WindowTransferQualification.CanStart(plan, placement, WindowTransferInput.Create(reordered)));
    }

    /// <summary>Changing any initial display fact, even on a third display, invalidates the first call.</summary>
    [TestMethod]
    public void CanStartWhenAnyInitialDisplayFactChangesRefuses()
    {
        WindowTransferPlan plan = WindowTransferTestContext.Plan;
        WindowDisplay changedScale = WindowDisplay.Create(plan.Target.Identity, plan.Target.WorkArea, 2d);
        WindowDisplay replacement = WindowTransferTestContext.Display(99, plan.Target.WorkArea, plan.Target.Scale);
        WindowDisplay changedArea = WindowDisplay.Create(plan.Target.Identity, WindowBounds.Create(0, 1, 2560, 1400), plan.Target.Scale);
        WindowDisplay extra = WindowTransferTestContext.Display(40, WindowBounds.Create(3000, 0, 1000, 800), 1d);
        foreach (WindowDisplay changed in new[] { changedScale, replacement, changedArea })
        {
            WindowDisplaySnapshot snapshot = WindowDisplaySnapshot.Create(plan.Displays.Current, [plan.Displays.Current, changed]);
            Assert.IsFalse(WindowTransferQualification.CanStart(plan, WindowTransferTestContext.At(snapshot, plan.Source.Bounds), WindowTransferInput.Create(snapshot)));
        }
        WindowDisplaySnapshot added = WindowDisplaySnapshot.Create(plan.Displays.Current, [plan.Displays.Current, plan.Target, extra]);
        Assert.IsFalse(WindowTransferQualification.CanStart(plan, WindowTransferTestContext.At(added, plan.Source.Bounds), WindowTransferInput.Create(added)));
        WindowDisplaySnapshot removed = WindowDisplaySnapshot.Create(plan.Displays.Current, [plan.Displays.Current]);
        Assert.IsFalse(WindowTransferQualification.CanStart(plan, WindowTransferTestContext.At(removed, plan.Source.Bounds), WindowTransferInput.Create(removed)));
    }

    /// <summary>A current-ID switch with identical mirrored facts is still a different source observation.</summary>
    [TestMethod]
    public void CanStartWhenCurrentIdentityChangesRefusesEvenWithSameArea()
    {
        WindowDisplay source = WindowTransferTestContext.Source;
        WindowDisplay mirror = WindowTransferTestContext.Display(11, source.WorkArea, source.Scale);
        WindowDisplaySnapshot original = WindowDisplaySnapshot.Create(source, [source, mirror, WindowTransferTestContext.Target]);
        WindowPlacement placement = WindowTransferTestContext.At(original, WindowTransferTestContext.Original.Bounds);
        WindowTransferPlan plan = WindowTransferTestContext.Transfer(placement, WindowAdjustmentAction.NextDisplay, original);
        WindowDisplaySnapshot changed = WindowDisplaySnapshot.Create(mirror, original);
        Assert.IsFalse(WindowTransferQualification.CanStart(plan, placement, WindowTransferInput.Create(changed)));
    }

    /// <summary>Moved source bounds and inconsistent placement topology cannot authorize an initial effect.</summary>
    [TestMethod]
    public void CanStartWhenSourcePlacementChangesRefuses()
    {
        WindowTransferPlan plan = WindowTransferTestContext.Plan;
        WindowPlacement moved = WindowTransferTestContext.At(plan.Displays, WindowBounds.Create(-1799, 100, 800, 600));
        Assert.IsFalse(WindowTransferQualification.CanStart(plan, moved, WindowTransferInput.Create(plan.Displays)));
        WindowDisplaySnapshot onlySource = WindowDisplaySnapshot.Create(plan.Displays.Current, [plan.Displays.Current]);
        WindowPlacement inconsistent = WindowTransferTestContext.At(onlySource, plan.Source.Bounds);
        Assert.IsFalse(WindowTransferQualification.CanStart(plan, inconsistent, WindowTransferInput.Create(plan.Displays)));
    }

    /// <summary>The final call requires the intended target facts but allows unrelated topology changes after the first call.</summary>
    [TestMethod]
    public void CanFinishWhenTargetMatchesAcceptsIntermediateBoundsAndUnrelatedDisplayRemoval()
    {
        WindowTransferPlan plan = WindowTransferTestContext.Plan;
        WindowDisplaySnapshot arrived = WindowDisplaySnapshot.Create(plan.Target, [plan.Target]);
        WindowPlacement intermediate = WindowTransferTestContext.Placement(plan.Geometry.Initial, arrived,
            WindowSizeConstraint.Create(23d, 0, 0), WindowPresenterState.Restored);

        Assert.IsTrue(WindowTransferQualification.CanFinish(plan, intermediate, WindowTransferInput.Create(arrived)));
        Assert.IsFalse(WindowTransferQualification.HasObservedResult(plan, intermediate, WindowTransferInput.Create(arrived)));
        WindowPlacement final = WindowTransferTestContext.At(arrived, plan.Geometry.Final);
        Assert.IsTrue(WindowTransferQualification.HasObservedResult(plan, final, WindowTransferInput.Create(arrived)));
    }

    /// <summary>A new target identity, scale or area, or still being on the source, forbids the final call.</summary>
    [TestMethod]
    public void CanFinishWhenDestinationFactsChangeRefuses()
    {
        WindowTransferPlan plan = WindowTransferTestContext.Plan;
        WindowDisplay[] changed =
        [
            plan.Displays.Current,
            WindowTransferTestContext.Display(99, plan.Target.WorkArea, plan.Target.Scale),
            WindowDisplay.Create(plan.Target.Identity, plan.Target.WorkArea, 2d),
            WindowDisplay.Create(plan.Target.Identity, WindowBounds.Create(0, 0, 2559, 1400), plan.Target.Scale),
        ];
        foreach (WindowDisplay current in changed)
        {
            WindowDisplaySnapshot snapshot = WindowDisplaySnapshot.Create(current, [current]);
            WindowPlacement placement = WindowTransferTestContext.At(snapshot, plan.Geometry.Final);
            Assert.IsFalse(WindowTransferQualification.CanFinish(plan, placement, WindowTransferInput.Create(snapshot)));
            Assert.IsFalse(WindowTransferQualification.HasObservedResult(plan, placement, WindowTransferInput.Create(snapshot)));
        }
        WindowDisplaySnapshot arrived = WindowDisplaySnapshot.Create(plan.Target, plan.Displays);
        Assert.IsFalse(WindowTransferQualification.CanFinish(plan, plan.Source, WindowTransferInput.Create(arrived)));
    }

    /// <summary>Visibility/read failure, non-normal presenter and either changed minimum block every phase.</summary>
    [TestMethod]
    public void QualifyWhenWindowStateOrMinimumChangesRefusesEveryPhase()
    {
        WindowTransferPlan plan = WindowTransferTestContext.Plan;
        foreach (WindowTransferInput unavailable in new[] { WindowTransferInput.Hidden, WindowTransferInput.Unavailable, WindowTransferInput.NotRequested })
        {
            Assert.IsFalse(WindowTransferQualification.CanStart(plan, plan.Source, unavailable));
            Assert.IsFalse(WindowTransferQualification.CanFinish(plan, plan.Source, unavailable));
        }
        WindowSizeConstraint[] minima = [WindowSizeConstraint.Create(1d, 1, 0), WindowSizeConstraint.Create(1d, 0, 1)];
        foreach (WindowSizeConstraint minimum in minima)
        {
            AssertWindowRefused(plan, minimum, WindowPresenterState.Restored);
        }
        AssertWindowRefused(plan, plan.Source.SizeConstraint, WindowPresenterState.Maximized);
        AssertWindowRefused(plan, plan.Source.SizeConstraint, WindowPresenterState.Minimized);
        AssertWindowRefused(plan, plan.Source.SizeConstraint, WindowPresenterState.Unavailable);
        AssertWindowRefused(plan, plan.Source.SizeConstraint, WindowPresenterState.NotOverlapped);
    }

    /// <summary>All public qualification predicates reject absent facts.</summary>
    [TestMethod]
    public void QualifyWhenRequiredPartIsAbsentThrows()
    {
        WindowTransferPlan plan = WindowTransferTestContext.Plan;
        foreach (string method in new[] { "CanStart", "CanFinish", "HasObservedResult" })
        {
            NullGuardAssertions.AssertStaticNullGuard(typeof(WindowTransferQualification), method, [null, plan.Source, WindowTransferInput.Hidden]);
            NullGuardAssertions.AssertStaticNullGuard(typeof(WindowTransferQualification), method, [plan, null, WindowTransferInput.Hidden]);
            NullGuardAssertions.AssertStaticNullGuard(typeof(WindowTransferQualification), method, [plan, plan.Source, null]);
        }
    }

    private static void AssertWindowRefused(WindowTransferPlan plan, WindowSizeConstraint minimum, WindowPresenterState state)
    {
        WindowPlacement source = WindowTransferTestContext.Placement(plan.Source.Bounds, plan.Displays, minimum, state);
        Assert.IsFalse(WindowTransferQualification.CanStart(plan, source, WindowTransferInput.Create(plan.Displays)));
        WindowDisplaySnapshot arrived = WindowDisplaySnapshot.Create(plan.Target, plan.Displays);
        WindowPlacement destination = WindowTransferTestContext.Placement(plan.Geometry.Final, arrived, minimum, state);
        Assert.IsFalse(WindowTransferQualification.CanFinish(plan, destination, WindowTransferInput.Create(arrived)));
        Assert.IsFalse(WindowTransferQualification.HasObservedResult(plan, destination, WindowTransferInput.Create(arrived)));
    }
}
