using System;
using System.Collections.Generic;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Represents a validated request to create one empty text file directly beneath an existing location.
/// The location is the sole source the gateway inspects; the target is derived by the domain path
/// rules from untrusted name text and never touches the filesystem before execution.
/// </summary>
public sealed record CreateTextFileRequest : FileOperationRequest
{
    private protected override void RestrictDerivation()
    {
    }

    private CreateTextFileRequest(
        IReadOnlyList<FileSystemPath> sources,
        FileSystemPath location,
        FileSystemPath target)
        : base(sources)
    {
        Location = location;
        Target = target;
    }

    /// <summary>Gets the frozen existing location the file is created in.</summary>
    public FileSystemPath Location { get; }

    /// <summary>Gets the frozen path of the empty file to create.</summary>
    public FileSystemPath Target { get; }

    /// <summary>Creates a validated immutable empty-file request.</summary>
    /// <param name="location">Existing location that receives the file.</param>
    /// <param name="name">Untrusted single-segment filename.</param>
    /// <returns>An accepted request or a typed rejection.</returns>
    public static FileOperationRequestCreation Create(FileSystemPath location, string name)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(name);
        return location.Child(name) is PathParseSuccess child
            ? new FileOperationRequestAccepted(new CreateTextFileRequest([location], location, child.Path))
            : new FileOperationRequestRejected(FileOperationRequestFailureKind.InvalidName);
    }
}
