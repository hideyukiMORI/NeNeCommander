namespace NeNeCommander.Application.Panes;

/// <summary>
/// Represents the closed external activity of one pane: idle, reading or launching a target, or
/// the typed result of the most recent read or launch that did not replace the content.
/// </summary>
public abstract record PaneActivity
{
    private protected PaneActivity()
    {
    }

    /// <summary>Gets the activity of a pane with no work in flight and no unreported result.</summary>
    public static PaneActivity Idle { get; } = new PaneIdle();

    private sealed record PaneIdle : PaneActivity;
}
