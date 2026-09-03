using System;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Application.Panes;

/// <summary>Represents an operation running through the gateway; every pane intent except escape is frozen meanwhile.</summary>
public sealed record OperationRunning : OperationActivity
{
    internal OperationRunning(OperationKind kind, FileOperationProgress progress)
    {
        ArgumentNullException.ThrowIfNull(kind);
        ArgumentNullException.ThrowIfNull(progress);
        Kind = kind;
        Progress = progress;
    }

    /// <summary>Gets the kind of the running operation.</summary>
    public OperationKind Kind { get; }

    /// <summary>Gets how many of the request's sources completed so far.</summary>
    public FileOperationProgress Progress { get; }
}
