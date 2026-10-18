using System;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Freezes provider identity and deletion capability for one preflighted source entry.
/// </summary>
public sealed record FileEntrySnapshot
{
    private readonly FileEntryTransfer _transfer;

    private FileEntrySnapshot(
        FileSystemPath path,
        FileIdentity identity,
        DeletionCapability deletionCapability,
        FileEntryTransfer transfer)
    {
        Path = path;
        Identity = identity;
        DeletionCapability = deletionCapability;
        _transfer = transfer;
    }

    /// <summary>Gets the source path captured during preflight.</summary>
    public FileSystemPath Path { get; }

    /// <summary>Gets the provider identity captured during preflight.</summary>
    public FileIdentity Identity { get; }

    /// <summary>Gets the provider-reported deletion capability.</summary>
    public DeletionCapability DeletionCapability { get; }

    /// <summary>Gets the exact preflighted transfer target, or absence before transfer preflight.</summary>
    public FileSystemPath? TransferTarget => _transfer.Target;

    /// <summary>Gets the operation-scoped conflict choice applied during resumed preflight.</summary>
    public TransferConflictChoice? ConflictChoice => _transfer.Choice;

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
        return new FileEntrySnapshot(path, identity, deletionCapability, FileEntryTransfer.Create(null, null));
    }

    internal FileEntrySnapshot WithTransferTarget(FileSystemPath target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return new FileEntrySnapshot(Path, Identity, DeletionCapability, FileEntryTransfer.Create(target, ConflictChoice));
    }

    internal FileEntrySnapshot WithConflictChoice(TransferConflictChoice? choice)
    {
        return new FileEntrySnapshot(Path, Identity, DeletionCapability, FileEntryTransfer.Create(TransferTarget, choice));
    }
}
