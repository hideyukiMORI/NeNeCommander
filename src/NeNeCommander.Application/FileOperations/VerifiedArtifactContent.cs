namespace NeNeCommander.Application.FileOperations;

/// <summary>Retains the last successful complete byte verification of an owned preparation; it does not assert current equality or grant mutation or cleanup authority.</summary>
public sealed record VerifiedArtifactContent : ArtifactContentObservation
{
    internal VerifiedArtifactContent(FileContentEvidence content)
    {
        Content = content;
    }

    /// <summary>Gets the historical verified length and digest; current equality requires fresh admission and comparison.</summary>
    public FileContentEvidence Content { get; }

    private protected override void RestrictDerivation()
    {
    }
}
