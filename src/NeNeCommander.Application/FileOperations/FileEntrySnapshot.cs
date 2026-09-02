using System;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Freezes provider identity and deletion capability for one preflighted source entry.
/// </summary>
public sealed record FileEntrySnapshot
{
    private FileEntrySnapshot(
        FileSystemPath path,
        FileIdentity identity,
        DeletionCapability deletionCapability)
    {
        Path = path;
        Identity = identity;
        DeletionCapability = deletionCapability;
    }

    /// <summary>Gets the source path captured during preflight.</summary>
    public FileSystemPath Path { get; }

    /// <summary>Gets the provider identity captured during preflight.</summary>
    public FileIdentity Identity { get; }

    /// <summary>Gets the provider-reported deletion capability.</summary>
    public DeletionCapability DeletionCapability { get; }

    /// <summary>
    /// Creates an immutable preflight snapshot from validated components.
    /// </summary>
    /// <param name="path">Validated source path.</param>
    /// <param name="identity">Validated provider identity.</param>
    /// <param name="deletionCapability">Provider delete capability.</param>
    /// <returns>A complete immutable snapshot.</returns>
    public static FileEntrySnapshot Create(
        FileSystemPath path,
        FileIdentity identity,
        DeletionCapability deletionCapability)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(deletionCapability);
        return new FileEntrySnapshot(path, identity, deletionCapability);
    }
}
