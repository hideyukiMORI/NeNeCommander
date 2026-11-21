using System.Collections.Generic;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Represents a validated delete request with optional exact permanent-delete confirmation.
/// </summary>
public sealed record DeleteRequest : FileOperationRequest
{
    private protected override void RestrictDerivation()
    {
    }

    private DeleteRequest(
        IReadOnlyList<FileSystemPath> sources,
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
        return failure is not null
            ? new FileOperationRequestRejected(failure)
            : new FileOperationRequestAccepted(new DeleteRequest(sources, confirmation));
    }
}
