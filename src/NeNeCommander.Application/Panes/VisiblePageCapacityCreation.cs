namespace NeNeCommander.Application.Panes;

/// <summary>
/// Represents the closed result of validating a measured visible-row capacity.
/// </summary>
public abstract record VisiblePageCapacityCreation
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    private protected VisiblePageCapacityCreation()
    {
    }
}
