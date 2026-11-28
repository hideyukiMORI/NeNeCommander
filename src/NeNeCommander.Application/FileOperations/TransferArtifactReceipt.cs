using System;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Retains every known or unconfirmed preparation without turning a candidate name into cleanup authority.</summary>
public abstract record TransferArtifactReceipt
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    private protected TransferArtifactReceipt(TransferArtifactRole role)
    {
        ArgumentNullException.ThrowIfNull(role);
        Role = role;
    }

    /// <summary>Gets the artifact's preparation purpose.</summary>
    public TransferArtifactRole Role { get; }

    /// <summary>Records an attempted creation whose exact object identity could not be obtained.</summary>
    public static TransferArtifactReceipt Unidentified(TransferArtifactRole role, FileSystemPath candidatePath)
    {
        ArgumentNullException.ThrowIfNull(candidatePath);
        return new UnidentifiedTransferArtifact(role, candidatePath);
    }

    /// <summary>Records an exact handle-derived object and its independent content and cleanup observations.</summary>
    public static TransferArtifactReceipt Identified(
        TransferArtifactRole role,
        TransferArtifactEntry artifact,
        ArtifactContentObservation content,
        ArtifactLinkObservation link)
    {
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(link);
        return !MatchesContentRole(role, content)
            ? throw new ArgumentException("Recovery mapping evidence and ordinary artifact content cannot be substituted.", nameof(content))
            : link != ArtifactLinkObservation.Retained && artifact.PathObservation == TransferPathObservation.CurrentPathVerified
                ? throw new ArgumentException("An unconfirmed or removed link cannot claim a current object path.", nameof(artifact))
                : new IdentifiedTransferArtifact(role, artifact, content, link);
    }

    private static bool MatchesContentRole(TransferArtifactRole role, ArtifactContentObservation content)
    {
        return role == TransferArtifactRole.RecoveryRecord
            ? content is not VerifiedArtifactContent
            : content is not RecoveryMappingContent;
    }

}
