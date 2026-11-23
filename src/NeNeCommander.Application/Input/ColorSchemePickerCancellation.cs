using System;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Input;

/// <summary>Cancels the exact theme picker without selecting or persisting its preview.</summary>
public sealed record ColorSchemePickerCancellation : UserIntent
{
    internal ColorSchemePickerCancellation(ColorSchemePickerOpen expectedState)
    {
        ArgumentNullException.ThrowIfNull(expectedState);
        ExpectedState = expectedState;
    }

    /// <summary>Gets the exact picker that owns this cancellation.</summary>
    public ColorSchemePickerOpen ExpectedState { get; }

    private protected override void RestrictDerivation()
    {
    }
}
