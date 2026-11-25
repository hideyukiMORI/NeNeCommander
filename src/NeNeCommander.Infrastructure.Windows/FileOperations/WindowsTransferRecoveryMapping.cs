using System;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Describes the prepared original and desired content without authorizing recovery or claiming present filesystem facts.</summary>
internal sealed record WindowsTransferRecoveryMapping
{
    private WindowsTransferRecoveryMapping(WindowsTransferArtifactNames names, WindowsTransferRecoveryFiles files,
        FileContentEvidence original, FileContentEvidence desired)
    {
        Names = names;
        Files = files;
        Original = original;
        Desired = desired;
    }

    internal WindowsTransferArtifactNames Names { get; }
    internal WindowsTransferRecoveryFiles Files { get; }
    internal FileContentEvidence Original { get; }
    internal FileContentEvidence Desired { get; }

    internal static WindowsTransferRecoveryMapping Create(Guid token, WindowsTransferRecoveryFiles files,
        FileContentEvidence original, FileContentEvidence desired)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(desired);
        WindowsTransferArtifactNames names = WindowsTransferArtifactNames.Create((WindowsLocalPath)files.Target.Path, token);
        return files.Backup.Path != names.OriginalBackup || files.Prepared.Path != names.PreparedContent ||
            FileSystemPathIdentityComparer.Instance.Equals(files.Source.Path, names.Recovery)
            ? throw new ArgumentException("The recovery artifacts do not match the exact target parent and operation token.", nameof(files))
            : new WindowsTransferRecoveryMapping(names, files, original, desired);
    }
}
