namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Identifies the closed frame a pane shows for its activation state. Each frame names the
/// semantic design resources for its border; no visual constant is chosen in code.
/// </summary>
public sealed record PaneFrame
{
    /// <summary>Gets the frame of the pane that receives intents.</summary>
    public static PaneFrame Active { get; } = new("FocusRingBrush", "BorderActivePaneThickness");

    /// <summary>Gets the frame of the pane that only keeps its display.</summary>
    public static PaneFrame Passive { get; } = new("BorderSubtleBrush", "BorderPassivePaneThickness");

    private PaneFrame(string brushResourceKey, string thicknessResourceKey)
    {
        BrushResourceKey = brushResourceKey;
        ThicknessResourceKey = thicknessResourceKey;
    }

    /// <summary>Gets the semantic brush resource key for the border.</summary>
    public string BrushResourceKey { get; }

    /// <summary>Gets the semantic thickness resource key for the border.</summary>
    public string ThicknessResourceKey { get; }
}
