namespace NeNeCommander.Application.Panes;

/// <summary>
/// Represents a rejected pane state and its reason.
/// </summary>
public sealed record PaneStateRejected : PaneStateCreation
{
    internal PaneStateRejected(PaneStateFailureKind kind)
    {
        Kind = kind;
    }

    /// <summary>Gets the construction failure.</summary>
    public PaneStateFailureKind Kind { get; }
}
