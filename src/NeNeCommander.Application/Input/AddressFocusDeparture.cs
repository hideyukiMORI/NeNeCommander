using System;
using NeNeCommander.Application.Sessions;

namespace NeNeCommander.Application.Input;

/// <summary>Closes the exact address editor whose native control lost focus.</summary>
public sealed record AddressFocusDeparture : UserIntent
{
    internal AddressFocusDeparture(AddressEditorState expectedState)
    {
        ArgumentNullException.ThrowIfNull(expectedState);
        ExpectedState = expectedState;
    }

    /// <summary>Gets the exact editor state formerly owned by the departing control.</summary>
    public AddressEditorState ExpectedState { get; }
}
