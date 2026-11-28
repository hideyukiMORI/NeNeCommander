using System;
using System.Collections.Generic;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Retains four distinct local file observations for a saved mapping, without asserting their current state.</summary>
internal sealed record WindowsTransferRecoveryFiles
{
    private WindowsTransferRecoveryFiles(VerifiedEntrySnapshot source, VerifiedEntrySnapshot target,
        VerifiedEntrySnapshot backup, VerifiedEntrySnapshot prepared)
    {
        Source = source;
        Target = target;
        Backup = backup;
        Prepared = prepared;
    }

    internal VerifiedEntrySnapshot Source { get; }
    internal VerifiedEntrySnapshot Target { get; }
    internal VerifiedEntrySnapshot Backup { get; }
    internal VerifiedEntrySnapshot Prepared { get; }

    internal static WindowsTransferRecoveryFiles Create(VerifiedEntrySnapshot source, VerifiedEntrySnapshot target,
        VerifiedEntrySnapshot backup, VerifiedEntrySnapshot prepared)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(backup);
        ArgumentNullException.ThrowIfNull(prepared);
        HashSet<FileObjectIdentity> identities = [];
        HashSet<FileSystemPath> paths = new(FileSystemPathIdentityComparer.Instance);
        foreach (VerifiedEntrySnapshot entry in new[] { source, target, backup, prepared })
        {
            if (entry.Path is not WindowsLocalPath { Parent: not null } || !paths.Add(entry.Path) || !identities.Add(entry.ObjectIdentity))
            {
                throw new ArgumentException("Recovery files require distinct non-root local paths and stable objects.", nameof(source));
            }
        }
        return new WindowsTransferRecoveryFiles(source, target, backup, prepared);
    }
}
