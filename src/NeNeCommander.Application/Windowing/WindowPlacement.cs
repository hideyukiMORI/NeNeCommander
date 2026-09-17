using System;

namespace NeNeCommander.Application.Windowing;

/// <summary>
/// Represents one complete reading of the application window's geometry in physical pixels, taken
/// by the host immediately before a window action is decided. Application never reads or writes the
/// window, so a placement is always a fresh input value and is never cached as state.
/// </summary>
public sealed record WindowPlacement
{
    private WindowPlacement(
        WindowBounds bounds,
        WindowPresenterState presenterState,
        WindowSizeConstraint sizeConstraint,
        WindowWorkAreas workAreas)
    {
        Bounds = bounds;
        PresenterState = presenterState;
        SizeConstraint = sizeConstraint;
        WorkAreas = workAreas;
    }

    /// <summary>
    /// Gets the placement the host reports when it could not read the window at all. Its rectangle
    /// is a placeholder that no decision reads, because every action is refused in this state.
    /// </summary>
    public static WindowPlacement Unavailable { get; } = new(
        WindowBounds.Create(0, 0, 1, 1),
        WindowPresenterState.Unavailable,
        WindowSizeConstraint.Create(1d, 0, 0),
        WindowWorkAreas.Create(
            WindowBounds.Create(0, 0, 1, 1),
            [WindowBounds.Create(0, 0, 1, 1)]));

    /// <summary>Gets the window rectangle in physical pixels.</summary>
    public WindowBounds Bounds { get; }

    /// <summary>Gets the closed presenter state of the window.</summary>
    public WindowPresenterState PresenterState { get; }

    /// <summary>Gets the rasterization scale and the declared preferred minimum of the window.</summary>
    public WindowSizeConstraint SizeConstraint { get; }

    /// <summary>Gets the work area of the window's display and of every attached display.</summary>
    public WindowWorkAreas WorkAreas { get; }

    /// <summary>Creates one placement from facts the host adapter has already translated.</summary>
    /// <param name="bounds">Window rectangle in physical pixels.</param>
    /// <param name="presenterState">Closed presenter state.</param>
    /// <param name="sizeConstraint">Rasterization scale and declared preferred minimum.</param>
    /// <param name="workAreas">Work area of the window's display and of every attached display.</param>
    /// <returns>A complete immutable placement.</returns>
    public static WindowPlacement Create(
        WindowBounds bounds,
        WindowPresenterState presenterState,
        WindowSizeConstraint sizeConstraint,
        WindowWorkAreas workAreas)
    {
        ArgumentNullException.ThrowIfNull(bounds);
        ArgumentNullException.ThrowIfNull(presenterState);
        ArgumentNullException.ThrowIfNull(sizeConstraint);
        ArgumentNullException.ThrowIfNull(workAreas);
        return new WindowPlacement(bounds, presenterState, sizeConstraint, workAreas);
    }
}
