namespace NeNeCommander.Application.FileOperations;

/// <summary>Retains the separately verified desired-content frame appended after the initial recovery mapping.</summary>
public sealed record VerifiedRecoveryAppend : RecoveryAppendObservation
{
    internal VerifiedRecoveryAppend(RecoveryFrameObservation frame)
    {
        Frame = frame;
    }

    /// <summary>Gets complete appended-frame bytes and independent flush evidence; the initial prefix remains separate.</summary>
    public RecoveryFrameObservation Frame { get; }

    private protected override void RestrictDerivation()
    {
    }
}
