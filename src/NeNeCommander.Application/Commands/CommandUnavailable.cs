namespace NeNeCommander.Application.Commands;

/// <summary>Represents one catalog command rejected by captured application state.</summary>
public sealed record CommandUnavailable : CommandAvailability
{
    internal CommandUnavailable(CommandUnavailableReason reason)
    {
        Reason = reason;
    }

    /// <summary>Gets the closed reason for unavailability.</summary>
    public CommandUnavailableReason Reason { get; }
}
