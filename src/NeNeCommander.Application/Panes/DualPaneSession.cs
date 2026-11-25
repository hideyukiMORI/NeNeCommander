using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Panes;

/// <summary>
/// Coordinates the two pane sessions, the sole active side, and file operations between them.
/// <see cref="UserIntent.ActivateOtherPane"/> and admitted qualified pointer input change the active side.
/// Other pane intents normally reach the active pane; the opposite-location intent captures its destination.
/// A read in flight always lands in the pane that
/// started it. <see cref="UserIntent.Move"/>, <see cref="UserIntent.Copy"/>, and <see cref="UserIntent.Delete"/> run through the sole
/// <see cref="FileOperationGateway"/>; every intent is frozen while an operation runs except
/// <see cref="UserIntent.Escape"/>, which cancels the session-owned token the running operation
/// observes, and only <see cref="UserIntent.Confirm"/> or <see cref="UserIntent.Escape"/> leave a
/// pending confirmation. <see cref="UserIntent.CreateDirectory"/> and <see cref="UserIntent.Rename"/>
/// share one <see cref="OperationAwaitingName"/> state that freezes their subject until a
/// <see cref="NameSubmission"/> starts the operation or <see cref="UserIntent.Escape"/> abandons it.
/// </summary>
public sealed class DualPaneSession
{
    private static readonly Action CancelNothingAction = CancelNothing;

    private readonly FileOperationGateway _gateway;
    private readonly PaneSession _left;
    private readonly PaneSession _right;
    private PaneSide _activeSide;
    private OperationActivity _operation;
    private OtherPaneNavigationState _otherPaneNavigation;
    private Action _cancelRunningOperation;

