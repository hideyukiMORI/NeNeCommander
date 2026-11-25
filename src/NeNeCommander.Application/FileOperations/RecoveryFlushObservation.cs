namespace NeNeCommander.Application.FileOperations;

/// <summary>Distinguishes byte verification from the native flush acknowledgement without promising hardware durability.</summary>
public abstract record RecoveryFlushObservation
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the state before a flush was attempted.</summary>
    public static RecoveryFlushObservation NotAttempted { get; } = new PendingFlush();

    /// <summary>Gets a flush attempt whose acknowledgement was not established.</summary>
    public static RecoveryFlushObservation OutcomeUnconfirmed { get; } = new UnconfirmedFlush();

    /// <summary>Gets the acknowledged native flush, without a power-loss consistency claim.</summary>
    public static RecoveryFlushObservation Confirmed { get; } = new ConfirmedFlush();

    private RecoveryFlushObservation()
    {
    }

    private sealed record PendingFlush : RecoveryFlushObservation
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record UnconfirmedFlush : RecoveryFlushObservation
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record ConfirmedFlush : RecoveryFlushObservation
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
