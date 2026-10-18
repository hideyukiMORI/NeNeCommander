using System;
using NeNeCommander.Application.Input;

namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>Represents a key event consumed as one typed application intent.</summary>
public sealed record MappedKeyboardIntent : KeyboardMappingOutcome
{
    private protected override void RestrictDerivation()
    {
    }

    internal MappedKeyboardIntent(UserIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);
        Intent = intent;
    }

    /// <summary>Gets the sole application intent emitted for the key event.</summary>
    public UserIntent Intent { get; }
}
