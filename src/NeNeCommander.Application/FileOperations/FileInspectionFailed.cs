using System;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Represents one expected provider inspection failure.</summary>
public sealed record FileInspectionFailed : FileInspectionOutcome
{
    internal FileInspectionFailed(FileOperationFailureKind failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        Failure = failure;
    }

    /// <summary>Gets the normalized failure that prevented a snapshot.</summary>
    public FileOperationFailureKind Failure { get; }
}
