namespace NeNeCommander.Application.Windowing;

/// <summary>
/// Represents the closed set of reasons the planner refuses one window adjustment. A refusal is a
/// decided outcome that the helper shows; it is never an exception.
/// </summary>
public abstract record WindowAdjustmentRefusal
{
    /// <summary>Gets the refusal of a move that would leave no reachable run of caption on the desktop.</summary>
    public static WindowAdjustmentRefusal CaptionWouldLeaveDesktop { get; } = new CaptionLeavesDesktop();

    /// <summary>Gets the refusal of an enlarge on a window already as wide and as tall as its work area.</summary>
    public static WindowAdjustmentRefusal AtMaximumSize { get; } = new MaximumSizeReached();

    /// <summary>Gets the refusal of a shrink that would fall below an effective minimum dimension.</summary>
    public static WindowAdjustmentRefusal AtMinimumSize { get; } = new MinimumSizeReached();

    /// <summary>Gets the refusal of an action that a maximized window does not accept.</summary>
    public static WindowAdjustmentRefusal WindowIsMaximized { get; } = new AlreadyMaximized();

    /// <summary>Gets the refusal of an action that a minimized window does not accept.</summary>
    public static WindowAdjustmentRefusal WindowIsMinimized { get; } = new AlreadyMinimized();

    /// <summary>Gets the refusal of a restore on a window that is already at its normal placement.</summary>
    public static WindowAdjustmentRefusal WindowIsRestored { get; } = new AlreadyRestored();

    /// <summary>Gets the refusal of every action while the window has no overlapped presenter.</summary>
    public static WindowAdjustmentRefusal PresenterIsNotOverlapped { get; } = new PresenterNotOverlapped();

    /// <summary>Gets the refusal of every action while the placement could not be read.</summary>
    public static WindowAdjustmentRefusal PlacementUnavailable { get; } = new PlacementNotAvailable();

    private WindowAdjustmentRefusal()
    {
    }

    private sealed record CaptionLeavesDesktop : WindowAdjustmentRefusal;
    private sealed record MaximumSizeReached : WindowAdjustmentRefusal;
    private sealed record MinimumSizeReached : WindowAdjustmentRefusal;
    private sealed record AlreadyMaximized : WindowAdjustmentRefusal;
    private sealed record AlreadyMinimized : WindowAdjustmentRefusal;
    private sealed record AlreadyRestored : WindowAdjustmentRefusal;
    private sealed record PresenterNotOverlapped : WindowAdjustmentRefusal;
    private sealed record PlacementNotAvailable : WindowAdjustmentRefusal;
}
