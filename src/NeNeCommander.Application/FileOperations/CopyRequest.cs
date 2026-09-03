using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Represents a validated composite copy request to one destination location. The sources are
/// never deleted; the gateway copies and verifies each one beneath the destination.
/// </summary>
public sealed record CopyRequest : FileOperationRequest
{
    private CopyRequest(ReadOnlyCollection<FileSystemPath> sources, FileSystemPath destination)
        : base(sources)
    {
        Destination = destination;
    }

    /// <summary>Gets the frozen destination location.</summary>
    public FileSystemPath Destination { get; }

    /// <summary>Creates a validated immutable copy request.</summary>
    /// <param name="sources">Ordered source paths.</param>
    /// <param name="destination">Destination location.</param>
    /// <returns>An accepted request or a typed rejection.</returns>
    public static FileOperationRequestCreation Create(
        IReadOnlyList<FileSystemPath> sources,
        FileSystemPath destination)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(destination);
        FileOperationRequestFailureKind? failure = ValidateTransfer(sources, destination);
        if (failure is not null)
        {
            return new FileOperationRequestRejected(failure);
        }
        List<FileSystemPath> ownedSources = [.. sources];
        return new FileOperationRequestAccepted(new CopyRequest(ownedSources.AsReadOnly(), destination));
    }
}
