using System;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Sessions;

/// <summary>
/// Captures one open window-adjustment mode: the pane that was active at entry and the most recent
/// outcome. It captures no pane snapshot, so a background read that completes while the mode is
/// open does not end the scope.
/// </summary>
public sealed record WindowAdjustmentOpen : WindowAdjustmentState
{
    internal WindowAdjustmentOpen(PaneSide activeSide, WindowAdjustmentOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(activeSide);
        ArgumentNullException.ThrowIfNull(outcome);
        ActiveSide = activeSide;
        Outcome = outcome;
    }

    /// <summary>Gets the pane that was active when the mode opened and receives focus when it leaves.</summary>
    public PaneSide ActiveSide { get; }

    /// <summary>Gets the most recent outcome, or the idle outcome before the first action.</summary>
    public WindowAdjustmentOutcome Outcome { get; }
}
