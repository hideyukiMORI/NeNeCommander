using System;
using NeNeCommander.Application.Windowing;

namespace NeNeCommander.Application.Sessions;

/// <summary>Names the action the mode last decided and handed to the host.</summary>
public sealed record WindowActionPlanned : WindowAdjustmentOutcome
{
    internal WindowActionPlanned(WindowAdjustmentAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        Action = action;
    }

    /// <summary>Gets the closed action the plan carried.</summary>
    public WindowAdjustmentAction Action { get; }
}
