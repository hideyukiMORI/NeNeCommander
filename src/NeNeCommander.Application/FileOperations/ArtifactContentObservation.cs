using System;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Retains preparation observations, including the last successful complete byte proof; none asserts current content or mutation authority.</summary>
public abstract record ArtifactContentObservation
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    private protected ArtifactContentObservation()
    {
    }

    /// <summary>Gets content for which no complete or partial claim is established.</summary>
    public static ArtifactContentObservation Unknown { get; } = new UnknownContent();

    /// <summary>Gets content known to be incomplete; it is not a usable verified backup.</summary>
    public static ArtifactContentObservation Partial { get; } = new PartialContent();

    /// <summary>Retains the last successful complete byte verification; later read failures do not erase this historical evidence.</summary>
    public static ArtifactContentObservation Verified(FileContentEvidence content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new VerifiedArtifactContent(content);
    }

    /// <summary>Retains the established mapping prefix and its independent append observation.</summary>
    public static ArtifactContentObservation RecoveryMapping(RecoveryFrameObservation initialFrame, RecoveryAppendObservation append)
    {
        ArgumentNullException.ThrowIfNull(initialFrame);
        ArgumentNullException.ThrowIfNull(append);
        return initialFrame.Flush != RecoveryFlushObservation.Confirmed && append != RecoveryAppendObservation.NotAttempted
            ? throw new ArgumentException("An append cannot precede the initial mapping flush acknowledgement.", nameof(append))
            : new RecoveryMappingContent(initialFrame, append);
    }

    private sealed record UnknownContent : ArtifactContentObservation
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record PartialContent : ArtifactContentObservation
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
