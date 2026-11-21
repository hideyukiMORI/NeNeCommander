using System;
using NeNeCommander.Application.Sessions;

namespace NeNeCommander.Application.Input;

/// <summary>Cancels the exact address modal whose dismissal was requested.</summary>
public sealed record AddressCancellation : UserIntent
{
    private protected override void RestrictDerivation()
    {
    }

    internal AddressCancellation(AddressEditorState expectedState)
    {
        ArgumentNullException.ThrowIfNull(expectedState);
        ExpectedState = expectedState;
    }

    /// <summary>Gets the exact editor state that owns this cancellation.</summary>
    public AddressEditorState ExpectedState { get; }
}
