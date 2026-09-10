using System;
using NeNeCommander.Application.Sessions;

namespace NeNeCommander.Application.Input;

/// <summary>Carries a palette cancellation qualified by the exact open state that owned its event.</summary>
public sealed record CommandPaletteCancellation : UserIntent
{
    internal CommandPaletteCancellation(CommandPaletteOpen expectedState)
    {
        ArgumentNullException.ThrowIfNull(expectedState);
        ExpectedState = expectedState;
    }

    /// <summary>Gets the open state that owned the cancellation event.</summary>
    public CommandPaletteOpen ExpectedState { get; }
}
