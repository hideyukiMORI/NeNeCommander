using System;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Panes;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Projects the dual-pane snapshot onto two pane presentations, their activation frames, the
/// file-operation status, and the keyboard context.
/// </summary>
public static class DualPanePresenter
{
    /// <summary>Translates both panes, the active side, and the operation activity without changing any state.</summary>
    /// <param name="snapshot">Current dual-pane snapshot.</param>
    /// <returns>A render-ready presentation.</returns>
    public static DualPanePresentation Present(DualPaneSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        PaneFrame leftFrame = snapshot.ActiveSide == PaneSide.Left ? PaneFrame.Active : PaneFrame.Passive;
        PaneFrame rightFrame = snapshot.ActiveSide == PaneSide.Right ? PaneFrame.Active : PaneFrame.Passive;
        OperationAwaitingConfirmation? pending = snapshot.Operation as OperationAwaitingConfirmation;
        return new DualPanePresentation(
            PaneListingPresenter.Present(snapshot.Left),
            leftFrame,
            PaneListingPresenter.Present(snapshot.Right),
            rightFrame,
            snapshot.ActiveSide,
            TranslateOperation(snapshot.Operation),
            pending is null ? 0 : pending.Request.Sources.Count,
            pending is null ? KeyboardContext.FileList : KeyboardContext.Modal);
    }

    private static OperationStatus TranslateOperation(OperationActivity activity)
    {
        return activity switch
        {
            OperationRunning running => TranslateRunning(running.Kind),
            OperationAwaitingConfirmation => OperationStatus.DeleteAwaitingConfirmation,
            OperationRequestRejected rejected => TranslateRequestRejection(rejected.Kind),
            OperationCompleted completed => TranslateCompletion(completed.Kind, completed.Outcome.Completion),
            _ => OperationStatus.Idle,
        };
    }

    private static OperationStatus TranslateRunning(OperationKind kind)
    {
        return kind == OperationKind.Move
            ? OperationStatus.Moving
            : kind == OperationKind.Copy ? OperationStatus.Copying : OperationStatus.Deleting;
    }

    private static OperationStatus TranslateRequestRejection(OperationKind kind)
    {
        return kind == OperationKind.Move
            ? OperationStatus.MoveRequestRejected
            : kind == OperationKind.Copy ? OperationStatus.CopyRequestRejected : OperationStatus.DeleteRequestRejected;
    }

    private static OperationStatus TranslateCompletion(OperationKind kind, FileOperationCompletionKind completion)
    {
        return kind == OperationKind.Move
            ? TranslateMoveCompletion(completion)
            : kind == OperationKind.Copy
                ? TranslateCopyCompletion(completion)
                : TranslateDeleteCompletion(completion);
    }

    private static OperationStatus TranslateCopyCompletion(FileOperationCompletionKind completion)
    {
        return completion == FileOperationCompletionKind.Succeeded
            ? OperationStatus.CopySucceeded
            : completion == FileOperationCompletionKind.Cancelled
                ? OperationStatus.CopyCancelled
                : completion == FileOperationCompletionKind.PartiallyCompleted
                    ? OperationStatus.CopyPartiallyCompleted
                    : OperationStatus.CopyRejected;
    }

    private static OperationStatus TranslateMoveCompletion(FileOperationCompletionKind completion)
    {
        return completion == FileOperationCompletionKind.Succeeded
            ? OperationStatus.MoveSucceeded
            : completion == FileOperationCompletionKind.Cancelled
                ? OperationStatus.MoveCancelled
                : completion == FileOperationCompletionKind.PartiallyCompleted
                    ? OperationStatus.MovePartiallyCompleted
                    : OperationStatus.MoveRejected;
    }

    private static OperationStatus TranslateDeleteCompletion(FileOperationCompletionKind completion)
    {
        return completion == FileOperationCompletionKind.Succeeded
            ? OperationStatus.DeleteSucceeded
            : completion == FileOperationCompletionKind.Cancelled
                ? OperationStatus.DeleteCancelled
                : completion == FileOperationCompletionKind.PartiallyCompleted
                    ? OperationStatus.DeletePartiallyCompleted
                    : OperationStatus.DeleteRejected;
    }
}
