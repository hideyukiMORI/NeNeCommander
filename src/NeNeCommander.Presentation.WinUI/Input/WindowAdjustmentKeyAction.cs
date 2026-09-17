namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>
/// Names the closed Presentation-owned action of one key the window-adjustment mode owns. Every
/// action but <see cref="Leave"/> is carried to Application as a window adjustment; leaving is a
/// qualified cancellation of the scope.
/// </summary>
public abstract record WindowAdjustmentKeyAction
{
    /// <summary>Gets the action that moves the window left by one step.</summary>
    public static WindowAdjustmentKeyAction MoveLeft { get; } = new MoveLeftAction();

    /// <summary>Gets the action that moves the window down by one step.</summary>
    public static WindowAdjustmentKeyAction MoveDown { get; } = new MoveDownAction();

    /// <summary>Gets the action that moves the window up by one step.</summary>
    public static WindowAdjustmentKeyAction MoveUp { get; } = new MoveUpAction();

    /// <summary>Gets the action that moves the window right by one step.</summary>
    public static WindowAdjustmentKeyAction MoveRight { get; } = new MoveRightAction();

    /// <summary>Gets the action that enlarges the window by one step.</summary>
    public static WindowAdjustmentKeyAction Enlarge { get; } = new EnlargeAction();

    /// <summary>Gets the action that shrinks the window by one step.</summary>
    public static WindowAdjustmentKeyAction Shrink { get; } = new ShrinkAction();

    /// <summary>Gets the action that maximizes the window.</summary>
    public static WindowAdjustmentKeyAction Maximize { get; } = new MaximizeAction();

    /// <summary>Gets the action that restores the window to its normal placement.</summary>
    public static WindowAdjustmentKeyAction Restore { get; } = new RestoreAction();

    /// <summary>Gets the action that leaves the mode and returns focus to the captured pane.</summary>
    public static WindowAdjustmentKeyAction Leave { get; } = new LeaveAction();

    private WindowAdjustmentKeyAction()
    {
    }

    private sealed record MoveLeftAction : WindowAdjustmentKeyAction;
    private sealed record MoveDownAction : WindowAdjustmentKeyAction;
    private sealed record MoveUpAction : WindowAdjustmentKeyAction;
    private sealed record MoveRightAction : WindowAdjustmentKeyAction;
    private sealed record EnlargeAction : WindowAdjustmentKeyAction;
    private sealed record ShrinkAction : WindowAdjustmentKeyAction;
    private sealed record MaximizeAction : WindowAdjustmentKeyAction;
    private sealed record RestoreAction : WindowAdjustmentKeyAction;
    private sealed record LeaveAction : WindowAdjustmentKeyAction;
}
