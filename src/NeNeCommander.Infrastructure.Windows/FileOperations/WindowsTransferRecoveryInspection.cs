using System;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Reports saved framed observations only, without live pathname/content/flush or recovery authority.</summary>
internal abstract record WindowsTransferRecoveryInspection
{
    private protected WindowsTransferRecoveryInspection(WindowsTransferRecoveryMapping mapping, RecoveryFrameEvidence initial)
    {
        Mapping = mapping;
        Initial = initial;
    }

    internal WindowsTransferRecoveryMapping Mapping { get; }
    internal RecoveryFrameEvidence Initial { get; }

    internal static WindowsTransferRecoveryInspection Prepared(WindowsTransferRecoveryMapping mapping,
        RecoveryFrameEvidence initial, KnownEntrySize incompleteTail)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentNullException.ThrowIfNull(initial);
        ArgumentNullException.ThrowIfNull(incompleteTail);
        return new WindowsTransferRecoveryPrepared(mapping, initial, incompleteTail);
    }

    internal static WindowsTransferRecoveryInspection Completed(WindowsTransferRecoveryMapping mapping,
        RecoveryFrameEvidence initial, RecoveryFrameEvidence completion)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentNullException.ThrowIfNull(initial);
        ArgumentNullException.ThrowIfNull(completion);
        return new WindowsTransferRecoveryCompleted(mapping, initial, completion);
    }
}
