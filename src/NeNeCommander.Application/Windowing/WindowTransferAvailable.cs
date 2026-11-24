using System;

namespace NeNeCommander.Application.Windowing;

/// <summary>Carries the complete observed display set of a visible own window.</summary>
public sealed record WindowTransferAvailable : WindowTransferInput
{
    internal WindowTransferAvailable(WindowDisplaySnapshot displays)
    {
        ArgumentNullException.ThrowIfNull(displays);
        Displays = displays;
    }

    /// <summary>Gets the owned fresh display observations.</summary>
    public WindowDisplaySnapshot Displays { get; }

    private protected override void RestrictDerivation()
    {
    }
}
