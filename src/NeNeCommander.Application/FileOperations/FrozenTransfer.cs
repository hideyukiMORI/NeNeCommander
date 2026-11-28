using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Owns the original request, ordered source identities and destination across transfer attempts.</summary>
internal sealed record FrozenTransfer
{
    private readonly ImmutableList<FileEntrySnapshot> _sources;

    private FrozenTransfer(FileOperationRequest request, ImmutableList<FileEntrySnapshot> sources, FileSystemPath destination)
    {
        Request = request;
        _sources = sources;
        Destination = destination;
    }

    internal FileOperationRequest Request { get; }
    internal IReadOnlyList<FileEntrySnapshot> Sources => _sources;
    internal FileSystemPath Destination { get; }

    internal static FrozenTransfer Create(FileOperationRequest request, IReadOnlyList<FileEntrySnapshot> sources, FileSystemPath destination)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(destination);
        ImmutableList<FileEntrySnapshot> snapshot = [.. sources];
        foreach (FileEntrySnapshot source in snapshot)
        {
            ArgumentNullException.ThrowIfNull(source);
        }
        return new FrozenTransfer(request, snapshot, destination);
    }
}
