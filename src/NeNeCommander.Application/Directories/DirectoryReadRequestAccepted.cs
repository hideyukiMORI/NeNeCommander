namespace NeNeCommander.Application.Directories;

/// <summary>
/// Represents an accepted directory read request.
/// </summary>
public sealed record DirectoryReadRequestAccepted : DirectoryReadRequestCreation
{
    internal DirectoryReadRequestAccepted(DirectoryReadRequest request)
    {
        Request = request;
    }

    /// <summary>Gets the validated request.</summary>
    public DirectoryReadRequest Request { get; }
}
