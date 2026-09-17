using System;

namespace NeNeCommander.Application.Windowing;

/// <summary>Asks the host to size the window to exactly these physical-pixel bounds, keeping its top-left corner.</summary>
public sealed record WindowResizePlan : WindowAdjustmentPlan
{
    internal WindowResizePlan(WindowBounds bounds)
    {
        ArgumentNullException.ThrowIfNull(bounds);
        Bounds = bounds;
    }

    /// <summary>Gets the requested window rectangle in physical pixels.</summary>
    public WindowBounds Bounds { get; }
}
