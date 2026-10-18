using System;
using System.Linq;
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
    /// <param name="format">Metadata format the host resolved once for both panes' rows.</param>
    /// <returns>A render-ready presentation.</returns>
    public static DualPanePresentation Present(DualPaneSnapshot snapshot, EntryMetadataFormat format)
    {
        return Present(snapshot, format, null);
    }

    internal static DualPanePresentation Present(
        DualPaneSnapshot snapshot,
        EntryMetadataFormat format,
        DualPanePresentation? previous)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(format);
        PaneFrame leftFrame = snapshot.ActiveSide == PaneSide.Left ? PaneFrame.Active : PaneFrame.Passive;
        PaneFrame rightFrame = snapshot.ActiveSide == PaneSide.Right ? PaneFrame.Active : PaneFrame.Passive;
        KeyboardContext inputContext = snapshot.Operation is OperationAwaitingConfirmation or OperationAwaitingName or OperationAwaitingConflict
            ? KeyboardContext.Modal
            : KeyboardContext.FileList;
        return new DualPanePresentation(
            PanePairPresentation.Create(
                PresentPane(snapshot.Left, leftFrame, format, previous?.Left),
                PresentPane(snapshot.Right, rightFrame, format, previous?.Right),
                snapshot.ActiveSide),
            OperationBarPresentation.Create(
                TranslateOperation(snapshot.Operation),
                TranslateDetail(snapshot.Operation),
                TranslateTone(snapshot.Operation),
                KeyHintPresenter.Present(inputContext)),
            OperationInteractionPresentation.Create(
                TranslateNameEntry(snapshot.Operation),
                TranslateConflictModal(snapshot.Operation, previous?.ConflictModal),
                inputContext));
    }

    private static PanePresentation PresentPane(
        PaneSnapshot snapshot,
        PaneFrame frame,
        EntryMetadataFormat format,
        PanePresentation? previous)
    {
        return previous is null
            ? PaneListingPresenter.Present(snapshot, frame, format)
            : PaneListingPresenter.Present(snapshot, frame, format, previous);
    }

    /// <summary>
    /// Translates the operation activity into the closed tone of the operation bar. A running,
    /// succeeded, or cancelled operation reports nothing that needs attention and keeps the idle
    /// tone; only a pending question or an incomplete result changes it.
    /// </summary>
    private static OperationBarTone TranslateTone(OperationActivity activity)
    {
        return activity switch
        {
            OperationAwaitingName => OperationBarTone.AwaitingName,
            OperationAwaitingConfirmation => OperationBarTone.AwaitingConfirmation,
            OperationAwaitingConflict => OperationBarTone.AwaitingConfirmation,
            OperationRequestRejected => OperationBarTone.Failure,
            OperationCompleted completed => TranslateCompletionTone(completed.Outcome.Completion),
            _ => OperationBarTone.Idle,
        };
    }

    private static OperationBarTone TranslateCompletionTone(FileOperationCompletionKind completion)
    {
        return completion == FileOperationCompletionKind.PartiallyCompleted ||
            completion == FileOperationCompletionKind.Rejected
            ? OperationBarTone.Failure
            : OperationBarTone.Idle;
    }

    private static NameEntryPresentation TranslateNameEntry(OperationActivity activity)
    {
        return activity is OperationAwaitingName awaiting
            ? new ActiveNameEntry(awaiting.InitialName)
            : NameEntryPresentation.Hidden;
    }

    private static OperationDetail TranslateDetail(OperationActivity activity)
    {
        return activity switch
        {
            OperationAwaitingConfirmation pending => new OperationItemCountDetail(pending.Request.Sources.Count),
            OperationRunning running => new OperationProgressDetail(running.Progress),
            OperationCompleted completed when completed.Kind == OperationKind.Copy || completed.Kind == OperationKind.Move =>
                TranslateTransferResult(completed.Outcome),
            _ => OperationDetail.None,
        };
    }

    private static OperationStatus TranslateOperation(OperationActivity activity)
    {
        return activity switch
        {
            OperationRunning running => TranslateRunning(running.Kind),
            OperationAwaitingConfirmation => OperationStatus.DeleteAwaitingConfirmation,
            OperationAwaitingConflict awaitingConflict => awaitingConflict.Kind == OperationKind.Copy
                ? OperationStatus.CopyAwaitingConflict
                : OperationStatus.MoveAwaitingConflict,
            OperationAwaitingName awaiting => TranslateAwaitingName(awaiting.Kind),
            OperationRequestRejected rejected => TranslateRequestRejection(rejected.Kind),
            OperationCompleted completed => TranslateCompletion(completed.Kind, completed.Outcome.Completion),
            _ => OperationStatus.Idle,
        };
    }

    private static ConflictModalPresentation TranslateConflictModal(
        OperationActivity activity,
        ConflictModalPresentation? previous)
    {
        return activity is not OperationAwaitingConflict awaiting
            ? ConflictModalPresentation.Hidden
            : previous is ActiveConflictModal active &&
                ReferenceEquals(active.Continuation, awaiting.Continuation)
                ? active
                : new ActiveConflictModal(
                    awaiting.Conflicts,
                    awaiting.InitialFocus,
                    awaiting.Continuation);
    }

    private static TransferResultDetail TranslateTransferResult(FileOperationOutcome outcome)
    {
        return new TransferResultDetail(
            outcome.NotTransferred.Count,
            outcome.Effects.Count(effect => effect.Kind == FileOperationEffectKind.Copied),
            outcome.Effects.Count(effect => effect.Kind == FileOperationEffectKind.Verified),
            SourceDeletionCounts.Create(
                outcome.Effects.Count(effect => effect.Kind == FileOperationEffectKind.SourceDeleted),
                outcome.Effects.Count(effect => effect.Kind == FileOperationEffectKind.SourcePartiallyDeleted)));
    }

    private static OperationStatus TranslateAwaitingName(OperationKind kind)
    {
        return kind == OperationKind.CreateDirectory
            ? OperationStatus.CreateDirectoryAwaitingName
            : OperationStatus.RenameAwaitingName;
    }

    private static OperationStatus TranslateRunning(OperationKind kind)
    {
        return kind switch
        {
            OperationKind value when value == OperationKind.Move => OperationStatus.Moving,
            OperationKind value when value == OperationKind.Copy => OperationStatus.Copying,
            OperationKind value when value == OperationKind.Delete => OperationStatus.Deleting,
            OperationKind value when value == OperationKind.CreateDirectory => OperationStatus.CreatingDirectory,
            _ => OperationStatus.Renaming,
        };
    }

    private static OperationStatus TranslateRequestRejection(OperationKind kind)
    {
        return kind switch
        {
            OperationKind value when value == OperationKind.Move => OperationStatus.MoveRequestRejected,
            OperationKind value when value == OperationKind.Copy => OperationStatus.CopyRequestRejected,
            OperationKind value when value == OperationKind.Delete => OperationStatus.DeleteRequestRejected,
            OperationKind value when value == OperationKind.CreateDirectory => OperationStatus.CreateDirectoryRequestRejected,
            _ => OperationStatus.RenameRequestRejected,
        };
    }

    private static OperationStatus TranslateCompletion(OperationKind kind, FileOperationCompletionKind completion)
    {
        return kind switch
        {
            OperationKind value when value == OperationKind.Move => TranslateMoveCompletion(completion),
            OperationKind value when value == OperationKind.Copy => TranslateCopyCompletion(completion),
            OperationKind value when value == OperationKind.Delete => TranslateDeleteCompletion(completion),
            OperationKind value when value == OperationKind.CreateDirectory => TranslateCreateDirectoryCompletion(completion),
            _ => TranslateRenameCompletion(completion),
        };
    }

    private static OperationStatus TranslateCreateDirectoryCompletion(FileOperationCompletionKind completion)
    {
        return completion == FileOperationCompletionKind.Succeeded
            ? OperationStatus.DirectoryCreated
            : completion == FileOperationCompletionKind.Cancelled
                ? OperationStatus.CreateDirectoryCancelled
                : OperationStatus.CreateDirectoryRejected;
    }

    private static OperationStatus TranslateRenameCompletion(FileOperationCompletionKind completion)
    {
        return completion == FileOperationCompletionKind.Succeeded
            ? OperationStatus.Renamed
            : completion == FileOperationCompletionKind.Cancelled
                ? OperationStatus.RenameCancelled
                : OperationStatus.RenameRejected;
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
