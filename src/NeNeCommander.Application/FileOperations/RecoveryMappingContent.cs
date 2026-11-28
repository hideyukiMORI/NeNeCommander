namespace NeNeCommander.Application.FileOperations;

/// <summary>Retains complete initial mapping bytes and their flush result independently of subsequent append observations.</summary>
public sealed record RecoveryMappingContent : ArtifactContentObservation
{
    internal RecoveryMappingContent(RecoveryFrameObservation initialFrame, RecoveryAppendObservation append)
    {
        InitialFrame = initialFrame;
        Append = append;
    }

    /// <summary>Gets the initial frame observation; later append results cannot overwrite this baseline.</summary>
    public RecoveryFrameObservation InitialFrame { get; }

    /// <summary>Gets independent pending, unconfirmed or verified desired-content append evidence.</summary>
    public RecoveryAppendObservation Append { get; }

    private protected override void RestrictDerivation()
    {
    }
}
