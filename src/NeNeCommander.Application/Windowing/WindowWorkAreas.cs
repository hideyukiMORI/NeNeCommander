using System;
using System.Collections.Generic;

namespace NeNeCommander.Application.Windowing;

/// <summary>
/// Carries the physical work area of the window's own display together with the work area of every
/// attached display. Both facts are read at the same instant, so they travel as one value; that
/// also keeps <see cref="WindowPlacement"/> within the CS-013 parameter ceiling.
/// </summary>
public sealed record WindowWorkAreas
{
    private WindowWorkAreas(WindowBounds current, IReadOnlyList<WindowBounds> attached)
    {
        Current = current;
        Attached = attached;
    }

    /// <summary>Gets the work area of the display the window is currently on.</summary>
    public WindowBounds Current { get; }

    /// <summary>Gets the work area of every attached display, including <see cref="Current"/>.</summary>
    public IReadOnlyList<WindowBounds> Attached { get; }

    /// <summary>Creates the work-area set an adapter read from the desktop.</summary>
    /// <param name="current">Work area of the window's display.</param>
    /// <param name="attached">Work area of every attached display; it must contain <paramref name="current"/>.</param>
    /// <returns>A complete immutable work-area set.</returns>
    /// <exception cref="ArgumentException">The attached set does not contain the current work area, which is an adapter defect.</exception>
    public static WindowWorkAreas Create(WindowBounds current, IReadOnlyList<WindowBounds> attached)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(attached);
        List<WindowBounds> owned = [.. attached];
        return owned.Contains(current)
            ? new WindowWorkAreas(current, owned.AsReadOnly())
            : throw new ArgumentException(
                "The attached work areas must contain the work area of the window's display.",
                nameof(attached));
    }
}
