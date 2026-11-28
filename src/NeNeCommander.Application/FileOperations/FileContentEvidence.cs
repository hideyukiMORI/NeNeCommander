using System;
using NeNeCommander.Application.Directories;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Retains the exact length and SHA-256 agreed by a complete content verification.</summary>
public sealed record FileContentEvidence
{
    private FileContentEvidence(KnownEntrySize size, FileContentHash hash)
    {
        Size = size;
        Hash = hash;
    }

    /// <summary>Gets the verified non-negative byte length, including zero-length content.</summary>
    public KnownEntrySize Size { get; }

    /// <summary>Gets the verified complete default-stream digest.</summary>
    public FileContentHash Hash { get; }

    /// <summary>Creates complete byte evidence; no metadata fidelity or future stability is implied.</summary>
    public static FileContentEvidence Create(KnownEntrySize size, FileContentHash hash)
    {
        ArgumentNullException.ThrowIfNull(size);
        ArgumentNullException.ThrowIfNull(hash);
        return new FileContentEvidence(size, hash);
    }
}
