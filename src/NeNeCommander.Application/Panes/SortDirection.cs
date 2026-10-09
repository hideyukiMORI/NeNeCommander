namespace NeNeCommander.Application.Panes;

/// <summary>
/// Identifies the closed direction of a pane's sort key. Only the key comparison follows the
/// direction; directories precede files in both directions.
/// </summary>
public abstract record SortDirection
{
    /// <summary>Gets the direction that places the smallest key first.</summary>
    public static SortDirection Ascending { get; } = new AscendingDirection();

    /// <summary>Gets the direction that places the largest key first.</summary>
    public static SortDirection Descending { get; } = new DescendingDirection();

    private SortDirection()
    {
    }

    /// <summary>Gets the opposite direction.</summary>
    public abstract SortDirection Reversed { get; }

    private sealed record AscendingDirection : SortDirection
    {
        public override SortDirection Reversed => Descending;
    }

    private sealed record DescendingDirection : SortDirection
    {
        public override SortDirection Reversed => Ascending;
    }
}
