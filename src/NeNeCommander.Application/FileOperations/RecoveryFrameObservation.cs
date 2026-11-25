using System;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Retains complete frame bytes and their independent flush observation.</summary>
public sealed record RecoveryFrameObservation
{
    private RecoveryFrameObservation(RecoveryFrameEvidence frame, RecoveryFlushObservation flush)
    {
        Frame = frame;
        Flush = flush;
    }

    /// <summary>Gets complete validated frame bytes, retained even when a subsequent flush fails.</summary>
    public RecoveryFrameEvidence Frame { get; }

    /// <summary>Gets pending, unconfirmed or acknowledged native flush independently of readback.</summary>
    public RecoveryFlushObservation Flush { get; }

    /// <summary>Creates one immutable observation without promoting byte verification to flush success.</summary>
    public static RecoveryFrameObservation Create(RecoveryFrameEvidence frame, RecoveryFlushObservation flush)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(flush);
        return new RecoveryFrameObservation(frame, flush);
    }
}
