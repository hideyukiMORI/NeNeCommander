using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Windowing;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves only normal rectangles survive minimize/maximize state transitions.</summary>
[TestClass]
public sealed class WindowPlacementTrackerTests
{
    /// <summary>Proves no fabricated normal bounds are saved from an initial unavailable or iconic state.</summary>
    [TestMethod]
    public void ObserveWhenNoNormalRectangleExistsKeepsAbsence()
    {
        WindowPlacementTracker tracker = new();
        WindowBounds placeholder = WindowBounds.Create(-32000, -32000, 1, 1);
        foreach (WindowPresenterState state in new[]
        {
            WindowPresenterState.Maximized, WindowPresenterState.Minimized,
            WindowPresenterState.NotOverlapped, WindowPresenterState.Unavailable,
        })
        {
            tracker.Observe(placeholder, state);
            Assert.IsNull(tracker.Current);
        }
    }

    /// <summary>Proves maximization remembers the normal size and minimization retains the preceding mode.</summary>
    [TestMethod]
    public void ObserveWhenMaximizedThenMinimizedRetainsNormalBoundsAndMaximizedMode()
    {
        WindowPlacementTracker tracker = new();
        WindowBounds normal = WindowBounds.Create(-1200, 30, 900, 650);
        tracker.Observe(normal, WindowPresenterState.Restored);
        tracker.Observe(WindowBounds.Create(-1920, 0, 1920, 1040), WindowPresenterState.Maximized);
        tracker.Observe(WindowBounds.Create(-32000, -32000, 200, 40), WindowPresenterState.Minimized);
        tracker.Observe(WindowBounds.Create(0, 0, 1, 1), WindowPresenterState.Unavailable);

        Assert.AreEqual(SavedWindowPlacement.Create(normal, WindowPresenterState.Maximized), tracker.Current);
    }

    /// <summary>Proves normal movement after restore replaces both prior position and maximized mode.</summary>
    [TestMethod]
    public void ObserveWhenRestoredAndMovedUsesLatestNormalGeometry()
    {
        WindowPlacementTracker tracker = new();
        tracker.Observe(WindowBounds.Create(20, 20, 900, 650), WindowPresenterState.Restored);
        tracker.Observe(WindowBounds.Create(0, 0, 1920, 1040), WindowPresenterState.Maximized);
        WindowBounds latest = WindowBounds.Create(400, 200, 1200, 700);
        tracker.Observe(latest, WindowPresenterState.Restored);
        tracker.Observe(WindowBounds.Create(-32000, -32000, 200, 40), WindowPresenterState.Minimized);

        Assert.AreEqual(SavedWindowPlacement.Create(latest, WindowPresenterState.Restored), tracker.Current);
    }

    /// <summary>Proves unsupported presenter states cannot become a saved value.</summary>
    [TestMethod]
    public void CreateWhenStateCannotBeRestoredRejectsCallerDefect()
    {
        WindowBounds normal = WindowBounds.Create(0, 0, 100, 100);
        foreach (WindowPresenterState state in new[]
        {
            WindowPresenterState.Minimized, WindowPresenterState.NotOverlapped, WindowPresenterState.Unavailable,
        })
        {
            ArgumentException error = Assert.ThrowsExactly<ArgumentException>(() => SavedWindowPlacement.Create(normal, state));
            Assert.AreEqual("state", error.ParamName);
        }
    }
}
