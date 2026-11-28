namespace NeNeCommander.Application.FileOperations;

/// <summary>Qualifies whether a retained object path was confirmed by the reporting observation.</summary>
public abstract record TransferPathObservation
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets a path whose association with the held object was confirmed at the observation.</summary>
    public static TransferPathObservation CurrentPathVerified { get; } = new CurrentPathVerifiedCase();

    /// <summary>Gets a previously observed path whose current association is not established.</summary>
    public static TransferPathObservation LastKnownPath { get; } = new LastKnownPathCase();

    private TransferPathObservation()
    {
    }

    private sealed record CurrentPathVerifiedCase : TransferPathObservation
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record LastKnownPathCase : TransferPathObservation
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
