using System;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Application.Panes;

/// <summary>Represents an operation that never reached the gateway because its request was invalid.</summary>
public sealed record OperationRequestRejected : OperationActivity
{
    internal OperationRequestRejected(FileOperationRequestFailureKind kind)
    {
        ArgumentNullException.ThrowIfNull(kind);
        Kind = kind;
    }

    /// <summary>Gets the closed request rejection.</summary>
    public FileOperationRequestFailureKind Kind { get; }
}
