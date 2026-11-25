using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>No complete matching completion frame exists; the target may have changed, irrespective of trailing byte count.</summary>
internal sealed record WindowsTransferRecoveryPrepared : WindowsTransferRecoveryInspection
{
    internal WindowsTransferRecoveryPrepared(WindowsTransferRecoveryMapping mapping, RecoveryFrameEvidence initial, KnownEntrySize incompleteTail)
        : base(mapping, initial)
    {
        IncompleteTail = incompleteTail;
    }

    internal KnownEntrySize IncompleteTail { get; }
}
