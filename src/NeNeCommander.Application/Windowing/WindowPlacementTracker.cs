using System;

namespace NeNeCommander.Application.Windowing;

/// <summary>Owns the last observed normal rectangle across maximize and minimize transitions.</summary>
public sealed class WindowPlacementTracker
{
    /// <summary>Gets the last saveable observation, or absence before any normal rectangle is known.</summary>
    public SavedWindowPlacement? Current { get; private set; }

    /// <summary>Observes one stable framework reading without performing I/O or correcting live geometry.</summary>
    public void Observe(WindowBounds bounds, WindowPresenterState state)
    {
        ArgumentNullException.ThrowIfNull(bounds);
        ArgumentNullException.ThrowIfNull(state);
        if (state == WindowPresenterState.Restored)
        {
            Current = SavedWindowPlacement.Create(bounds, state);
        }
        else if (state == WindowPresenterState.Maximized && Current is not null)
        {
            Current = SavedWindowPlacement.Create(Current.Bounds, state);
        }
    }
}
