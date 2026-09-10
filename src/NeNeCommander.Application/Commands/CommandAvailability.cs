namespace NeNeCommander.Application.Commands;

/// <summary>Represents whether captured application state can start one catalog command.</summary>
public abstract record CommandAvailability
{
    /// <summary>Gets the state-level available result.</summary>
    public static CommandAvailability Available { get; } = new AvailableCommand();

    private protected CommandAvailability()
    {
    }

    private sealed record AvailableCommand : CommandAvailability;
}
