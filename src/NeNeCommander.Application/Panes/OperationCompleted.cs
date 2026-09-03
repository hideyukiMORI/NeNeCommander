using System;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Application.Panes;

/// <summary>Represents the complete typed outcome of the most recent operation.</summary>
public sealed record OperationCompleted : OperationActivity
{
    internal OperationCompleted(OperationKind kind, FileOperationOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(kind);
        ArgumentNullException.ThrowIfNull(outcome);
        Kind = kind;
        Outcome = outcome;
    }

    /// <summary>Gets the kind of the completed operation.</summary>
    public OperationKind Kind { get; }

    /// <summary>Gets the gateway outcome with its completion, failure, and completed effects.</summary>
    public FileOperationOutcome Outcome { get; }
}
