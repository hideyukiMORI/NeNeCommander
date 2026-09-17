namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>
/// Names one displayed hint of the window-adjustment helper. Several keys of the mode's table may
/// declare the same group, in which case the helper shows one hint whose caps are those keys in
/// declaration order (KBD-005).
/// </summary>
public abstract record WindowAdjustmentHintGroup
{
    /// <summary>Gets the hint that names moving the window.</summary>
    public static WindowAdjustmentHintGroup Move { get; } = new MoveGroup();

    /// <summary>Gets the hint that names enlarging the window.</summary>
    public static WindowAdjustmentHintGroup Enlarge { get; } = new EnlargeGroup();

    /// <summary>Gets the hint that names shrinking the window.</summary>
    public static WindowAdjustmentHintGroup Shrink { get; } = new ShrinkGroup();

    /// <summary>Gets the hint that names maximizing the window.</summary>
    public static WindowAdjustmentHintGroup Maximize { get; } = new MaximizeGroup();

    /// <summary>Gets the hint that names restoring the window.</summary>
    public static WindowAdjustmentHintGroup Restore { get; } = new RestoreGroup();

    /// <summary>Gets the hint that names leaving the mode.</summary>
    public static WindowAdjustmentHintGroup Leave { get; } = new LeaveGroup();

    private WindowAdjustmentHintGroup()
    {
    }

    /// <summary>Gets the localization resource key that names what the group's keys do.</summary>
    public abstract string LabelResourceKey { get; }

    private sealed record MoveGroup : WindowAdjustmentHintGroup
    {
        public override string LabelResourceKey => "WindowAdjustmentHintMove";
    }

    private sealed record EnlargeGroup : WindowAdjustmentHintGroup
    {
        public override string LabelResourceKey => "WindowAdjustmentHintEnlarge";
    }

    private sealed record ShrinkGroup : WindowAdjustmentHintGroup
    {
        public override string LabelResourceKey => "WindowAdjustmentHintShrink";
    }

    private sealed record MaximizeGroup : WindowAdjustmentHintGroup
    {
        public override string LabelResourceKey => "WindowAdjustmentHintMaximize";
    }

    private sealed record RestoreGroup : WindowAdjustmentHintGroup
    {
        public override string LabelResourceKey => "WindowAdjustmentHintRestore";
    }

    private sealed record LeaveGroup : WindowAdjustmentHintGroup
    {
        public override string LabelResourceKey => "WindowAdjustmentHintLeave";
    }
}
