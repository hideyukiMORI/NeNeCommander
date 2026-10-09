namespace NeNeCommander.Application.Sessions;

/// <summary>
/// Represents the closed result of validating one intent against the Locations picker scope. The
/// scope state is already final when the result is returned.
/// </summary>
public abstract record LocationsValidation
{
    /// <summary>Gets the result that leaves the session no navigation to perform.</summary>
    public static LocationsValidation NothingToRoute { get; } = new NoRoute();

    private protected LocationsValidation()
    {
    }

    private sealed record NoRoute : LocationsValidation;
}
