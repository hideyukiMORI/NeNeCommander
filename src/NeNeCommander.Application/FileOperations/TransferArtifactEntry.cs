using System;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Retains a known artifact object even when complete metadata has not been observed; it grants no cleanup authority.</summary>
public sealed record TransferArtifactEntry
{
    private TransferArtifactEntry(WindowsLocalPath path, FileObjectIdentity objectIdentity,
        ArtifactFullIdentityObservation fullIdentity, TransferPathObservation pathObservation)
    {
        Path = path;
        ObjectIdentity = objectIdentity;
        FullIdentity = fullIdentity;
        PathObservation = pathObservation;
    }

    /// <summary>Gets the non-root local artifact path, qualified separately as current or last-known.</summary>
    public WindowsLocalPath Path { get; }
    /// <summary>Gets the stable identity already obtained from the owned handle, never replaced by a candidate token.</summary>
    public FileObjectIdentity ObjectIdentity { get; }
    /// <summary>Gets whether a complete metadata identity was ever obtained and retains its last successful value.</summary>
    public ArtifactFullIdentityObservation FullIdentity { get; }
    /// <summary>Gets the independently observed path association; a failed later query cannot assert a current path.</summary>
    public TransferPathObservation PathObservation { get; }

    /// <summary>Creates an immutable artifact observation; native ownership, shape and later knowledge retention remain the attempt owner's responsibility.</summary>
    public static TransferArtifactEntry Create(WindowsLocalPath path, FileObjectIdentity objectIdentity,
        ArtifactFullIdentityObservation fullIdentity, TransferPathObservation pathObservation)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(objectIdentity);
        ArgumentNullException.ThrowIfNull(fullIdentity);
        ArgumentNullException.ThrowIfNull(pathObservation);
        return path.Parent is null
            ? throw new ArgumentException("An artifact observation requires a non-root Windows local path.", nameof(path))
            : new TransferArtifactEntry(path, objectIdentity, fullIdentity, pathObservation);
    }
}
