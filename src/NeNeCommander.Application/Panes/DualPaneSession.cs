using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Panes;

/// <summary>
/// Coordinates the two pane sessions, the sole active side, and file operations between them.
/// Only <see cref="UserIntent.ActivateOtherPane"/> changes the active side; every other pane intent
/// reaches the active pane's session alone, so a read in flight always lands in the pane that
/// started it. <see cref="UserIntent.Move"/>, <see cref="UserIntent.Copy"/>, and <see cref="UserIntent.Delete"/> run through the sole
/// <see cref="FileOperationGateway"/>; every intent is frozen while an operation runs except
/// <see cref="UserIntent.Escape"/>, which cancels the session-owned token the running operation
/// observes, and only <see cref="UserIntent.Confirm"/> or <see cref="UserIntent.Escape"/> leave a
/// pending confirmation.
/// </summary>
public sealed class DualPaneSession
{
    private readonly FileOperationGateway _gateway;
    private readonly PaneSession _left;
    private readonly PaneSession _right;
    private PaneSide _activeSide;
    private OperationActivity _operation;
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
        _cancelRunningOperation = CancelNothing;
    }

    /// <summary>Gets the current immutable snapshot of both panes and the operation activity.</summary>
    public DualPaneSnapshot Current => new(_left.Current, _right.Current, _activeSide, _operation);

    private bool IsFrozen => _operation is OperationRunning or OperationAwaitingConfirmation;

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
    /// focus; move, copy, and delete start gateway operations; confirm and escape resolve a pending
    /// confirmation; every other intent is handled by the active pane's session. While an operation
    /// runs, escape requests its cancellation and every other intent is frozen.
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
        if (intent == UserIntent.ActivateOtherPane)
        {
            _activeSide = _activeSide.Other;
            return Task.FromResult(Current);
        }
        return intent == UserIntent.Move
            ? TransferAsync(OperationKind.Move, MoveRequest.Create, observer, cancellationToken)
            : intent == UserIntent.Copy
                ? TransferAsync(OperationKind.Copy, CopyRequest.Create, observer, cancellationToken)
                : intent == UserIntent.Delete
                    ? DeleteAsync(observer, cancellationToken)
                    : ReportAfterAsync(SessionOf(_activeSide).HandleAsync(intent, cancellationToken));
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
        FileOperationOutcome outcome;
        using (CancellationTokenSource owned = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            _cancelRunningOperation = owned.Cancel;
            outcome = await _gateway.ExecuteAsync(request, new ProgressRelay(this, kind, observer), owned.Token);
        }
        if (request is DeleteRequest unconfirmed && outcome.Failure == FileOperationFailureKind.ConfirmationRequired)
        {
            _operation = new OperationAwaitingConfirmation(unconfirmed);
            return Current;
        }

        _operation = new OperationCompleted(kind, outcome);
        _ = await _left.RefreshAsync(cancellationToken);
        _ = await _right.RefreshAsync(cancellationToken);
        return Current;
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
