namespace NeNeCommander.Application.Panes;

/// <summary>
/// Represents an accepted visible-row capacity.
/// </summary>
public sealed record VisiblePageCapacityAccepted : VisiblePageCapacityCreation
{
    private protected override void RestrictDerivation()
    {
    }

    internal VisiblePageCapacityAccepted(VisiblePageCapacity capacity)
    {
        Capacity = capacity;
    }

    /// <summary>Gets the validated capacity.</summary>
    public VisiblePageCapacity Capacity { get; }
}
