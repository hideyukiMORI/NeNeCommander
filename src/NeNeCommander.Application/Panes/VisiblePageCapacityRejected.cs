namespace NeNeCommander.Application.Panes;

/// <summary>
/// Represents a rejected visible-row capacity.
/// </summary>
public sealed record VisiblePageCapacityRejected : VisiblePageCapacityCreation
{
    private protected override void RestrictDerivation()
    {
    }

    internal VisiblePageCapacityRejected()
    {
    }
}
