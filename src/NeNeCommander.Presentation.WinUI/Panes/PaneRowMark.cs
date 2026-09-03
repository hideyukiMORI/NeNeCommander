namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Identifies whether a row belongs to the pane's explicit selection.
/// </summary>
public abstract record PaneRowMark
{
    /// <summary>Gets the mark of a row inside the explicit selection.</summary>
    public static PaneRowMark Selected { get; } = new SelectedMark();

    /// <summary>Gets the mark of a row outside the explicit selection.</summary>
    public static PaneRowMark Unselected { get; } = new UnselectedMark();

    private PaneRowMark()
    {
    }

    private sealed record SelectedMark : PaneRowMark;
    private sealed record UnselectedMark : PaneRowMark;
}
