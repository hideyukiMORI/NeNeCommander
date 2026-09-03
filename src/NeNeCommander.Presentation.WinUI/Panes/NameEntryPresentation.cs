namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Identifies whether the host shows and focuses the name editor: hidden, or active while the
/// session waits for a directory name.
/// </summary>
public abstract record NameEntryPresentation
{
    /// <summary>Gets the presentation when no name is being entered.</summary>
    public static NameEntryPresentation Hidden { get; } = new HiddenNameEntry();

    /// <summary>Gets the presentation while the session waits for a name.</summary>
    public static NameEntryPresentation Active { get; } = new ActiveNameEntry();

    private NameEntryPresentation()
    {
    }

    private sealed record HiddenNameEntry : NameEntryPresentation;
    private sealed record ActiveNameEntry : NameEntryPresentation;
}
