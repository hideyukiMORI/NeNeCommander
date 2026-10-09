using System;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Directories;

/// <summary>
/// Represents one immutable direct entry of a read directory with its validated path.
/// </summary>
public sealed record DirectoryEntry
{
    private DirectoryEntry(
        FileSystemPath path,
        string name,
        DirectoryEntryKind kind,
        EntryMetadata metadata)
    {
        Path = path;
        Name = name;
        Kind = kind;
        Metadata = metadata;
    }

    /// <summary>Gets the validated path of the entry inside its parent location.</summary>
    public FileSystemPath Path { get; }

    /// <summary>Gets the provider-reported entry name used for display and ordering.</summary>
    public string Name { get; }

    /// <summary>Gets the closed entry kind.</summary>
    public DirectoryEntryKind Kind { get; }

    /// <summary>
    /// Gets the visibility, size, and last-modification time the provider reported for the entry
    /// (ADR-0056). Size and time may be unknown; nothing infers them.
    /// </summary>
    public EntryMetadata Metadata { get; }

    /// <summary>
    /// Creates an entry from components an adapter has already parsed at its boundary.
    /// </summary>
    /// <param name="path">Validated entry path.</param>
    /// <param name="name">Non-empty provider-reported entry name.</param>
    /// <param name="kind">Closed entry kind.</param>
    /// <param name="metadata">Visibility, size, and time the provider reported for the entry.</param>
    /// <returns>A complete immutable entry.</returns>
    /// <exception cref="ArgumentException">The name is empty or whitespace, which is an adapter defect.</exception>
    public static DirectoryEntry Create(
        FileSystemPath path,
        string name,
        DirectoryEntryKind kind,
        EntryMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(kind);
        ArgumentNullException.ThrowIfNull(metadata);
        return new DirectoryEntry(path, name, kind, metadata);
    }
}
