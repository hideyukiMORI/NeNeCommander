using NeNeCommander.Application.Windowing;

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

    /// <summary>
    /// Gets the Application window action this key action requests, or <see langword="null"/> for
    /// <see cref="Leave"/>, which is not a window action but the qualified leave of the scope.
    /// </summary>
    public abstract WindowAdjustmentAction? WindowAction { get; }

    private sealed record MoveLeftAction : WindowAdjustmentKeyAction
    {
        public override WindowAdjustmentAction WindowAction => WindowAdjustmentAction.MoveLeft;
    }

    private sealed record MoveDownAction : WindowAdjustmentKeyAction
    {
        public override WindowAdjustmentAction WindowAction => WindowAdjustmentAction.MoveDown;
    }

    private sealed record MoveUpAction : WindowAdjustmentKeyAction
    {
        public override WindowAdjustmentAction WindowAction => WindowAdjustmentAction.MoveUp;
    }

    private sealed record MoveRightAction : WindowAdjustmentKeyAction
    {
        public override WindowAdjustmentAction WindowAction => WindowAdjustmentAction.MoveRight;
    }

    private sealed record EnlargeAction : WindowAdjustmentKeyAction
    {
        public override WindowAdjustmentAction WindowAction => WindowAdjustmentAction.Enlarge;
    }

    private sealed record ShrinkAction : WindowAdjustmentKeyAction
    {
        public override WindowAdjustmentAction WindowAction => WindowAdjustmentAction.Shrink;
    }

    private sealed record MaximizeAction : WindowAdjustmentKeyAction
    {
        public override WindowAdjustmentAction WindowAction => WindowAdjustmentAction.Maximize;
    }

    private sealed record RestoreAction : WindowAdjustmentKeyAction
    {
        public override WindowAdjustmentAction WindowAction => WindowAdjustmentAction.Restore;
    }

    private sealed record LeaveAction : WindowAdjustmentKeyAction
    {
        public override WindowAdjustmentAction? WindowAction => null;
    }
}
