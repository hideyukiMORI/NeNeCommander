namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Represents an accepted immutable file operation request.
/// </summary>
public sealed record FileOperationRequestAccepted : FileOperationRequestCreation
{
    private protected override void RestrictDerivation()
    {
    }

    internal FileOperationRequestAccepted(FileOperationRequest request)
    {
        Request = request;
    }

    /// <summary>Gets the validated operation request.</summary>
    public FileOperationRequest Request { get; }
}