    /// <summary>Initializes the coordinator over two distinct pane sessions with the left pane active.</summary>
    /// <param name="left">Left pane session.</param>
    /// <param name="right">Right pane session.</param>
    /// <param name="gateway">Sole filesystem mutation gateway.</param>
    /// <exception cref="ArgumentException">Both sides share one session, which is a composition defect.</exception>
    public DualPaneSession(PaneSession left, PaneSession right, FileOperationGateway gateway)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ArgumentNullException.ThrowIfNull(gateway);
        if (ReferenceEquals(left, right))
        {
            throw new ArgumentException("Each pane side requires its own session.", nameof(right));
        }
        _left = left;
        _right = right;
        _gateway = gateway;
        _activeSide = PaneSide.Left;
        _operation = OperationActivity.Idle;
        _otherPaneNavigation = OtherPaneNavigationState.Idle;
        _cancelRunningOperation = CancelNothingAction;
    }

    /// <summary>Gets the current immutable snapshot of both panes and the operation activity.</summary>
    public DualPaneSnapshot Current => new(_left.Current, _right.Current, _activeSide, _operation);

    private bool IsFrozen => _operation is OperationRunning or OperationAwaitingConfirmation or OperationAwaitingName or OperationAwaitingConflict;

    /// <summary>Reads a location into one side regardless of which side is active, unless an operation runs or awaits confirmation.</summary>
    /// <param name="side">Pane to read into.</param>
    /// <param name="location">Validated location to read; absence is rejected by the pane session.</param>
    /// <param name="cancellationToken">Token observed by the read.</param>
    /// <returns>The snapshot current after the read completed or was superseded.</returns>
    public Task<DualPaneSnapshot> NavigateAsync(
        PaneSide side,
        FileSystemPath location,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(side);
        return IsFrozen
            ? Task.FromResult(Current)
            : ReportAfterAsync(SessionOf(side).NavigateAsync(location, cancellationToken));
    }

    /// <summary>
    /// Applies one intent: activation switches the active side without touching either pane's
    /// focus; move, copy, and delete start gateway operations; create directory and rename open a
    /// name entry that a name submission turns into a creation or a rename; confirm and escape
    /// resolve a pending confirmation. The opposite-location intent navigates its captured side;
    /// remaining pane intents are handled by the active pane's session.
    /// While an operation runs, escape requests its cancellation and every other intent is frozen.
    /// Otherwise escape reaches the active pane's session, which abandons that pane's read in
    /// flight (ADR-0058). When no active read owns Escape, the exact passive read started by
    /// OpenCurrentLocationInOtherPane may be abandoned through the same route (ADR-0084).
    /// </summary>
    /// <param name="intent">Typed user intent; absence is rejected by the pane session.</param>
    /// <param name="observer">Receives the snapshot each time a started operation reports progress.</param>
    /// <param name="cancellationToken">Token observed by any read or operation the intent starts.</param>
    /// <returns>The resulting snapshot.</returns>
    public Task<DualPaneSnapshot> HandleAsync(
        UserIntent intent,
        IDualPaneProgressObserver observer,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(observer);
        if (_operation is OperationRunning)
        {
            if (intent == UserIntent.Escape)
            {
                _cancelRunningOperation();
            }
            return Task.FromResult(Current);
        }
        if (_operation is OperationAwaitingConfirmation awaiting)
        {
            return ResolveConfirmationAsync(awaiting, intent, observer, cancellationToken);
        }
        if (_operation is OperationAwaitingName awaitingName)
        {
            return ResolveNameAsync(awaitingName, intent, observer, cancellationToken);
        }
        if (_operation is OperationAwaitingConflict awaitingConflict)
        {
            return ResolveConflictAsync(awaitingConflict, intent, observer, cancellationToken);
        }
        if (intent is PaneInteraction interaction)
        {
            return HandleInteractionAsync(interaction, cancellationToken);
        }
        if (intent == UserIntent.ActivateOtherPane)
        {
            _activeSide = _activeSide.Other;
            return Task.FromResult(Current);
        }
        return RouteAsync(intent, observer, cancellationToken);
    }

    /// <summary>Applies one qualified query to the active pane while pane input remains admitted.</summary>
    internal DualPaneSnapshot ApplyNameFilter(PaneSide side, PaneSnapshot expected, PaneNameFilterQuery query)
    {
        if (_activeSide == side && Current.CanInteractWithPanes)
        {
            _ = SessionOf(side).ApplyNameFilter(expected, query);
        }
        return Current;
    }

    private Task<DualPaneSnapshot> HandleInteractionAsync(PaneInteraction interaction, CancellationToken cancellationToken)
    {
        PaneSnapshot before = Current.Of(interaction.Side);
        DualPaneSnapshot after = ApplyInteraction(interaction);
        return !ReferenceEquals(before, after.Of(interaction.Side)) &&
            interaction.NavigationIntent is UserIntent navigation
                ? ReportAfterAsync(SessionOf(interaction.Side).HandleAsync(navigation, cancellationToken))
                : Task.FromResult(after);
    }

    /// <summary>Commits one qualified pointer transition without awaiting any external work.</summary>
    internal DualPaneSnapshot ApplyInteraction(PaneInteraction interaction)
    {
        PaneSession target = SessionOf(interaction.Side);
        if (!Current.CanInteractWithPanes ||
            !interaction.Matches(target.Current))
        {
            return Current;
        }
        _ = target.ApplyInteraction(interaction);
        _activeSide = interaction.Side;
        return Current;
    }

    private Task<DualPaneSnapshot> RouteAsync(
        UserIntent intent,
        IDualPaneProgressObserver observer,
        CancellationToken cancellationToken)
    {
        return intent switch
        {
            _ when intent == UserIntent.OpenCurrentLocationInOtherPane => NavigateOtherPaneAsync(cancellationToken),
            _ when intent == UserIntent.Escape &&
                _otherPaneNavigation is OtherPaneNavigationPending pending && CanAbandonOtherPaneRead(pending) =>
                ReportAfterAsync(SessionOf(pending.Side).HandleAsync(intent, cancellationToken)),
            _ when intent == UserIntent.Move =>
                TransferAsync(OperationKind.Move, MoveRequest.Create, observer, cancellationToken),
            _ when intent == UserIntent.Copy =>
                TransferAsync(OperationKind.Copy, CopyRequest.Create, observer, cancellationToken),
            _ when intent == UserIntent.Delete => DeleteAsync(observer, cancellationToken),
            _ => RouteNamedOperationAsync(intent, cancellationToken),
        };
    }
    private async Task<DualPaneSnapshot> NavigateOtherPaneAsync(CancellationToken cancellationToken)
    {
        DualPaneSnapshot before = Current;
        if (!before.CanOpenCurrentLocationInOtherPane)
        {
            return before;
        }
        PaneContentListed source = (PaneContentListed)before.Of(before.ActiveSide).Content;
        PaneSide destination = before.ActiveSide.Other;
        if (before.Of(destination).Content is PaneContentListed target &&
            FileSystemPathIdentityComparer.Instance.Equals(source.State.Location, target.State.Location))
        {
            return before;
        }
        Task<DualPaneSnapshot> navigation = NavigateAsync(destination, source.State.Location, cancellationToken);
        if (SessionOf(destination).Current.Activity is not PaneLoading loading)
        {
            return await navigation;
        }
        OtherPaneNavigationPending pending = new(destination, loading);
        _otherPaneNavigation = pending;
        try
        {
            return await navigation;
        }
        finally
        {
            if (ReferenceEquals(_otherPaneNavigation, pending))
            {
                _otherPaneNavigation = OtherPaneNavigationState.Idle;
            }
        }
    }

    private bool CanAbandonOtherPaneRead(OtherPaneNavigationPending pending)
    {
        return SessionOf(_activeSide).Current.Activity is not PaneLoading &&
            ReferenceEquals(SessionOf(pending.Side).Current.Activity, pending.Read);
    }
    /// <summary>
    /// Routes the intents that need a typed name into the shared name entry, and every
    /// remaining intent to the active pane's own session.
    /// </summary>
    private Task<DualPaneSnapshot> RouteNamedOperationAsync(UserIntent intent, CancellationToken cancellationToken)
    {
        return intent switch
        {
            _ when intent == UserIntent.CreateDirectory => Task.FromResult(BeginCreationName(OperationKind.CreateDirectory)),
            _ when intent == UserIntent.CreateTextFile => Task.FromResult(BeginCreationName(OperationKind.CreateTextFile)),
            _ when intent == UserIntent.Rename => Task.FromResult(BeginRenameName()),
            ResolvedBookmarkNavigation bookmark => NavigateAsync(_activeSide, bookmark.Path, cancellationToken),
            _ => ReportAfterAsync(SessionOf(_activeSide).HandleAsync(intent, cancellationToken)),
        };
    }

    private Task<DualPaneSnapshot> ResolveNameAsync(
        OperationAwaitingName awaiting,
        UserIntent intent,
        IDualPaneProgressObserver observer,
        CancellationToken cancellationToken)
    {
        if (intent == UserIntent.Escape)
        {
            _operation = OperationActivity.Idle;
            return Task.FromResult(Current);
        }
        return intent is NameSubmission submission
            ? StartAsync(awaiting.Kind, CreateNamedRequest(awaiting, submission.Name), observer, cancellationToken)
            : Task.FromResult(Current);
    }

    private static FileOperationRequestCreation CreateNamedRequest(OperationAwaitingName awaiting, string name)
    {
        return awaiting.Kind == OperationKind.CreateDirectory
            ? CreateDirectoryRequest.Create(awaiting.Subject, name)
            : awaiting.Kind == OperationKind.CreateTextFile
                ? CreateTextFileRequest.Create(awaiting.Subject, name)
                : RenameRequest.Create(awaiting.Subject, name);
    }

    private DualPaneSnapshot BeginCreationName(OperationKind kind)
    {
        if (SessionOf(_activeSide).Current.Content is PaneContentListed active)
        {
            _operation = new OperationAwaitingName(
                kind,
                active.Listing.Location,
                string.Empty);
        }
        return Current;
    }

    private DualPaneSnapshot BeginRenameName()
    {
        if (SessionOf(_activeSide).Current.Content is PaneContentListed active &&
            active.FindFocusedEntry() is DirectoryEntry focused)
        {
            _operation = new OperationAwaitingName(OperationKind.Rename, focused.Path, focused.Name);
        }
        return Current;
    }

    private Task<DualPaneSnapshot> ResolveConfirmationAsync(
        OperationAwaitingConfirmation awaiting,
        UserIntent intent,
        IDualPaneProgressObserver observer,
        CancellationToken cancellationToken)
    {
        if (intent == UserIntent.Escape)
        {
            _operation = OperationActivity.Idle;
            return Task.FromResult(Current);
        }
        if (intent != UserIntent.Confirm)
        {
            return Task.FromResult(Current);
        }
        FileOperationRequestCreation confirmed = DeleteRequest.Create(
            awaiting.Request.Sources,
            PermanentDeletionConfirmation.CreateFor(awaiting.Request));
        return StartAsync(OperationKind.Delete, confirmed, observer, cancellationToken);
    }

    private Task<DualPaneSnapshot> ResolveConflictAsync(
        OperationAwaitingConflict awaiting,
        UserIntent intent,
        IDualPaneProgressObserver observer,
        CancellationToken cancellationToken)
    {
        return intent == UserIntent.Escape
            ? ResumeConflictAsync(
                awaiting,
                TransferConflictResponse.Create(TransferConflictDecision.Cancel, TransferConflictScope.Current),
                observer,
                cancellationToken)
            : intent is ConflictDecisionSubmission submission && ReferenceEquals(submission.ExpectedState, awaiting)
            ? ResumeConflictAsync(
                awaiting,
                TransferConflictResponse.Create(submission.Decision, submission.Scope),
                observer,
                cancellationToken)
            : Task.FromResult(Current);
    }

    private async Task<DualPaneSnapshot> ResumeConflictAsync(
        OperationAwaitingConflict awaiting,
        TransferConflictResponse response,
        IDualPaneProgressObserver observer,
        CancellationToken cancellationToken)
    {
        _operation = new OperationRunning(
            awaiting.Kind,
            FileOperationProgress.Create(0, awaiting.Continuation.Sources.Count));
        FileOperationOutcome outcome = await RunCancellableAsync(
            token => _gateway.ResumeAsync(
                awaiting.Continuation,
                response,
                new ProgressRelay(this, awaiting.Kind, observer),
                token),
            cancellationToken);
        if (outcome.Conflicts is ConflictSet conflicts)
        {
            TransferContinuation continuation = outcome.Continuation!;
            _operation = new OperationAwaitingConflict(awaiting.Kind, conflicts, continuation);
            return Current;
        }
        _operation = new OperationCompleted(awaiting.Kind, outcome);
        await RefreshBothAsync(awaiting.Continuation.Request, outcome, cancellationToken);
        return Current;
    }

    private Task<DualPaneSnapshot> TransferAsync(
        OperationKind kind,
        Func<IReadOnlyList<FileSystemPath>, FileSystemPath, FileOperationRequestCreation> createRequest,
        IDualPaneProgressObserver observer,
        CancellationToken cancellationToken)
    {
        if (SessionOf(_activeSide).Current.Content is not PaneContentListed active ||
            SessionOf(_activeSide.Other).Current.Content is not PaneContentListed passive)
        {
            return Task.FromResult(Current);
        }
        IReadOnlyList<FileSystemPath> sources = SelectSources(active.State);
        return sources.Count == 0
            ? Task.FromResult(Current)
            : StartAsync(kind, createRequest(sources, passive.Listing.Location), observer, cancellationToken);
    }

    private Task<DualPaneSnapshot> DeleteAsync(IDualPaneProgressObserver observer, CancellationToken cancellationToken)
    {
        if (SessionOf(_activeSide).Current.Content is not PaneContentListed active)
        {
            return Task.FromResult(Current);
        }
        IReadOnlyList<FileSystemPath> sources = SelectSources(active.State);
        return sources.Count == 0
            ? Task.FromResult(Current)
            : StartAsync(OperationKind.Delete, DeleteRequest.Create(sources, null), observer, cancellationToken);
    }

    private async Task<DualPaneSnapshot> StartAsync(
        OperationKind kind,
        FileOperationRequestCreation creation,
        IDualPaneProgressObserver observer,
        CancellationToken cancellationToken)
    {
        if (creation is FileOperationRequestRejected rejected)
        {
            _operation = new OperationRequestRejected(kind, rejected.Kind);
            return Current;
        }

        FileOperationRequest request = ((FileOperationRequestAccepted)creation).Request;
        _operation = new OperationRunning(kind, FileOperationProgress.Create(0, request.Sources.Count));
        FileOperationOutcome outcome = await RunCancellableAsync(
            token => _gateway.ExecuteAsync(request, new ProgressRelay(this, kind, observer), token),
            cancellationToken);
        if (request is DeleteRequest unconfirmed && outcome.Failure == FileOperationFailureKind.ConfirmationRequired)
        {
            _operation = new OperationAwaitingConfirmation(unconfirmed);
            return Current;
        }
        if (outcome.Conflicts is ConflictSet conflicts &&
            outcome.Continuation is TransferContinuation continuation)
        {
            _operation = new OperationAwaitingConflict(kind, conflicts, continuation);
            return Current;
        }

        _operation = new OperationCompleted(kind, outcome);
        await RefreshBothAsync(request, outcome, cancellationToken);
        return Current;
    }

    private async Task RefreshBothAsync(
        FileOperationRequest request,
        FileOperationOutcome outcome,
        CancellationToken cancellationToken)
    {
        if (outcome.Completion == FileOperationCompletionKind.Succeeded &&
            SucceededFocus(request) is FileSystemPath focus)
        {
            _ = await SessionOf(_activeSide).RefreshFocusingAsync(focus, cancellationToken);
            _ = await SessionOf(_activeSide.Other).RefreshAsync(cancellationToken);
            return;
        }
        _ = await _left.RefreshAsync(cancellationToken);
        _ = await _right.RefreshAsync(cancellationToken);
    }

    /// <summary>
    /// Names the path the active pane should focus after the request succeeded, or absence when
    /// the operation produced no single new path the user should be moved to.
    /// </summary>
    private static FileSystemPath? SucceededFocus(FileOperationRequest request)
    {
        return request switch
        {
            CreateDirectoryRequest created => created.Target,
            CreateTextFileRequest created => created.Target,
            RenameRequest renamed => renamed.Target,
            _ => null,
        };
    }

    /// <summary>
    /// Runs one gateway attempt under a session-owned cancellation source so Escape can cancel it,
    /// and leaves no running activity behind when the attempt throws.
    /// </summary>
    private async Task<FileOperationOutcome> RunCancellableAsync(
        Func<CancellationToken, Task<FileOperationOutcome>> attempt,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource owned = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        try
        {
            _cancelRunningOperation = owned.Cancel;
            return await attempt(owned.Token);
        }
        catch
        {
            _operation = OperationActivity.Idle;
            throw;
        }
        finally
        {
            _cancelRunningOperation = CancelNothingAction;
        }
    }

    private static void CancelNothing()
    {
    }

    private static IReadOnlyList<FileSystemPath> SelectSources(PaneState state)
    {
        return state.Selection.Count > 0
            ? state.Selection
            : state.FocusItem is FileSystemPath focus ? [focus] : [];
    }

    private async Task<DualPaneSnapshot> ReportAfterAsync(Task<PaneSnapshot> work)
    {
        _ = await work;
        return Current;
    }

    private PaneSession SessionOf(PaneSide side)
    {
        return side == PaneSide.Left ? _left : _right;
    }

    private sealed class ProgressRelay : IFileOperationProgressObserver
    {
        private readonly OperationKind _kind;
        private readonly IDualPaneProgressObserver _observer;
        private readonly DualPaneSession _session;

        internal ProgressRelay(DualPaneSession session, OperationKind kind, IDualPaneProgressObserver observer)
        {
            _session = session;
            _kind = kind;
            _observer = observer;
        }

        public void Report(FileOperationProgress progress)
        {
            _session._operation = new OperationRunning(_kind, progress);
            _observer.OperationProgressed(_session.Current);
        }
    }
}
