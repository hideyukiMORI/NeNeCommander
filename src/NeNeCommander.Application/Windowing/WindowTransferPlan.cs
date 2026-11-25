using System;

namespace NeNeCommander.Application.Windowing;

/// <summary>Asks the existing adapter to apply two precomputed rectangles under exact fresh qualifications.</summary>
public sealed record WindowTransferPlan : WindowAdjustmentPlan
{
    internal WindowTransferPlan(
        WindowPlacement source,
        WindowDisplaySnapshot displays,
        WindowDisplay target,
        WindowTransferGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(displays);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(geometry);
        Source = source;
        Displays = displays;
        Target = target;
        Geometry = geometry;
    }

    /// <summary>Gets the source placement whose physical bounds and declared minima qualify the first effect.</summary>
    public WindowPlacement Source { get; }

    /// <summary>Gets the complete display facts used to choose this destination.</summary>
    public WindowDisplaySnapshot Displays { get; }

    /// <summary>Gets the exact destination identity, work area and reported scale.</summary>
    public WindowDisplay Target { get; }

    /// <summary>Gets the pure calculation's two exact rectangles.</summary>
    public WindowTransferGeometry Geometry { get; }

    private protected override void RestrictDerivation()
    {
    }
}
