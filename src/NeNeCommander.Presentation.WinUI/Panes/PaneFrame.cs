namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Identifies the closed frame a pane shows for its activation state. Each frame names the
/// semantic design resources for its border and for the pane-number badge in its header; no visual
/// constant is chosen in code and no view decides what activation looks like.
/// </summary>
public abstract record PaneFrame
{
    /// <summary>Gets the frame of the pane that receives intents.</summary>
    public static PaneFrame Active { get; } = new ActivePaneFrame();

    /// <summary>Gets the frame of the pane that only keeps its display.</summary>
    public static PaneFrame Passive { get; } = new PassivePaneFrame();

    private PaneFrame()
    {
    }

    /// <summary>Gets the semantic brush resource key for the pane border and its header rule.</summary>
    public abstract string BrushResourceKey { get; }

    /// <summary>Gets the semantic thickness resource key for the border.</summary>
    public abstract string ThicknessResourceKey { get; }

    /// <summary>Gets the semantic brush resource key filling the pane-number badge.</summary>
    public abstract string NumberSurfaceBrushResourceKey { get; }

    /// <summary>Gets the semantic brush resource key of the pane-number badge text.</summary>
    public abstract string NumberForegroundBrushResourceKey { get; }

    private sealed record ActivePaneFrame : PaneFrame
    {
        public override string BrushResourceKey => "FocusRingBrush";

        public override string ThicknessResourceKey => "BorderActivePaneThickness";

        public override string NumberSurfaceBrushResourceKey => "FocusRingBrush";

        public override string NumberForegroundBrushResourceKey => "SurfacePaneBrush";
    }

    private sealed record PassivePaneFrame : PaneFrame
    {
        public override string BrushResourceKey => "BorderSubtleBrush";

        public override string ThicknessResourceKey => "BorderPassivePaneThickness";

        public override string NumberSurfaceBrushResourceKey => "BorderSubtleBrush";

        public override string NumberForegroundBrushResourceKey => "TextSecondaryBrush";
    }
}
