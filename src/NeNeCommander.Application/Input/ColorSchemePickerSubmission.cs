using System;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Input;

/// <summary>Accepts a candidate only for the exact theme picker that displayed it.</summary>
public sealed record ColorSchemePickerSubmission : UserIntent
{
    internal ColorSchemePickerSubmission(ColorSchemePickerOpen expectedState, ColorScheme scheme)
    {
        ArgumentNullException.ThrowIfNull(expectedState);
        ArgumentNullException.ThrowIfNull(scheme);
        ExpectedState = expectedState;
        Scheme = scheme;
    }

    /// <summary>Gets the exact picker that owns this submission.</summary>
    public ColorSchemePickerOpen ExpectedState { get; }

    /// <summary>Gets the existing closed scheme explicitly accepted by the user.</summary>
    public ColorScheme Scheme { get; }

    private protected override void RestrictDerivation()
    {
    }
}
