namespace NeNeCommander.Application.Windowing;

/// <summary>
/// Represents the closed set of window adjustments the mode can request. Leaving the mode is not an
/// action here, because it never reaches the planner and can never be refused.
/// </summary>
public abstract record WindowAdjustmentAction
{
    /// <summary>Gets the action that moves the window left by one step.</summary>
    public static WindowAdjustmentAction MoveLeft { get; } = new MoveLeftAction();

    /// <summary>Gets the action that moves the window down by one step.</summary>
    public static WindowAdjustmentAction MoveDown { get; } = new MoveDownAction();

    /// <summary>Gets the action that moves the window up by one step.</summary>
    public static WindowAdjustmentAction MoveUp { get; } = new MoveUpAction();

    /// <summary>Gets the action that moves the window right by one step.</summary>
    public static WindowAdjustmentAction MoveRight { get; } = new MoveRightAction();

    /// <summary>Gets the action that enlarges the window by one step, anchored at its top-left corner.</summary>
    public static WindowAdjustmentAction Enlarge { get; } = new EnlargeAction();

    /// <summary>Gets the action that shrinks the window by one step, anchored at its top-left corner.</summary>
    public static WindowAdjustmentAction Shrink { get; } = new ShrinkAction();

    /// <summary>Gets the action that maximizes the window.</summary>
    public static WindowAdjustmentAction Maximize { get; } = new MaximizeAction();

    /// <summary>Gets the action that restores the window to its normal placement.</summary>
    public static WindowAdjustmentAction Restore { get; } = new RestoreAction();

    private WindowAdjustmentAction()
    {
    }

    private sealed record MoveLeftAction : WindowAdjustmentAction;
    private sealed record MoveDownAction : WindowAdjustmentAction;
    private sealed record MoveUpAction : WindowAdjustmentAction;
    private sealed record MoveRightAction : WindowAdjustmentAction;
    private sealed record EnlargeAction : WindowAdjustmentAction;
    private sealed record ShrinkAction : WindowAdjustmentAction;
    private sealed record MaximizeAction : WindowAdjustmentAction;
    private sealed record RestoreAction : WindowAdjustmentAction;
}
