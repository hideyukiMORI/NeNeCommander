namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Identifies the closed tone the operation bar shows for the current operation activity. Each
/// tone names the semantic design resources for the bar's surface, its text, and its border, and
/// the closed icon that precedes the status, so the view carries no state-to-colour decision
/// (ARC-012, CMD-003).
/// </summary>
public abstract record OperationBarTone
{
    /// <summary>Gets the tone of a bar that reports no attention-seeking state.</summary>
    public static OperationBarTone Idle { get; } = new IdleTone();

    /// <summary>Gets the tone of a bar that is waiting for a typed name.</summary>
    public static OperationBarTone AwaitingName { get; } = new AwaitingNameTone();

    /// <summary>Gets the tone of a bar that is waiting for a destructive confirmation.</summary>
    public static OperationBarTone AwaitingConfirmation { get; } = new AwaitingConfirmationTone();

    /// <summary>Gets the tone of a bar that reports a rejected request or an incomplete operation.</summary>
    public static OperationBarTone Failure { get; } = new FailureTone();

    private OperationBarTone()
    {
    }

    /// <summary>Gets the semantic brush resource key of the bar's background.</summary>
    public abstract string SurfaceBrushResourceKey { get; }

    /// <summary>Gets the semantic brush resource key of the bar's status text and icon.</summary>
    public abstract string ForegroundBrushResourceKey { get; }

    /// <summary>Gets the semantic brush resource key of the bar's border.</summary>
    public abstract string BorderBrushResourceKey { get; }

    /// <summary>Gets the closed icon shown before the status text.</summary>
    public abstract OperationBarIcon Icon { get; }

    private sealed record IdleTone : OperationBarTone
    {
        public override string SurfaceBrushResourceKey => "SurfacePaneBrush";

        public override string ForegroundBrushResourceKey => "TextPrimaryBrush";

        public override string BorderBrushResourceKey => "BorderSubtleBrush";

        public override OperationBarIcon Icon => OperationBarIcon.None;
    }

    private sealed record AwaitingNameTone : OperationBarTone
    {
        public override string SurfaceBrushResourceKey => "FocusSurfaceBrush";

        public override string ForegroundBrushResourceKey => "TextPrimaryBrush";

        public override string BorderBrushResourceKey => "FocusRingBrush";

        public override OperationBarIcon Icon => OperationBarIcon.NameEntry;
    }

    private sealed record AwaitingConfirmationTone : OperationBarTone
    {
        public override string SurfaceBrushResourceKey => "StatusWarningSurfaceBrush";

        public override string ForegroundBrushResourceKey => "StatusWarningBrush";

        public override string BorderBrushResourceKey => "StatusWarningBrush";

        public override OperationBarIcon Icon => OperationBarIcon.Warning;
    }

    private sealed record FailureTone : OperationBarTone
    {
        public override string SurfaceBrushResourceKey => "StatusDangerSurfaceBrush";

        public override string ForegroundBrushResourceKey => "StatusDangerBrush";

        public override string BorderBrushResourceKey => "StatusDangerBrush";

        public override OperationBarIcon Icon => OperationBarIcon.Warning;
    }
}
