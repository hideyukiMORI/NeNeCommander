using System;
using NeNeCommander.Application.Input;

namespace NeNeCommander.Application.Commands;

/// <summary>Pairs one canonical catalog intent with its captured state-level availability.</summary>
public sealed record CommandCandidate
{
    internal CommandCandidate(UserIntent intent, CommandAvailability availability, CommandMenuGroup group)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(availability);
        ArgumentNullException.ThrowIfNull(group);
        Group = group;
        Intent = intent;
        Availability = availability;
    }

    /// <summary>Gets the existing parameterless intent that identifies and executes the command.</summary>
    public UserIntent Intent { get; }

    /// <summary>Gets the canonical menu target group declared with this intent.</summary>
    public CommandMenuGroup Group { get; }

    /// <summary>Gets availability calculated from the frozen open scope.</summary>
    public CommandAvailability Availability { get; }
}
