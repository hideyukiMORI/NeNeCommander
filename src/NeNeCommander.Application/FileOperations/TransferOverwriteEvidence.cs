using System;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Binds the original source and existing target observations without granting execution permission by itself.</summary>
public sealed record TransferOverwriteEvidence
{
    private TransferOverwriteEvidence(TransferOverwriteFileObservation source, TransferOverwriteFileObservation target)
    {
        Source = source;
        Target = target;
    }

    /// <summary>Gets the original source identity and displayed file facts.</summary>
    public TransferOverwriteFileObservation Source { get; }
    /// <summary>Gets the exact original existing target identity and displayed file facts.</summary>
    public TransferOverwriteFileObservation Target { get; }

    /// <summary>Rejects lexical or native-object aliases while retaining the provider's immutable observations.</summary>
    public static TransferOverwriteEvidence Create(TransferOverwriteFileObservation source, TransferOverwriteFileObservation target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        return FileSystemPathIdentityComparer.Instance.Equals(source.Observation.Entry.Path, target.Observation.Entry.Path) ||
            source.Observation.Entry.ObjectIdentity == target.Observation.Entry.ObjectIdentity
            ? throw new ArgumentException("The overwrite source and target must be different file objects and paths.", nameof(target))
            : new TransferOverwriteEvidence(source, target);
    }
}
