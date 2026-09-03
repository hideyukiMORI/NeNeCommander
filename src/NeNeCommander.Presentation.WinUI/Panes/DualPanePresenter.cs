using System;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Projects the dual-pane snapshot onto two pane presentations, their activation frames, and the
/// file-operation status.
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
        return new DualPanePresentation(
            PaneListingPresenter.Present(snapshot.Left),
            leftFrame,
            PaneListingPresenter.Present(snapshot.Right),
            rightFrame,
            snapshot.ActiveSide,
            TranslateOperation(snapshot.Operation));
    }

    private static OperationStatus TranslateOperation(OperationActivity activity)
    {
        return activity switch
        {
            OperationRunning => OperationStatus.Moving,
            OperationRequestRejected => OperationStatus.MoveRequestRejected,
            OperationCompleted completed => TranslateCompletion(completed.Outcome.Completion),
            _ => OperationStatus.Idle,
        };
    }

    private static OperationStatus TranslateCompletion(FileOperationCompletionKind completion)
    {
        return completion == FileOperationCompletionKind.Succeeded
            ? OperationStatus.MoveSucceeded
            : completion == FileOperationCompletionKind.Cancelled
                ? OperationStatus.MoveCancelled
                : completion == FileOperationCompletionKind.PartiallyCompleted
                    ? OperationStatus.MovePartiallyCompleted
                    : OperationStatus.MoveRejected;
    }
}
