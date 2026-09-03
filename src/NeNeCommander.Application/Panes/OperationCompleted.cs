using System;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Application.Panes;

/// <summary>Represents the complete typed outcome of the most recent operation.</summary>
public sealed record OperationCompleted : OperationActivity
{
    internal OperationCompleted(FileOperationOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        Outcome = outcome;
    }

    /// <summary>Gets the gateway outcome with its completion, failure, and completed effects.</summary>
    public FileOperationOutcome Outcome { get; }
}
