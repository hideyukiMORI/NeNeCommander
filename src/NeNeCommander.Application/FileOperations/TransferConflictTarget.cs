using System;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Owns one conflict's original target binding and its single qualified capability or refusal.</summary>
public sealed record TransferConflictTarget
{
    private TransferConflictTarget(FileSystemPath path, TransferOverwriteCapability overwrite)
    {
        Path = path;
        Overwrite = overwrite;
    }

    /// <summary>Gets the original ordinary target path, which may also be only a batch reservation.</summary>
    public FileSystemPath Path { get; }
    /// <summary>Gets the sole original overwrite capability or explicit refusal.</summary>
    public TransferOverwriteCapability Overwrite { get; }
    /// <summary>Gets the qualified observation from the sole capability, or absence when unavailable.</summary>
    public TransferOverwriteEvidence? OverwriteEvidence => (Overwrite as TransferOverwriteCapability.QualifiedOverwrite)?.Evidence;

    internal static TransferConflictTarget Create(FileSystemPath path, TransferOverwriteCapability overwrite)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(overwrite);
        return overwrite is TransferOverwriteCapability.QualifiedOverwrite qualified &&
            !FileSystemPathIdentityComparer.Instance.Equals(path, qualified.Evidence.Target.Observation.Entry.Path)
            ? throw new ArgumentException("The overwrite evidence does not describe the original conflict target.", nameof(overwrite))
            : new TransferConflictTarget(path, overwrite);
    }
}
