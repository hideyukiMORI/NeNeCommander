namespace NeNeCommander.Application.Windowing;

/// <summary>
/// Represents the closed result of planning one window adjustment. A plan is a return value and
/// never state, so a later render, background read, or repeated snapshot cannot apply it again.
/// </summary>
public abstract record WindowAdjustmentPlan
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the plan that asks the host to maximize the window.</summary>
    public static WindowAdjustmentPlan Maximize { get; } = new MaximizePlan();

    /// <summary>Gets the plan that asks the host to restore the window to its normal placement.</summary>
    public static WindowAdjustmentPlan Restore { get; } = new RestorePlan();

    private protected WindowAdjustmentPlan()
    {
    }

    private sealed record MaximizePlan : WindowAdjustmentPlan
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record RestorePlan : WindowAdjustmentPlan
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
