using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Retains the original transfer request and frozen source identities while conflict input is pending.</summary>
public sealed record TransferContinuation
{
    private readonly ReadOnlyCollection<FileEntrySnapshot> _sources;
    private int _consumed;

    private TransferContinuation(
        FileOperationRequest request,
        ReadOnlyCollection<FileEntrySnapshot> sources,
        FileSystemPath destination,
        TransferResolution resolution)
    {
        Request = request;
        _sources = sources;
        Destination = destination;
        Resolution = resolution;
    }

    internal FileOperationRequest Request { get; }
    /// <summary>Gets the original ordered frozen source snapshots.</summary>
    public IReadOnlyList<FileEntrySnapshot> Sources => _sources;
    /// <summary>Gets the original transfer destination.</summary>
    public FileSystemPath Destination { get; }
    internal TransferResolution Resolution { get; }

    internal bool TryConsume()
    {
        return Interlocked.Exchange(ref _consumed, 1) == 0;
    }

    internal static TransferContinuation Create(
        FileOperationRequest request,
        IReadOnlyList<FileEntrySnapshot> sources,
        FileSystemPath destination,
        TransferResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(resolution);
        return new TransferContinuation(request, new List<FileEntrySnapshot>(sources).AsReadOnly(), destination, resolution);
    }
}
