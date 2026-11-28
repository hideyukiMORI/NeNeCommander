using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>A complete saved completion observation matches this exact initial frame; current content and flush remain unproved.</summary>
internal sealed record WindowsTransferRecoveryCompleted : WindowsTransferRecoveryInspection
{
    internal WindowsTransferRecoveryCompleted(WindowsTransferRecoveryMapping mapping, RecoveryFrameEvidence initial, RecoveryFrameEvidence completion)
        : base(mapping, initial)
    {
        Completion = completion;
    }

    internal RecoveryFrameEvidence Completion { get; }
}
