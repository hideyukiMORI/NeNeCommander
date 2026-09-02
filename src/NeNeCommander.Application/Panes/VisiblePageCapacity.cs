namespace NeNeCommander.Application.Panes;

/// <summary>
/// Represents a positive number of file rows visible in one pane.
/// </summary>
public sealed record VisiblePageCapacity
{
    private VisiblePageCapacity(int value)
    {
        Value = value;
    }

    /// <summary>Gets the positive visible-row count.</summary>
    public int Value { get; }

    /// <summary>
    /// Validates a measured visible-row count.
    /// </summary>
    /// <param name="value">Measured number of visible rows.</param>
    /// <returns>An accepted capacity or a typed rejection.</returns>
    public static VisiblePageCapacityCreation Create(int value)
    {
        return value > 0
            ? new VisiblePageCapacityAccepted(new VisiblePageCapacity(value))
            : new VisiblePageCapacityRejected();
    }
}
