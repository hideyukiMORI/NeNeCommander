using System;
using NeNeCommander.Application.Input;

namespace NeNeCommander.Application.Commands;

/// <summary>Pairs one canonical catalog intent with its captured state-level availability.</summary>
public sealed record CommandCandidate
{
    internal CommandCandidate(UserIntent intent, CommandAvailability availability)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(availability);
        Intent = intent;
        Availability = availability;
    }

    /// <summary>Gets the existing parameterless intent that identifies and executes the command.</summary>
    public UserIntent Intent { get; }

    /// <summary>Gets availability calculated from the frozen open scope.</summary>
    public CommandAvailability Availability { get; }
}
