using System;
using NeNeCommander.Application.Sessions;

namespace NeNeCommander.Application.Input;

/// <summary>Carries a Locations picker cancellation qualified by the exact open state that owned its event.</summary>
public sealed record LocationsCancellation : UserIntent
{
    internal LocationsCancellation(LocationsOpen expectedState)
    {
        ArgumentNullException.ThrowIfNull(expectedState);
        ExpectedState = expectedState;
    }

    /// <summary>Gets the open state that owned the cancellation event.</summary>
    public LocationsOpen ExpectedState { get; }
}
