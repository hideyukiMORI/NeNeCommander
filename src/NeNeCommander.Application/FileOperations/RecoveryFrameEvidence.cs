using System;
using NeNeCommander.Application.Directories;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Pins one complete validated recovery frame, including its framing bytes.</summary>
public sealed record RecoveryFrameEvidence
{
    private RecoveryFrameEvidence(KnownEntrySize size, FileContentHash hash)
    {
        Size = size;
        Hash = hash;
    }

    /// <summary>Gets the positive framed byte length, not the full record length after later appends.</summary>
    public KnownEntrySize Size { get; }

    /// <summary>Gets the SHA-256 of exactly the complete framed bytes.</summary>
    public FileContentHash Hash { get; }

    /// <summary>Creates evidence after complete frame validation; this alone makes no flush or power-loss claim.</summary>
    public static RecoveryFrameEvidence Create(KnownEntrySize size, FileContentHash hash)
    {
        ArgumentNullException.ThrowIfNull(size);
        ArgumentNullException.ThrowIfNull(hash);
        ArgumentOutOfRangeException.ThrowIfZero(size.Bytes);
        return new RecoveryFrameEvidence(size, hash);
    }
}
