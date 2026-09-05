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
        FileAttributes attributes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(kind);
        Name = name;
        Kind = kind;
        Attributes = attributes;
    }

    internal string Name { get; }

    internal DirectoryEntryKind Kind { get; }

    internal FileAttributes Attributes { get; }
}
