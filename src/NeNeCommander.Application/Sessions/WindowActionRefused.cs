using System;
using NeNeCommander.Application.Windowing;

namespace NeNeCommander.Application.Sessions;

/// <summary>Names the closed reason the mode last refused an action.</summary>
public sealed record WindowActionRefused : WindowAdjustmentOutcome
{
    internal WindowActionRefused(WindowAdjustmentRefusal reason)
    {
        ArgumentNullException.ThrowIfNull(reason);
        Reason = reason;
    }

    /// <summary>Gets the closed refusal reason.</summary>
    public WindowAdjustmentRefusal Reason { get; }
}
