using System;

namespace NeNeCommander.Application.Commands;

/// <summary>Represents one catalog command rejected by captured application state.</summary>
public sealed record CommandUnavailable : CommandAvailability
{
    private protected override void RestrictDerivation()
    {
    }

    internal CommandUnavailable(CommandUnavailableReason reason)
    {
        ArgumentNullException.ThrowIfNull(reason);
        Reason = reason;
    }

    /// <summary>Gets the closed reason for unavailability.</summary>
    public CommandUnavailableReason Reason { get; }
}
