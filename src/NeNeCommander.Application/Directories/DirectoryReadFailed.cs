using System;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Application.Directories;

/// <summary>Represents one expected directory read failure normalized by its adapter.</summary>
public sealed record DirectoryReadFailed : DirectoryReadOutcome
{
    internal DirectoryReadFailed(FileOperationFailureKind failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        Failure = failure;
    }

    /// <summary>Gets the normalized failure that prevented a listing.</summary>
    public FileOperationFailureKind Failure { get; }
}
