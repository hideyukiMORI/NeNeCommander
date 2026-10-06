using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Windowing;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves the closed placement facts the host translates and the planner reads.</summary>
[TestClass]
public sealed class WindowPlacementTests
{
    /// <summary>Proves bounds keep their physical-pixel edges and derive the far edges.</summary>
    [TestMethod]
    public void CreateBoundsWhenExtentIsPositiveDerivesRightAndBottom()
    {
        WindowBounds bounds = WindowBounds.Create(-30, 20, 1, 7);

        Assert.AreEqual(-30, bounds.Left);
        Assert.AreEqual(20, bounds.Top);
        Assert.AreEqual(1, bounds.Width);
        Assert.AreEqual(7, bounds.Height);
        Assert.AreEqual(-29, bounds.Right);
        Assert.AreEqual(27, bounds.Bottom);
    }

    /// <summary>Proves bounds reject an empty or negative width or height.</summary>
    [TestMethod]
    public void CreateBoundsWhenExtentIsNotPositiveThrowsArgumentOutOfRangeException()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => WindowBounds.Create(0, 0, 0, 1));
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => WindowBounds.Create(0, 0, 1, 0));
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => WindowBounds.Create(0, 0, -1, 1));
    }

    /// <summary>Proves the size constraint keeps a positive scale and non-negative minimums.</summary>
    [TestMethod]
    public void CreateSizeConstraintWhenValuesAreValidKeepsThem()
    {
        WindowSizeConstraint constraint = WindowSizeConstraint.Create(1.75, 0, 480);

        Assert.AreEqual(1.75, constraint.RasterizationScale);
        Assert.AreEqual(0, constraint.PreferredMinimumWidth);
        Assert.AreEqual(480, constraint.PreferredMinimumHeight);
    }

    /// <summary>Proves the size constraint rejects a non-positive scale and a negative minimum.</summary>
    [TestMethod]
    public void CreateSizeConstraintWhenValueIsOutOfRangeThrowsArgumentOutOfRangeException()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => WindowSizeConstraint.Create(0d, 0, 0));
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => WindowSizeConstraint.Create(-1d, 0, 0));
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => WindowSizeConstraint.Create(1d, -1, 0));
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => WindowSizeConstraint.Create(1d, 0, -1));
    }

    /// <summary>Proves the work areas own a copy of the attached set, so a later caller edit cannot change them.</summary>
    [TestMethod]
    public void CreateWorkAreasWhenCurrentIsAttachedOwnsACopy()
    {
        WindowBounds current = WindowBounds.Create(0, 0, 1920, 1040);
        WindowBounds other = WindowBounds.Create(1920, 0, 1920, 1040);
        List<WindowBounds> attached = [other, current];

        WindowWorkAreas areas = WindowWorkAreas.Create(current, attached);
        attached.Clear();

        Assert.AreSame(current, areas.Current);
        Assert.HasCount(2, areas.Attached);
        Assert.AreSame(other, areas.Attached[0]);
        Assert.AreSame(current, areas.Attached[1]);
        _ = Assert.ThrowsExactly<NotSupportedException>(
            () => ((ICollection<WindowBounds>)areas.Attached).Add(other));
    }

    /// <summary>Proves the current display's work area must be one of the attached work areas.</summary>
    [TestMethod]
    public void CreateWorkAreasWhenCurrentIsNotAttachedThrowsArgumentException()
    {
        WindowBounds current = WindowBounds.Create(0, 0, 1920, 1040);

        ArgumentException failure = Assert.ThrowsExactly<ArgumentException>(() => WindowWorkAreas.Create(
            current,
            [WindowBounds.Create(1920, 0, 1920, 1040)]));

        Assert.AreEqual("attached", failure.ParamName);
    }

    /// <summary>Proves a placement keeps exactly the facts it was created from.</summary>
    [TestMethod]
    public void CreatePlacementWhenFactsAreValidKeepsThem()
    {
        WindowBounds bounds = WindowBounds.Create(10, 20, 300, 200);
        WindowSizeConstraint constraint = WindowSizeConstraint.Create(1.5, 0, 0);
        WindowWorkAreas areas = WindowWorkAreas.Create(bounds, [bounds]);

        WindowPlacement placement = WindowPlacement.Create(bounds, WindowPresenterState.Maximized, constraint, areas);

        Assert.AreSame(bounds, placement.Bounds);
        Assert.AreSame(WindowPresenterState.Maximized, placement.PresenterState);
        Assert.AreSame(constraint, placement.SizeConstraint);
        Assert.AreSame(areas, placement.WorkAreas);
    }

    /// <summary>Proves every placement factory rejects an absent fact.</summary>
    [TestMethod]
    public void CreateWhenAPartIsNullThrowsArgumentNullException()
    {
        WindowBounds bounds = WindowBounds.Create(0, 0, 10, 10);
        WindowSizeConstraint constraint = WindowSizeConstraint.Create(1d, 0, 0);
        WindowWorkAreas areas = WindowWorkAreas.Create(bounds, [bounds]);

        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => WindowPlacement.Create(null!, WindowPresenterState.Restored, constraint, areas));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => WindowPlacement.Create(bounds, null!, constraint, areas));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => WindowPlacement.Create(bounds, WindowPresenterState.Restored, null!, areas));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => WindowPlacement.Create(bounds, WindowPresenterState.Restored, constraint, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => WindowWorkAreas.Create(null!, [bounds]));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => WindowWorkAreas.Create(bounds, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WindowMovePlan(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WindowResizePlan(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WindowRefusedPlan(null!));
    }
}
