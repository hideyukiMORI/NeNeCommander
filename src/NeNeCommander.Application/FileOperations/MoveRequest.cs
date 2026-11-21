using System;
using System.Collections.Generic;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Represents a validated composite move request to one destination location.
/// </summary>
public sealed record MoveRequest : FileOperationRequest
{
    private protected override void RestrictDerivation()
    {
    }

    private MoveRequest(IReadOnlyList<FileSystemPath> sources, FileSystemPath destination)
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
        FileOperationRequestFailureKind? failure = ValidateTransfer(sources, destination);
        return failure is not null
            ? new FileOperationRequestRejected(failure)
            : new FileOperationRequestAccepted(new MoveRequest(sources, destination));
    }
}
