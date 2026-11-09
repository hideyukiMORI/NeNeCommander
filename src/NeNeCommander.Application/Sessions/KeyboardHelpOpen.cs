using System;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Sessions;

/// <summary>
/// Captures one open keyboard help surface and the pane that was active at entry. It captures no
/// pane snapshot, so a background read that completes while the help is open does not close it.
/// </summary>
public sealed record KeyboardHelpOpen : KeyboardHelpState
{
    private protected override void RestrictDerivation()
    {
    }

    internal KeyboardHelpOpen(PaneSide activeSide)
    {
        ArgumentNullException.ThrowIfNull(activeSide);
        ActiveSide = activeSide;
    }

    /// <summary>Gets the pane that was active when the help opened and receives focus when it closes.</summary>
    public PaneSide ActiveSide { get; }
}
