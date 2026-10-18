using System;
using System.IO;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Freezes one WSL entry and its provider identity at an adapter boundary.</summary>
internal sealed record WslFileSystemEntry
{
    private readonly WslEntryMetadata _metadata;

    internal WslFileSystemEntry(
        WslPath path,
        string name,
        WslEntryMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(metadata);
        _metadata = metadata;
        Path = path;
        Name = name;
    }

    internal WslPath Path { get; }

    internal string Name { get; }

    internal FileIdentity Identity => _metadata.Identity;

    internal DirectoryEntryKind Kind => _metadata.Kind;

    internal FileAttributes Attributes => _metadata.Attributes;
}
