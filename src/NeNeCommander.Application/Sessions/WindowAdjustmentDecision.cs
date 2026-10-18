namespace NeNeCommander.Application.Sessions;

/// <summary>
/// Represents the closed result of offering one window action to the mode. The mode state is
/// already final when the result is returned, so the host applies a plan against a scope that has
/// already recorded its outcome.
/// </summary>
public abstract record WindowAdjustmentDecision
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the result that leaves the host nothing to apply and changes no state.</summary>
    public static WindowAdjustmentDecision NothingToApply { get; } = new NoDecision();

    private protected WindowAdjustmentDecision()
    {
    }

    private sealed record NoDecision : WindowAdjustmentDecision
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
