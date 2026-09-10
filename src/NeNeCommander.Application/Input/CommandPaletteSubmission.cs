using System;
using NeNeCommander.Application.Sessions;

namespace NeNeCommander.Application.Input;

/// <summary>Carries one catalog intent qualified by the exact palette state that selected it.</summary>
public sealed record CommandPaletteSubmission : UserIntent
{
    internal CommandPaletteSubmission(CommandPaletteOpen expectedState, UserIntent selectedIntent)
    {
        ArgumentNullException.ThrowIfNull(expectedState);
        ArgumentNullException.ThrowIfNull(selectedIntent);
        ExpectedState = expectedState;
        SelectedIntent = selectedIntent;
    }

    /// <summary>Gets the open state that owned the selection.</summary>
    public CommandPaletteOpen ExpectedState { get; }

    /// <summary>Gets the canonical intent selected from the catalog.</summary>
    public UserIntent SelectedIntent { get; }
}
