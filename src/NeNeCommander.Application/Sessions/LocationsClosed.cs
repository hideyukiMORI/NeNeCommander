namespace NeNeCommander.Application.Sessions;

/// <summary>Represents the Locations picker while it is closed and owns no input.</summary>
public sealed record LocationsClosed : LocationsState
{
    private protected override void RestrictDerivation()
    {
    }

    internal LocationsClosed()
    {
    }
}
