namespace NeNeCommander.Application.Panes;

/// <summary>
/// Represents an accepted initial pane state.
/// </summary>
public sealed record PaneStateAccepted : PaneStateCreation
{
    internal PaneStateAccepted(PaneState state)
    {
        State = state;
    }

    /// <summary>Gets the valid pane state.</summary>
    public PaneState State { get; }
}
