using System;
using NeNeCommander.Application.Locations;
using NeNeCommander.Application.Sessions;

namespace NeNeCommander.Application.Input;

/// <summary>Carries one Locations entry selection qualified by the exact open state that showed it.</summary>
public sealed record LocationSelection : UserIntent
{
    internal LocationSelection(LocationsOpen expectedState, LocationItem item)
    {
        ArgumentNullException.ThrowIfNull(expectedState);
        ArgumentNullException.ThrowIfNull(item);
        ExpectedState = expectedState;
        Item = item;
    }

    /// <summary>Gets the open state that owned the selection event.</summary>
    public LocationsOpen ExpectedState { get; }

    /// <summary>Gets the selected entry.</summary>
    public LocationItem Item { get; }
}
