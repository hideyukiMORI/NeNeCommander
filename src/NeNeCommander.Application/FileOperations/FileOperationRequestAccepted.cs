namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Represents an accepted immutable file operation request.
/// </summary>
public sealed record FileOperationRequestAccepted : FileOperationRequestCreation
{
    internal FileOperationRequestAccepted(FileOperationRequest request)
    {
        Request = request;
    }

    /// <summary>Gets the validated operation request.</summary>
    public FileOperationRequest Request { get; }
}
