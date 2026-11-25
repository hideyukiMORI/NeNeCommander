using System;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Owns one conflict's original target binding, including absence of qualified overwrite evidence.</summary>
public sealed record TransferConflictTarget
{
    private TransferConflictTarget(FileSystemPath path, TransferOverwriteEvidence? overwriteEvidence)
    {
        Path = path;
        OverwriteEvidence = overwriteEvidence;
    }

    /// <summary>Gets the original ordinary target path, which may also be only a batch reservation.</summary>
    public FileSystemPath Path { get; }
    /// <summary>Gets qualified original file observations, or absence when overwrite is not evidenced.</summary>
    public TransferOverwriteEvidence? OverwriteEvidence { get; }

    internal static TransferConflictTarget Create(FileSystemPath path, TransferOverwriteEvidence? overwriteEvidence)
    {
        ArgumentNullException.ThrowIfNull(path);
        return overwriteEvidence is not null &&
            !FileSystemPathIdentityComparer.Instance.Equals(path, overwriteEvidence.Target.Observation.Entry.Path)
            ? throw new ArgumentException("The overwrite evidence does not describe the original conflict target.", nameof(overwriteEvidence))
            : new TransferConflictTarget(path, overwriteEvidence);
    }
}
