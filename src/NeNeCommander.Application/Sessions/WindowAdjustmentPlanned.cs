using System;
using NeNeCommander.Application.Windowing;

namespace NeNeCommander.Application.Sessions;

/// <summary>Carries the one plan the host applies exactly once for a qualified window action.</summary>
public sealed record WindowAdjustmentPlanned : WindowAdjustmentDecision
{
    private protected override void RestrictDerivation()
    {
    }

    internal WindowAdjustmentPlanned(WindowAdjustmentPlan plan, WindowAdjustmentOpen owner)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(owner);
        Plan = plan;
        Owner = owner;
    }

    /// <summary>Gets the decided plan, which may itself be a refusal the host only renders.</summary>
    public WindowAdjustmentPlan Plan { get; }

    /// <summary>Gets the exact new state that may receive the qualified transfer observation.</summary>
    public WindowAdjustmentOpen Owner { get; }
}
