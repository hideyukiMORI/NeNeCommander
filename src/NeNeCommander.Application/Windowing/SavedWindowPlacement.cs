using System;

namespace NeNeCommander.Application.Windowing;

/// <summary>Remembers a normal physical-pixel rectangle and the non-minimized startup state.</summary>
public sealed record SavedWindowPlacement
{
    private SavedWindowPlacement(WindowBounds bounds, WindowPresenterState state)
    {
        Bounds = bounds;
        State = state;
    }

    /// <summary>Gets the normal rectangle, even when the window closes maximized.</summary>
    public WindowBounds Bounds { get; }

    /// <summary>Gets only Restored or Maximized; minimized never becomes a startup state.</summary>
    public WindowPresenterState State { get; }

    /// <summary>Creates a complete saved placement from a validated normal rectangle and state.</summary>
    public static SavedWindowPlacement Create(WindowBounds bounds, WindowPresenterState state)
    {
        ArgumentNullException.ThrowIfNull(bounds);
        ArgumentNullException.ThrowIfNull(state);
        return state != WindowPresenterState.Restored && state != WindowPresenterState.Maximized
            ? throw new ArgumentException("Only restored or maximized placement can be saved.", nameof(state))
            : new SavedWindowPlacement(bounds, state);
    }
}
