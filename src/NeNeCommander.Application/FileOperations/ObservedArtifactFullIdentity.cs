namespace NeNeCommander.Application.FileOperations;

/// <summary>Retains the most recent successful complete metadata identity independently of later failed queries.</summary>
public sealed record ObservedArtifactFullIdentity : ArtifactFullIdentityObservation
{
    internal ObservedArtifactFullIdentity(FileIdentity identity)
    {
        Identity = identity;
    }

    /// <summary>Gets the last completely observed identity, without a current-content or cleanup-authority claim.</summary>
    public FileIdentity Identity { get; }

    private protected override void RestrictDerivation()
    {
    }
}
