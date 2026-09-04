namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Represents the closed state of the name editor: hidden, or active with the text the editor
/// starts from while the session waits for a name. The presentation owns that initial text so the
/// host only assigns it.
/// </summary>
public abstract record NameEntryPresentation
{
    /// <summary>Gets the presentation when no name is being entered.</summary>
    public static NameEntryPresentation Hidden { get; } = new HiddenNameEntry();

    private protected NameEntryPresentation()
    {
    }

    private sealed record HiddenNameEntry : NameEntryPresentation;
}
