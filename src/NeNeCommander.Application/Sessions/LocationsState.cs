namespace NeNeCommander.Application.Sessions;

/// <summary>
/// Represents the closed state of the Locations picker scope: closed, loading both sections, or
/// open with both sections decided.
/// </summary>
public abstract record LocationsState
{
    /// <summary>Gets the closed state.</summary>
    public static LocationsState Closed { get; } = new LocationsClosed();

    private protected LocationsState()
    {
    }
}
