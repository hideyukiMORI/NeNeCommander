using System;
using NeNeCommander.Application.Input;

namespace NeNeCommander.Application.Sessions;

/// <summary>Carries the one catalog intent the session must now dispatch over an already closed palette.</summary>
public sealed record CommandPaletteIntentAccepted : CommandPaletteValidation
{
    internal CommandPaletteIntentAccepted(UserIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);
        Intent = intent;
    }

    /// <summary>Gets the validated catalog intent awaiting the session's single dispatch path.</summary>
    public UserIntent Intent { get; }
}
