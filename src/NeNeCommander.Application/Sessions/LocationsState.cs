namespace NeNeCommander.Application.Sessions;

/// <summary>
/// Represents the closed state of the Locations picker scope: closed, loading both sections, or
/// open with both sections decided.
/// </summary>
public abstract record LocationsState
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the closed state.</summary>
    public static LocationsState Closed { get; } = new LocationsClosed();

    private protected LocationsState()
    {
    }
}
