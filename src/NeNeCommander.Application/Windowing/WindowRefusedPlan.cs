using System;

namespace NeNeCommander.Application.Windowing;

/// <summary>Carries the closed reason one window adjustment was refused; the host applies nothing.</summary>
public sealed record WindowRefusedPlan : WindowAdjustmentPlan
{
    private protected override void RestrictDerivation()
    {
    }

    internal WindowRefusedPlan(WindowAdjustmentRefusal reason)
    {
        ArgumentNullException.ThrowIfNull(reason);
        Reason = reason;
    }

    /// <summary>Gets the closed refusal reason the helper shows.</summary>
    public WindowAdjustmentRefusal Reason { get; }
}
