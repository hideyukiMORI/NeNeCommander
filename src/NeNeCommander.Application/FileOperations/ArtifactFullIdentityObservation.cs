using System;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Distinguishes no complete metadata identity yet from the last successful full observation for an identified artifact.</summary>
public abstract record ArtifactFullIdentityObservation
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    private protected ArtifactFullIdentityObservation()
    {
    }

    /// <summary>Gets the explicit knowledge state before any complete metadata identity has been obtained.</summary>
    public static ArtifactFullIdentityObservation NotObserved { get; } = new UnobservedArtifactFullIdentity();

    /// <summary>Retains the last complete observation; it is not a guarantee that current metadata or bytes still match.</summary>
    public static ArtifactFullIdentityObservation Observed(FileIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        return new ObservedArtifactFullIdentity(identity);
    }

    private sealed record UnobservedArtifactFullIdentity : ArtifactFullIdentityObservation
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
