using System;

namespace NeNeCommander.Application.Windowing;

/// <summary>
/// Represents one immutable rectangle in physical pixels. It describes either a window rectangle
/// or the work area of a display, because the adjustment mode compares only those two shapes and
/// compares them in the same coordinate space. Every instance keeps a positive width and height,
/// because neither a window nor a work area can be empty.
/// </summary>
public sealed record WindowBounds
{
    private WindowBounds(int left, int top, int width, int height)
    {
        Left = left;
        Top = top;
        Width = width;
        Height = height;
    }

    /// <summary>Gets the physical-pixel coordinate of the left edge.</summary>
    public int Left { get; }

    /// <summary>Gets the physical-pixel coordinate of the top edge.</summary>
    public int Top { get; }

    /// <summary>Gets the physical-pixel width.</summary>
    public int Width { get; }

    /// <summary>Gets the physical-pixel height.</summary>
    public int Height { get; }

    /// <summary>Gets the physical-pixel coordinate just past the right edge.</summary>
    public int Right => Left + Width;

    /// <summary>Gets the physical-pixel coordinate just past the bottom edge.</summary>
    public int Bottom => Top + Height;

    /// <summary>Creates a rectangle from values an adapter or the planner has already computed.</summary>
    /// <param name="left">Physical-pixel left edge.</param>
    /// <param name="top">Physical-pixel top edge.</param>
    /// <param name="width">Positive physical-pixel width.</param>
    /// <param name="height">Positive physical-pixel height.</param>
    /// <returns>A complete immutable rectangle.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A dimension is not positive, which is a caller defect.</exception>
    public static WindowBounds Create(int left, int top, int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        return new WindowBounds(left, top, width, height);
    }
}
