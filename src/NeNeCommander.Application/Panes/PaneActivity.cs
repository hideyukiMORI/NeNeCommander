namespace NeNeCommander.Application.Panes;

/// <summary>
/// Represents the closed read activity of one pane: idle, loading a target, or the typed
/// result of the most recent read that did not replace the content.
/// </summary>
public abstract record PaneActivity
{
    private protected PaneActivity()
    {
    }

    /// <summary>Gets the activity of a pane with no read in flight and no unreported result.</summary>
    public static PaneActivity Idle { get; } = new PaneIdle();

    private sealed record PaneIdle : PaneActivity;
}
