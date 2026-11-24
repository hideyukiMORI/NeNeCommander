namespace NeNeCommander.Application.Windowing;

/// <summary>Classifies the observed effect boundary of one bounded transfer apply.</summary>
public abstract record WindowTransferEffect
{
    /// <summary>Restricts concrete effect classifications to this assembly and trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets refusal before entering the first changing platform call.</summary>
    public static WindowTransferEffect NotApplied { get; } = new NotAppliedEffect();

    /// <summary>Gets an uncertain effect after entering the first changing platform call.</summary>
    public static WindowTransferEffect Unconfirmed { get; } = new UnconfirmedEffect();

    /// <summary>Gets the effect whose exact final bounds and qualifications were observed immediately.</summary>
    public static WindowTransferEffect Observed { get; } = new ObservedEffect();

    private WindowTransferEffect()
    {
    }

    private sealed record NotAppliedEffect : WindowTransferEffect
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record UnconfirmedEffect : WindowTransferEffect
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record ObservedEffect : WindowTransferEffect
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
