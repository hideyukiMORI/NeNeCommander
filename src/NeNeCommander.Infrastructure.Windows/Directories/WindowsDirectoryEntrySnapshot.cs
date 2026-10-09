using System;
using System.IO;
using NeNeCommander.Application.Directories;

namespace NeNeCommander.Infrastructure.Windows.Directories;

/// <summary>Freezes the provider facts needed to construct one directory entry.</summary>
internal sealed record WindowsDirectoryEntrySnapshot
{
    internal WindowsDirectoryEntrySnapshot(
        string name,
        DirectoryEntryKind kind,
        FileAttributes attributes,
        EntryMetadata metadata)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(kind);
        ArgumentNullException.ThrowIfNull(metadata);
        Name = name;
        Kind = kind;
        Attributes = attributes;
        Metadata = metadata;
    }

    internal string Name { get; }

    internal DirectoryEntryKind Kind { get; }

    internal FileAttributes Attributes { get; }

    /// <summary>
    /// Gets the length and last-write time read from the enumerated entry (ADR-0056); a fact the
    /// enumeration could not provide is unknown, never zero or a minimum time.
    /// </summary>
    internal EntryMetadata Metadata { get; }
}
