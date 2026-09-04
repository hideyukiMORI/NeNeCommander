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

    /// <summary>Gets the status when every requested move effect completed.</summary>
    public static OperationStatus MoveSucceeded { get; } = new("OperationStatusMoveSucceeded");

    /// <summary>Gets the status when cancellation stopped new move work.</summary>
    public static OperationStatus MoveCancelled { get; } = new("OperationStatusMoveCancelled");

    /// <summary>Gets the status when move side effects completed before a failure stopped the batch.</summary>
    public static OperationStatus MovePartiallyCompleted { get; } = new("OperationStatusMovePartiallyCompleted");

    /// <summary>Gets the status when the gateway rejected the move before any side effect.</summary>
    public static OperationStatus MoveRejected { get; } = new("OperationStatusMoveRejected");

    /// <summary>Gets the status when the move request itself was invalid and never reached the gateway.</summary>
    public static OperationStatus MoveRequestRejected { get; } = new("OperationStatusMoveRequestRejected");

    /// <summary>Gets the status while a copy runs.</summary>
    public static OperationStatus Copying { get; } = new("OperationStatusCopying");

    /// <summary>Gets the status when every requested copy effect completed.</summary>
    public static OperationStatus CopySucceeded { get; } = new("OperationStatusCopySucceeded");

    /// <summary>Gets the status when cancellation stopped new copy work.</summary>
    public static OperationStatus CopyCancelled { get; } = new("OperationStatusCopyCancelled");

    /// <summary>Gets the status when copy side effects completed before a failure stopped the batch.</summary>
    public static OperationStatus CopyPartiallyCompleted { get; } = new("OperationStatusCopyPartiallyCompleted");

    /// <summary>Gets the status when the gateway rejected the copy before any side effect.</summary>
    public static OperationStatus CopyRejected { get; } = new("OperationStatusCopyRejected");

    /// <summary>Gets the status when the copy request itself was invalid and never reached the gateway.</summary>
    public static OperationStatus CopyRequestRejected { get; } = new("OperationStatusCopyRequestRejected");

    /// <summary>Gets the status while a directory is being created.</summary>
    public static OperationStatus CreatingDirectory { get; } = new("OperationStatusCreatingDirectory");

    /// <summary>Gets the status while the session waits for the new directory's name.</summary>
    public static OperationStatus CreateDirectoryAwaitingName { get; } = new("OperationStatusCreateDirectoryAwaitingName");

    /// <summary>Gets the status when the directory was created.</summary>
    public static OperationStatus DirectoryCreated { get; } = new("OperationStatusDirectoryCreated");

    /// <summary>Gets the status when cancellation stopped the directory creation.</summary>
    public static OperationStatus CreateDirectoryCancelled { get; } = new("OperationStatusCreateDirectoryCancelled");

    /// <summary>Gets the status when the gateway rejected the directory creation before any side effect.</summary>
    public static OperationStatus CreateDirectoryRejected { get; } = new("OperationStatusCreateDirectoryRejected");

    /// <summary>Gets the status when the directory name was invalid and never reached the gateway.</summary>
    public static OperationStatus CreateDirectoryRequestRejected { get; } = new("OperationStatusCreateDirectoryRequestRejected");

    /// <summary>Gets the status while a rename runs.</summary>
    public static OperationStatus Renaming { get; } = new("OperationStatusRenaming");

    /// <summary>Gets the status while the session waits for the focus item's new name.</summary>
    public static OperationStatus RenameAwaitingName { get; } = new("OperationStatusRenameAwaitingName");

    /// <summary>Gets the status when the entry was renamed.</summary>
    public static OperationStatus Renamed { get; } = new("OperationStatusRenamed");

    /// <summary>Gets the status when cancellation stopped the rename.</summary>
    public static OperationStatus RenameCancelled { get; } = new("OperationStatusRenameCancelled");

    /// <summary>Gets the status when the gateway rejected the rename before any change.</summary>
    public static OperationStatus RenameRejected { get; } = new("OperationStatusRenameRejected");

    /// <summary>Gets the status when the new name was invalid and never reached the gateway.</summary>
    public static OperationStatus RenameRequestRejected { get; } = new("OperationStatusRenameRequestRejected");

    /// <summary>Gets the status while a deletion runs.</summary>
    public static OperationStatus Deleting { get; } = new("OperationStatusDeleting");

    /// <summary>Gets the status while a permanent deletion waits for explicit confirmation.</summary>
    public static OperationStatus DeleteAwaitingConfirmation { get; } = new("OperationStatusDeleteAwaitingConfirmation");

    /// <summary>Gets the status when every requested deletion effect completed.</summary>
    public static OperationStatus DeleteSucceeded { get; } = new("OperationStatusDeleteSucceeded");

    /// <summary>Gets the status when cancellation stopped new deletion work.</summary>
    public static OperationStatus DeleteCancelled { get; } = new("OperationStatusDeleteCancelled");

    /// <summary>Gets the status when deletion side effects completed before a failure stopped the batch.</summary>
    public static OperationStatus DeletePartiallyCompleted { get; } = new("OperationStatusDeletePartiallyCompleted");

    /// <summary>Gets the status when the gateway rejected the deletion before any side effect.</summary>
    public static OperationStatus DeleteRejected { get; } = new("OperationStatusDeleteRejected");

    /// <summary>Gets the status when the delete request itself was invalid and never reached the gateway.</summary>
    public static OperationStatus DeleteRequestRejected { get; } = new("OperationStatusDeleteRequestRejected");

    private OperationStatus(string resourceKey)
    {
        ResourceKey = resourceKey;
    }

    /// <summary>Gets the localization resource key that names this status.</summary>
    public string ResourceKey { get; }
}
