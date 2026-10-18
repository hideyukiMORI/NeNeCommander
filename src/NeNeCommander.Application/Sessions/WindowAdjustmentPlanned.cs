using System;
using NeNeCommander.Application.Windowing;

namespace NeNeCommander.Application.Sessions;

/// <summary>Carries the one plan the host applies exactly once for a qualified window action.</summary>
public sealed record WindowAdjustmentPlanned : WindowAdjustmentDecision
{
    private protected override void RestrictDerivation()
    {
    }

    internal WindowAdjustmentPlanned(WindowAdjustmentPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        Plan = plan;
    }

    /// <summary>Gets the decided plan, which may itself be a refusal the host only renders.</summary>
    public WindowAdjustmentPlan Plan { get; }
}
