using System;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Panes;

/// <summary>Represents a read that failed with a normalized reason and left the content unchanged.</summary>
public sealed record PaneReadFailed : PaneActivity
{
    internal PaneReadFailed(FileSystemPath target, FileOperationFailureKind failure)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(failure);
        Target = target;
        Failure = failure;
    }

    /// <summary>Gets the location whose read failed.</summary>
    public FileSystemPath Target { get; }

    /// <summary>Gets the normalized failure.</summary>
    public FileOperationFailureKind Failure { get; }
}
