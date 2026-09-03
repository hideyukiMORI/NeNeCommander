namespace NeNeCommander.Application.Panes;

/// <summary>
/// Identifies one of the two pane surfaces. Exactly one side is active at any time.
/// </summary>
public abstract record PaneSide
{
    /// <summary>Gets the left pane side.</summary>
    public static PaneSide Left { get; } = new LeftSide();

    /// <summary>Gets the right pane side.</summary>
    public static PaneSide Right { get; } = new RightSide();

    private PaneSide()
    {
    }

    /// <summary>Gets the opposite side.</summary>
    public abstract PaneSide Other { get; }

    private sealed record LeftSide : PaneSide
    {
        public override PaneSide Other => Right;
    }

    private sealed record RightSide : PaneSide
    {
        public override PaneSide Other => Left;
    }
}
