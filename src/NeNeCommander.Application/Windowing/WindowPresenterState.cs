namespace NeNeCommander.Application.Windowing;

/// <summary>
/// Represents the closed presenter state of the application window as the host can observe it. A
/// snapped window reports <see cref="Restored"/> because the framework exposes nothing else.
/// </summary>
public abstract record WindowPresenterState
{
    /// <summary>Gets the state of an overlapped window at its normal placement.</summary>
    public static WindowPresenterState Restored { get; } = new RestoredState();

    /// <summary>Gets the state of a maximized overlapped window.</summary>
    public static WindowPresenterState Maximized { get; } = new MaximizedState();

    /// <summary>Gets the state of a minimized overlapped window.</summary>
    public static WindowPresenterState Minimized { get; } = new MinimizedState();

    /// <summary>Gets the state of a window whose presenter is not the overlapped one.</summary>
    public static WindowPresenterState NotOverlapped { get; } = new NotOverlappedState();

    /// <summary>Gets the state of a window whose placement could not be read at all.</summary>
    public static WindowPresenterState Unavailable { get; } = new UnavailableState();

    private WindowPresenterState()
    {
    }

    private sealed record RestoredState : WindowPresenterState;
    private sealed record MaximizedState : WindowPresenterState;
    private sealed record MinimizedState : WindowPresenterState;
    private sealed record NotOverlappedState : WindowPresenterState;
    private sealed record UnavailableState : WindowPresenterState;
}
