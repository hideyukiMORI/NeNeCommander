namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Identifies the closed mark one row shows for focus and explicit selection. Exactly one mark
/// applies to a row: the focus item of the active pane outranks selection, selection outranks the
/// focus item of the passive pane, and every other row is unmarked. Each mark names the semantic
/// design resources for its left marker and its background, so no visual constant and no
/// precedence decision reaches the view (ARC-012, CS-023).
/// </summary>
public abstract record PaneRowMark
{
    /// <summary>Gets the mark of the focus item while its pane receives intents.</summary>
    public static PaneRowMark FocusInActivePane { get; } = new ActiveFocusMark();

    /// <summary>Gets the mark of a row inside the explicit selection.</summary>
    public static PaneRowMark Selected { get; } = new SelectedMark();

    /// <summary>Gets the mark of the focus item while its pane only keeps its display.</summary>
    public static PaneRowMark FocusInPassivePane { get; } = new PassiveFocusMark();

    /// <summary>Gets the mark of a row that is neither focused nor selected.</summary>
    public static PaneRowMark Unmarked { get; } = new PlainMark();

    private PaneRowMark()
    {
    }

    /// <summary>Gets the semantic brush resource key of the row's left marker.</summary>
    public abstract string MarkerBrushResourceKey { get; }

    /// <summary>Gets the semantic brush resource key of the row's background.</summary>
    public abstract string SurfaceBrushResourceKey { get; }

    private sealed record ActiveFocusMark : PaneRowMark
    {
        public override string MarkerBrushResourceKey => "FocusRingBrush";

        public override string SurfaceBrushResourceKey => "FocusSurfaceBrush";
    }

    private sealed record SelectedMark : PaneRowMark
    {
        public override string MarkerBrushResourceKey => "SelectionMarkBrush";

        public override string SurfaceBrushResourceKey => "SelectionSurfaceBrush";
    }

    private sealed record PassiveFocusMark : PaneRowMark
    {
        public override string MarkerBrushResourceKey => "BorderSubtleBrush";

        public override string SurfaceBrushResourceKey => "SurfacePaneBrush";
    }

    private sealed record PlainMark : PaneRowMark
    {
        public override string MarkerBrushResourceKey => "SurfacePaneBrush";

        public override string SurfaceBrushResourceKey => "SurfacePaneBrush";
    }
}
