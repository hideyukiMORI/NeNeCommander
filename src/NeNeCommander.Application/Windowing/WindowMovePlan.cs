using System;

namespace NeNeCommander.Application.Windowing;

/// <summary>Asks the host to place the window at exactly these physical-pixel bounds, keeping its size.</summary>
public sealed record WindowMovePlan : WindowAdjustmentPlan
{
    internal WindowMovePlan(WindowBounds bounds)
    {
        ArgumentNullException.ThrowIfNull(bounds);
        Bounds = bounds;
    }

    /// <summary>Gets the requested window rectangle in physical pixels.</summary>
    public WindowBounds Bounds { get; }
}
