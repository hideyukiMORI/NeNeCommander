using System;
using System.IO;
using NeNeCommander.Application.Directories;

namespace NeNeCommander.Infrastructure.Windows.Directories;

/// <summary>Freezes the provider facts needed to construct one directory entry.</summary>
internal sealed record WindowsDirectoryEntrySnapshot
{
    internal WindowsDirectoryEntrySnapshot(
        string name,
        FileAttributes attributes,
        EntrySize size,
        EntryTimestamp modified)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(size);
        ArgumentNullException.ThrowIfNull(modified);
        Name = name;
        Attributes = attributes;
        Size = size;
        Modified = modified;
    }

    internal string Name { get; }

    internal FileAttributes Attributes { get; }

    /// <summary>
    /// Gets the kind the attributes report. The enumeration returns a <see cref="DirectoryInfo"/>
    /// exactly when the directory attribute is set, for Windows local and WSL namespaces alike, so
    /// the kind is derived rather than carried twice.
    /// </summary>
    internal DirectoryEntryKind Kind => Attributes.HasFlag(FileAttributes.Directory)
        ? DirectoryEntryKind.Directory
        : DirectoryEntryKind.File;

    /// <summary>Gets the length read from the enumerated entry, or unknown (ADR-0056).</summary>
    internal EntrySize Size { get; }

    /// <summary>Gets the last-write time read from the enumerated entry, or unknown (ADR-0056).</summary>
    internal EntryTimestamp Modified { get; }
}
