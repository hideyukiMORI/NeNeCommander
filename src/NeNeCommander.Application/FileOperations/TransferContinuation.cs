using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Retains the original transfer request and frozen source identities while conflict input is pending.</summary>
public sealed class TransferContinuation
{
    private readonly ReadOnlyCollection<FileEntrySnapshot> _sources;
    private readonly object _owner;
    private int _consumed;

    private TransferContinuation(
        FileOperationRequest request,
        ReadOnlyCollection<FileEntrySnapshot> sources,
        FileSystemPath destination,
        TransferResolution resolution,
        ConflictSet pendingConflicts,
        object owner)
    {
        Request = request;
        _sources = sources;
        Destination = destination;
        Resolution = resolution;
        PendingConflicts = pendingConflicts;
        _owner = owner;
    }

    internal FileOperationRequest Request { get; }
    /// <summary>Gets the original ordered frozen source snapshots.</summary>
    public IReadOnlyList<FileEntrySnapshot> Sources => _sources;
    /// <summary>Gets the original transfer destination.</summary>
    public FileSystemPath Destination { get; }
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
        FileOperationRequest request,
        IReadOnlyList<FileEntrySnapshot> sources,
        FileSystemPath destination,
        TransferResolution resolution,
        ConflictSet pendingConflicts,
        object owner)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(resolution);
        ArgumentNullException.ThrowIfNull(pendingConflicts);
        ArgumentNullException.ThrowIfNull(owner);
        return new TransferContinuation(
            request,
            new List<FileEntrySnapshot>(sources).AsReadOnly(),
            destination,
            resolution,
            pendingConflicts,
            owner);
    }
}
