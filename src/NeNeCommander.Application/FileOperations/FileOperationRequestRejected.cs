namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Represents a rejected file operation request.
/// </summary>
public sealed record FileOperationRequestRejected : FileOperationRequestCreation
{
    internal FileOperationRequestRejected(FileOperationRequestFailureKind kind)
    {
        Kind = kind;
    }

    /// <summary>Gets the request rejection reason.</summary>
    public FileOperationRequestFailureKind Kind { get; }
}
