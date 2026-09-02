using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Represents a validated composite move request to one destination location.
/// </summary>
public sealed record MoveRequest : FileOperationRequest
{
    private MoveRequest(ReadOnlyCollection<FileSystemPath> sources, FileSystemPath destination)
        : base(sources)
    {
        Destination = destination;
    }

    /// <summary>Gets the frozen destination location.</summary>
    public FileSystemPath Destination { get; }

    /// <summary>Creates a validated immutable move request.</summary>
    /// <param name="sources">Ordered source paths.</param>
    /// <param name="destination">Destination location.</param>
    /// <returns>An accepted request or a typed rejection.</returns>
    public static FileOperationRequestCreation Create(
        IReadOnlyList<FileSystemPath> sources,
        FileSystemPath destination)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(destination);
        FileOperationRequestFailureKind? failure = ValidateSources(sources, destination);
        if (failure is not null)
        {
            return new FileOperationRequestRejected(failure);
        }
        List<FileSystemPath> ownedSources = [.. sources];
        return new FileOperationRequestAccepted(new MoveRequest(ownedSources.AsReadOnly(), destination));
    }

    private static FileOperationRequestFailureKind? ValidateSources(
        IReadOnlyList<FileSystemPath> sources,
        FileSystemPath destination)
    {
        FileOperationRequestFailureKind? failure = ValidateSourceSet(sources);
        if (failure is not null)
        {
            return failure;
        }

        foreach (FileSystemPath source in sources)
        {
            if (FileSystemPathIdentityComparer.Instance.Equals(source, destination))
            {
                return FileOperationRequestFailureKind.DestinationIsSource;
            }
        }
        return null;
    }
}
