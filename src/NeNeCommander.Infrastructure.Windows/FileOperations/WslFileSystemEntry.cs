using System;
using System.IO;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Freezes one WSL entry and its provider identity at an adapter boundary.</summary>
internal sealed record WslFileSystemEntry
{
    internal WslFileSystemEntry(
        WslPath path,
        string name,
        FileIdentity identity,
        DirectoryEntryKind kind,
        FileAttributes attributes)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(kind);
        Path = path;
        Name = name;
        Identity = identity;
        Kind = kind;
        Attributes = attributes;
    }

    internal WslPath Path { get; }

    internal string Name { get; }

    internal FileIdentity Identity { get; }

    internal DirectoryEntryKind Kind { get; }

    internal FileAttributes Attributes { get; }
}
