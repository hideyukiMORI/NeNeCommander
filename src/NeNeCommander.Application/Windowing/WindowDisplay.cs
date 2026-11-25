using System;

namespace NeNeCommander.Application.Windowing;

/// <summary>Owns the identity, physical work area and reported scale of one observed display.</summary>
public sealed record WindowDisplay
{
    private WindowDisplay(WindowDisplayIdentity identity, WindowBounds workArea, double scale)
    {
        Identity = identity;
        WorkArea = workArea;
        Scale = scale;
    }

    /// <summary>Gets the current opaque display identity.</summary>
    public WindowDisplayIdentity Identity { get; }

    /// <summary>Gets its physical work area.</summary>
    public WindowBounds WorkArea { get; }

    /// <summary>Gets its positive finite reported physical pixels per layout pixel.</summary>
    public double Scale { get; }

    /// <summary>Creates validated facts read at the window adapter boundary.</summary>
    /// <param name="identity">Opaque identity, never a monitor index.</param>
    /// <param name="workArea">Nonempty physical work area.</param>
    /// <param name="scale">Positive finite reported scale.</param>
    /// <returns>One immutable display observation.</returns>
    public static WindowDisplay Create(WindowDisplayIdentity identity, WindowBounds workArea, double scale)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(workArea);
        return !double.IsFinite(scale) || scale <= 0d
            ? throw new ArgumentOutOfRangeException(nameof(scale), scale, "Display scale must be positive and finite.")
            : new WindowDisplay(identity, workArea, scale);
    }
}
