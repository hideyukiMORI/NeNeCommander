using System;
using System.IO;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Freezes an entry's provider identity, kind and raw attributes as one observation.</summary>
internal sealed record WslEntryMetadata
{
    internal WslEntryMetadata(FileIdentity identity, DirectoryEntryKind kind, FileAttributes attributes)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(kind);
        Identity = identity;
        Kind = kind;
        Attributes = attributes;
    }

    internal FileIdentity Identity { get; }
    internal DirectoryEntryKind Kind { get; }
    internal FileAttributes Attributes { get; }
}
