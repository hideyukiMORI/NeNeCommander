using NeNeCommander.Application.Windowing;

namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>
/// Names the closed Presentation-owned action of one key the window-adjustment mode owns. Geometry
/// actions reach Application; leaving is a qualified cancellation and help paging changes only the view.
/// </summary>
public abstract record WindowAdjustmentKeyAction
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

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

    /// <summary>Gets the previous display action.</summary>
    public static WindowAdjustmentKeyAction PreviousDisplay { get; } = new PreviousDisplayCase();

    /// <summary>Gets the next display action.</summary>
    public static WindowAdjustmentKeyAction NextDisplay { get; } = new NextDisplayCase();

    /// <summary>Gets the view-only action that reads the previous page of mode help.</summary>
    public static WindowAdjustmentKeyAction PreviousHelpPage { get; } = new PreviousHelpPageCase();

    /// <summary>Gets the view-only action that reads the next page of mode help.</summary>
    public static WindowAdjustmentKeyAction NextHelpPage { get; } = new NextHelpPageCase();
    private WindowAdjustmentKeyAction()
    {
    }

    /// <summary>
    /// Gets the Application window action this key action requests, or <see langword="null"/> for
    /// leaving or paging the view, neither of which requests window geometry.
    /// </summary>
    public abstract WindowAdjustmentAction? WindowAction { get; }

    private sealed record MoveLeftAction : WindowAdjustmentKeyAction
    {
        private protected override void RestrictDerivation()
        {
        }

        public override WindowAdjustmentAction WindowAction => WindowAdjustmentAction.MoveLeft;
    }

    private sealed record MoveDownAction : WindowAdjustmentKeyAction
    {
        private protected override void RestrictDerivation()
        {
        }

        public override WindowAdjustmentAction WindowAction => WindowAdjustmentAction.MoveDown;
    }

    private sealed record MoveUpAction : WindowAdjustmentKeyAction
    {
        private protected override void RestrictDerivation()
        {
        }

        public override WindowAdjustmentAction WindowAction => WindowAdjustmentAction.MoveUp;
    }

    private sealed record MoveRightAction : WindowAdjustmentKeyAction
    {
        private protected override void RestrictDerivation()
        {
        }

        public override WindowAdjustmentAction WindowAction => WindowAdjustmentAction.MoveRight;
    }

    private sealed record EnlargeAction : WindowAdjustmentKeyAction
    {
        private protected override void RestrictDerivation()
        {
        }

        public override WindowAdjustmentAction WindowAction => WindowAdjustmentAction.Enlarge;
    }

    private sealed record ShrinkAction : WindowAdjustmentKeyAction
    {
        private protected override void RestrictDerivation()
        {
        }

        public override WindowAdjustmentAction WindowAction => WindowAdjustmentAction.Shrink;
    }

    private sealed record MaximizeAction : WindowAdjustmentKeyAction
    {
        private protected override void RestrictDerivation()
        {
        }

        public override WindowAdjustmentAction WindowAction => WindowAdjustmentAction.Maximize;
    }

    private sealed record RestoreAction : WindowAdjustmentKeyAction
    {
        private protected override void RestrictDerivation()
        {
        }

        public override WindowAdjustmentAction WindowAction => WindowAdjustmentAction.Restore;
    }

    private sealed record LeaveAction : WindowAdjustmentKeyAction
    {
        private protected override void RestrictDerivation()
        {
        }

        public override WindowAdjustmentAction? WindowAction => null;
    }
    private sealed record PreviousDisplayCase : WindowAdjustmentKeyAction
    {
        private protected override void RestrictDerivation()
        {
        }

        public override WindowAdjustmentAction WindowAction => WindowAdjustmentAction.PreviousDisplay;
    }

    private sealed record NextDisplayCase : WindowAdjustmentKeyAction
    {
        private protected override void RestrictDerivation()
        {
        }

        public override WindowAdjustmentAction WindowAction => WindowAdjustmentAction.NextDisplay;
    }

    private sealed record PreviousHelpPageCase : WindowAdjustmentKeyAction
    {
        private protected override void RestrictDerivation()
        {
        }

        public override WindowAdjustmentAction? WindowAction => null;
    }

    private sealed record NextHelpPageCase : WindowAdjustmentKeyAction
    {
        private protected override void RestrictDerivation()
        {
        }

        public override WindowAdjustmentAction? WindowAction => null;
    }
}
