using System;

namespace NeNeCommander.Application.Windowing;

/// <summary>
/// Carries the two facts that size one adjustment: the window's current rasterization scale, which
/// turns the device-independent step into physical pixels, and the presenter's declared preferred
/// minimum, which floors a shrink. A dimension whose minimum is undeclared is zero, which
/// constrains nothing, so the value is not a sentinel and no nullable minimum exists. Both facts
/// travel as one value so that <see cref="WindowPlacement"/> stays within the CS-013 parameter
/// ceiling.
/// </summary>
public sealed record WindowSizeConstraint
{
    private WindowSizeConstraint(
        double rasterizationScale,
        int preferredMinimumWidth,
        int preferredMinimumHeight)
    {
        RasterizationScale = rasterizationScale;
        PreferredMinimumWidth = preferredMinimumWidth;
        PreferredMinimumHeight = preferredMinimumHeight;
    }

    /// <summary>Gets the window's current rasterization scale, where 1.0 is 100 percent.</summary>
    public double RasterizationScale { get; }

    /// <summary>Gets the presenter's declared preferred minimum width in physical pixels, or zero.</summary>
    public int PreferredMinimumWidth { get; }

    /// <summary>Gets the presenter's declared preferred minimum height in physical pixels, or zero.</summary>
    public int PreferredMinimumHeight { get; }

    /// <summary>Creates the sizing facts an adapter read from the window and its presenter.</summary>
    /// <param name="rasterizationScale">Positive rasterization scale of the window.</param>
    /// <param name="preferredMinimumWidth">Declared preferred minimum width, or zero when none is declared.</param>
    /// <param name="preferredMinimumHeight">Declared preferred minimum height, or zero when none is declared.</param>
    /// <returns>A complete immutable sizing constraint.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A value is out of range, which is an adapter defect.</exception>
    public static WindowSizeConstraint Create(
        double rasterizationScale,
        int preferredMinimumWidth,
        int preferredMinimumHeight)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rasterizationScale);
        ArgumentOutOfRangeException.ThrowIfNegative(preferredMinimumWidth);
        ArgumentOutOfRangeException.ThrowIfNegative(preferredMinimumHeight);
        return new WindowSizeConstraint(
            rasterizationScale,
            preferredMinimumWidth,
            preferredMinimumHeight);
    }
}
