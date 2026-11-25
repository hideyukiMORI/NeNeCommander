using System;

namespace NeNeCommander.Application.Windowing;

/// <summary>Declares whether a transfer read was requested, unavailable, hidden, or complete.</summary>
public abstract record WindowTransferInput
{
    /// <summary>Restricts concrete input cases to this assembly and trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the input for ordinary actions that never read transfer-only platform information.</summary>
    public static WindowTransferInput NotRequested { get; } = new NotRequestedInput();

    /// <summary>Gets the input when supported, consistent display information could not be obtained.</summary>
    public static WindowTransferInput Unavailable { get; } = new UnavailableInput();

    /// <summary>Gets the input when the own window is hidden and no display scale needs to be read.</summary>
    public static WindowTransferInput Hidden { get; } = new HiddenInput();

    private protected WindowTransferInput()
    {
    }

    /// <summary>Publishes complete facts for a visible window.</summary>
    /// <param name="displays">Owned current display observations.</param>
    /// <returns>The complete closed transfer input.</returns>
    public static WindowTransferInput Create(WindowDisplaySnapshot displays)
    {
        ArgumentNullException.ThrowIfNull(displays);
        return new WindowTransferAvailable(displays);
    }

    private sealed record NotRequestedInput : WindowTransferInput
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record UnavailableInput : WindowTransferInput
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record HiddenInput : WindowTransferInput
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
