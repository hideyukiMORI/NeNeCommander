using System;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Application.Panes;

/// <summary>Represents an operation that never reached the gateway because its request was invalid.</summary>
public sealed record OperationRequestRejected : OperationActivity
{
    internal OperationRequestRejected(OperationKind kind, FileOperationRequestFailureKind failure)
    {
        ArgumentNullException.ThrowIfNull(kind);
        ArgumentNullException.ThrowIfNull(failure);
        Kind = kind;
        Failure = failure;
    }

    /// <summary>Gets the kind of the rejected operation.</summary>
    public OperationKind Kind { get; }

    /// <summary>Gets the closed request rejection.</summary>
    public FileOperationRequestFailureKind Failure { get; }
}
