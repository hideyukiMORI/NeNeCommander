using System.Collections.Generic;
using System.Collections.ObjectModel;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Represents a validated delete request with optional exact permanent-delete confirmation.
/// </summary>
public sealed record DeleteRequest : FileOperationRequest
{
    private DeleteRequest(
        ReadOnlyCollection<FileSystemPath> sources,
        PermanentDeletionConfirmation? confirmation)
        : base(sources)
    {
        Confirmation = confirmation;
    }

    /// <summary>Gets explicit permanent confirmation, or absence when recycle is expected.</summary>
    public PermanentDeletionConfirmation? Confirmation { get; }

    /// <summary>Creates a validated immutable delete request.</summary>
    /// <param name="sources">Ordered source paths.</param>
    /// <param name="confirmation">Exact permanent confirmation, or absence.</param>
    /// <returns>An accepted request or a typed rejection.</returns>
    public static FileOperationRequestCreation Create(
        IReadOnlyList<FileSystemPath> sources,
        PermanentDeletionConfirmation? confirmation)
    {
        FileOperationRequestFailureKind? failure = ValidateSourceSet(sources);
        if (failure is not null)
        {
            return new FileOperationRequestRejected(failure);
        }
        List<FileSystemPath> ownedSources = [.. sources];
        return new FileOperationRequestAccepted(new DeleteRequest(ownedSources.AsReadOnly(), confirmation));
    }
}
