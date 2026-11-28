using System;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Reports the desired-content frame append independently of the already established initial mapping.</summary>
public abstract record RecoveryAppendObservation
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    private protected RecoveryAppendObservation()
    {
    }

    /// <summary>Gets the state before the first append write; the initial mapping still stands.</summary>
    public static RecoveryAppendObservation NotAttempted { get; } = new PendingAppend();

    /// <summary>Gets a write/verification/flush attempt whose complete appended frame was not established.</summary>
    public static RecoveryAppendObservation OutcomeUnconfirmed { get; } = new UnconfirmedAppend();

    /// <summary>Reports a complete validated desired-content frame with its separate flush observation.</summary>
    public static RecoveryAppendObservation Verified(RecoveryFrameObservation frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        return new VerifiedRecoveryAppend(frame);
    }

    private sealed record PendingAppend : RecoveryAppendObservation
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record UnconfirmedAppend : RecoveryAppendObservation
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
