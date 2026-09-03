using System;

namespace NeNeCommander.Application.Panes;

/// <summary>Represents an operation running through the gateway; every pane intent is frozen meanwhile.</summary>
public sealed record OperationRunning : OperationActivity
{
    internal OperationRunning(OperationKind kind)
    {
        ArgumentNullException.ThrowIfNull(kind);
        Kind = kind;
    }

    /// <summary>Gets the kind of the running operation.</summary>
    public OperationKind Kind { get; }
}
