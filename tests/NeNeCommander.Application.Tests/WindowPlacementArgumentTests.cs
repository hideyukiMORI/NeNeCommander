using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Windowing;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves placement boundaries reject absent required values before retaining invalid state.</summary>
[TestClass]
public sealed class WindowPlacementArgumentTests
{
    /// <summary>Proves saved placement rejects either absent component with the caller's parameter name.</summary>
    [TestMethod]
    [DataRow("bounds")]
    [DataRow("state")]
    public void CreateWhenRequiredComponentIsAbsentNamesParameter(string parameter)
    {
        WindowBounds bounds = WindowBounds.Create(20, 30, 600, 400);
        object?[] arguments = parameter == "bounds"
            ? [null, WindowPresenterState.Restored] : [bounds, null];

        ArgumentNullException error = NullGuardAssertions.AssertInternalMethodNullGuard(typeof(SavedWindowPlacement), nameof(SavedWindowPlacement.Create), null, arguments);

        Assert.AreEqual(parameter, error.ParamName);
    }

    /// <summary>Proves an invalid observation cannot replace the last valid normal placement.</summary>
    [TestMethod]
    [DataRow("bounds")]
    [DataRow("state")]
    public void ObserveWhenRequiredComponentIsAbsentPreservesCurrent(string parameter)
    {
        WindowBounds bounds = WindowBounds.Create(20, 30, 600, 400);
        WindowPlacementTracker tracker = new();
        tracker.Observe(bounds, WindowPresenterState.Restored);
        SavedWindowPlacement? before = tracker.Current;
        object?[] arguments = parameter == "bounds"
            ? [null, WindowPresenterState.Minimized] : [bounds, null];

        ArgumentNullException error = NullGuardAssertions.AssertInternalMethodNullGuard(
            typeof(WindowPlacementTracker), nameof(WindowPlacementTracker.Observe), tracker, arguments);

        Assert.AreEqual(parameter, error.ParamName);
        Assert.AreSame(before, tracker.Current);
    }

    /// <summary>Proves even a saved rectangle cannot bypass required startup input validation.</summary>
    [TestMethod]
    [DataRow("initial")]
    [DataRow("workAreas")]
    public void PlanWhenRequiredContextIsAbsentNamesParameter(string parameter)
    {
        WindowBounds bounds = WindowBounds.Create(20, 30, 600, 400);
        SavedWindowPlacement saved = SavedWindowPlacement.Create(bounds, WindowPresenterState.Restored);
        WindowWorkAreas areas = WindowWorkAreas.Create(bounds, [bounds]);
        object?[] arguments = parameter == "initial" ? [saved, null, areas] : [saved, bounds, null];

        ArgumentNullException error = NullGuardAssertions.AssertInternalMethodNullGuard(typeof(WindowRestorePlanner), nameof(WindowRestorePlanner.Plan), null, arguments);

        Assert.AreEqual(parameter, error.ParamName);
    }

    /// <summary>Proves the bounds-operation projection rejects an absent decided placement.</summary>
    [TestMethod]
    public void PlanNormalBoundsWhenPlacementIsAbsentNamesParameter()
    {
        ArgumentNullException error = NullGuardAssertions.AssertInternalMethodNullGuard(typeof(WindowRestorePlanner), nameof(WindowRestorePlanner.PlanNormalBounds), null, [null]);

        Assert.AreEqual("placement", error.ParamName);
    }
}
