using System;
using System.Collections.Generic;
using System.Threading;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Retains the original transfer request and frozen source identities while conflict input is pending.</summary>
public sealed class TransferContinuation
{

    private readonly object _owner;
    private int _consumed;

    private TransferContinuation(
        FrozenTransfer transfer,
        TransferResolution resolution,
        ConflictSet pendingConflicts,
        object owner)
    {
        Transfer = transfer;
        Resolution = resolution;
        PendingConflicts = pendingConflicts;
        _owner = owner;
    }

    internal FrozenTransfer Transfer { get; }
    internal FileOperationRequest Request => Transfer.Request;
    /// <summary>Gets the original ordered frozen source snapshots.</summary>
    public IReadOnlyList<FileEntrySnapshot> Sources => Transfer.Sources;
    /// <summary>Gets the original transfer destination.</summary>
    public FileSystemPath Destination => Transfer.Destination;
    internal TransferResolution Resolution { get; }
    internal ConflictSet PendingConflicts { get; }

    internal bool IsOwnedBy(object owner)
    {
        return ReferenceEquals(_owner, owner);
    }

    internal bool TryConsume()
    {
        return Interlocked.Exchange(ref _consumed, 1) == 0;
    }

    internal static TransferContinuation Create(
        FrozenTransfer transfer,
        TransferResolution resolution,
        ConflictSet pendingConflicts,
        object owner)
    {
        ArgumentNullException.ThrowIfNull(transfer);
        ArgumentNullException.ThrowIfNull(resolution);
        ArgumentNullException.ThrowIfNull(pendingConflicts);
        ArgumentNullException.ThrowIfNull(owner);
        return new TransferContinuation(
            transfer,
            resolution,
            pendingConflicts,
            owner);
    }
}
