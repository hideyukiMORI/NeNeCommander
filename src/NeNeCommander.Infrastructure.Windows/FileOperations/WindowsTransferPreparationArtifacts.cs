using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Registers all three candidate owners before the first native creation, and closes only their handles on disposal.</summary>
internal sealed class WindowsTransferPreparationArtifacts : IDisposable
{
    private WindowsTransferPreparationArtifacts(WindowsTransferArtifactNames names, WindowsTransferPathLease parent, WindowsTransferArtifactQueries queries)
    {
        Names = names;
        Recovery = WindowsTransferArtifactOwner.Prepare(TransferArtifactRole.RecoveryRecord, names.Recovery, parent, queries);
        Backup = WindowsTransferArtifactOwner.Prepare(TransferArtifactRole.OriginalBackup, names.OriginalBackup, parent, queries);
        Prepared = WindowsTransferArtifactOwner.Prepare(TransferArtifactRole.PreparedContent, names.PreparedContent, parent, queries);
    }

    internal WindowsTransferArtifactNames Names { get; }
    internal WindowsTransferArtifactOwner Recovery { get; }
    internal WindowsTransferArtifactOwner Backup { get; }
    internal WindowsTransferArtifactOwner Prepared { get; }

    internal static WindowsTransferPreparationArtifacts Create(WindowsTransferArtifactNames names, WindowsTransferPathLease parent, WindowsTransferArtifactQueries queries)
    {
        ArgumentNullException.ThrowIfNull(names);
        ArgumentNullException.ThrowIfNull(parent);
        return new WindowsTransferPreparationArtifacts(names, parent, queries);
    }

    internal ProviderStepReceipt Snapshot(ProviderTargetObservation target)
    {
        List<TransferArtifactReceipt> receipts = [.. new[] { Recovery, Backup, Prepared }
            .Select(owner => owner.Receipt).OfType<TransferArtifactReceipt>()];
        return ProviderStepReceipt.Create([], target, receipts);
    }

    internal void CreateNative(WindowsTransferSecurityDescriptor security, CancellationToken cancellationToken)
    {
        foreach (WindowsTransferArtifactOwner owner in new[] { Recovery, Backup, Prepared })
        {
            cancellationToken.ThrowIfCancellationRequested();
            owner.CreateNative(security);
            _ = owner.AdmitBeforeTargetMutation();
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    public void Dispose()
    {
        Prepared.Dispose();
        Backup.Dispose();
        Recovery.Dispose();
    }
}
