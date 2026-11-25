namespace NeNeCommander.Application.FileOperations;

/// <summary>Retains the exact observed artifact object with independent verification and link-cleanup facts.</summary>
public sealed record IdentifiedTransferArtifact : TransferArtifactReceipt
{
    internal IdentifiedTransferArtifact(
        TransferArtifactRole role,
        TransferArtifactEntry artifact,
        ArtifactContentObservation content,
        ArtifactLinkObservation link)
        : base(role)
    {
        Artifact = artifact;
        Content = content;
        Link = link;
    }

    /// <summary>Gets the last exact object identities and its qualified typed path.</summary>
    public TransferArtifactEntry Artifact { get; }

    /// <summary>Gets unknown, partial or completely verified content evidence.</summary>
    public ArtifactContentObservation Content { get; }

    /// <summary>Gets the outcome for the owned link; possible late aliases are not declared removed.</summary>
    public ArtifactLinkObservation Link { get; }

    private protected override void RestrictDerivation()
    {
    }
}
