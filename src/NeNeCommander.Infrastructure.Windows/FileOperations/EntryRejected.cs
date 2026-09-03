using System;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Represents a snapshot that no longer describes an entry the adapter may act on.</summary>
public sealed record EntryRejected : RevalidationOutcome
{
    internal EntryRejected(FileOperationFailureKind failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        Failure = failure;
    }

    /// <summary>Gets the closed failure.</summary>
    public FileOperationFailureKind Failure { get; }
}
