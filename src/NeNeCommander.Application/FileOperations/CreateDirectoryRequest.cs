using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Represents a validated request to create one directory directly beneath an existing location.
/// The location is the sole source the gateway inspects; the target is derived by the domain path
/// rules from untrusted name text and never touches the filesystem before execution.
/// </summary>
public sealed record CreateDirectoryRequest : FileOperationRequest
{
    private CreateDirectoryRequest(
        ReadOnlyCollection<FileSystemPath> sources,
        FileSystemPath location,
        FileSystemPath target)
        : base(sources)
    {
        Location = location;
        Target = target;
    }

    /// <summary>Gets the frozen existing location the directory is created in.</summary>
    public FileSystemPath Location { get; }

    /// <summary>Gets the frozen path of the directory to create.</summary>
    public FileSystemPath Target { get; }

    /// <summary>Creates a validated immutable directory-creation request.</summary>
    /// <param name="location">Existing location that receives the directory.</param>
    /// <param name="name">Untrusted single-segment directory name.</param>
    /// <returns>An accepted request or a typed rejection.</returns>
    public static FileOperationRequestCreation Create(FileSystemPath location, string name)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(name);
        if (location.Child(name) is not PathParseSuccess child)
        {
            return new FileOperationRequestRejected(FileOperationRequestFailureKind.InvalidName);
        }
        List<FileSystemPath> ownedSources = [location];
        return new FileOperationRequestAccepted(
            new CreateDirectoryRequest(ownedSources.AsReadOnly(), location, child.Path));
    }
}
