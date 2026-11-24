using System;

namespace NeNeCommander.Application.Windowing;

/// <summary>Carries the two physical rectangles already decided by the pure transfer calculation.</summary>
public sealed record WindowTransferGeometry
{
    private WindowTransferGeometry(WindowBounds initial, WindowBounds final)
    {
        Initial = initial;
        Final = final;
    }

    /// <summary>Gets the compensated first request, at the final origin.</summary>
    public WindowBounds Initial { get; }

    /// <summary>Gets the final fitted rectangle, applied once after qualification.</summary>
    public WindowBounds Final { get; }

    internal static WindowTransferGeometry Create(WindowBounds initial, WindowBounds final)
    {
        ArgumentNullException.ThrowIfNull(initial);
        ArgumentNullException.ThrowIfNull(final);
        return new WindowTransferGeometry(initial, final);
    }
}
