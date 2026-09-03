namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Identifies the closed status shown for the file-operation activity. Each status names a
/// localization resource; no user-facing text is assembled in code.
/// </summary>
public sealed record OperationStatus
{
    /// <summary>Gets the status when no operation is running and none has been reported.</summary>
    public static OperationStatus Idle { get; } = new("OperationStatusIdle");

    /// <summary>Gets the status while a move runs.</summary>
    public static OperationStatus Moving { get; } = new("OperationStatusMoving");

    /// <summary>Gets the status when every requested effect completed.</summary>
    public static OperationStatus MoveSucceeded { get; } = new("OperationStatusMoveSucceeded");

    /// <summary>Gets the status when cancellation stopped new work.</summary>
    public static OperationStatus MoveCancelled { get; } = new("OperationStatusMoveCancelled");

    /// <summary>Gets the status when side effects completed before a failure stopped the batch.</summary>
    public static OperationStatus MovePartiallyCompleted { get; } = new("OperationStatusMovePartiallyCompleted");

    /// <summary>Gets the status when the gateway rejected the operation before any side effect.</summary>
    public static OperationStatus MoveRejected { get; } = new("OperationStatusMoveRejected");

    /// <summary>Gets the status when the request itself was invalid and never reached the gateway.</summary>
    public static OperationStatus MoveRequestRejected { get; } = new("OperationStatusMoveRequestRejected");

    private OperationStatus(string resourceKey)
    {
        ResourceKey = resourceKey;
    }

    /// <summary>Gets the localization resource key that names this status.</summary>
    public string ResourceKey { get; }
}
