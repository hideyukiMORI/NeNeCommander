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
/// started it. <see cref="UserIntent.Move"/> runs through the sole <see cref="FileOperationGateway"/>,
/// and every intent is frozen while an operation runs.
/// </summary>
public sealed class DualPaneSession
{
    private readonly FileOperationGateway _gateway;
    private readonly PaneSession _left;
    private readonly PaneSession _right;
    private PaneSide _activeSide;
    private OperationActivity _operation;

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
    }

    /// <summary>Gets the current immutable snapshot of both panes and the operation activity.</summary>
    public DualPaneSnapshot Current => new(_left.Current, _right.Current, _activeSide, _operation);

    /// <summary>Reads a location into one side regardless of which side is active, unless an operation runs.</summary>
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
        return _operation is OperationRunning
            ? Task.FromResult(Current)
            : ReportAfterAsync(SessionOf(side).NavigateAsync(location, cancellationToken));
    }

    /// <summary>
    /// Applies one intent: activation switches the active side without touching either pane's
    /// focus; move starts the gateway operation; every other intent is handled by the active
    /// pane's session. Every intent is frozen while an operation runs.
    /// </summary>
    /// <param name="intent">Typed user intent; absence is rejected by the pane session.</param>
    /// <param name="cancellationToken">Token observed by any read or operation the intent starts.</param>
    /// <returns>The resulting snapshot.</returns>
    public Task<DualPaneSnapshot> HandleAsync(UserIntent intent, CancellationToken cancellationToken)
    {
        if (_operation is OperationRunning)
        {
            return Task.FromResult(Current);
        }
        if (intent == UserIntent.ActivateOtherPane)
        {
            _activeSide = _activeSide.Other;
            return Task.FromResult(Current);
        }
        return intent == UserIntent.Move
            ? MoveAsync(cancellationToken)
            : ReportAfterAsync(SessionOf(_activeSide).HandleAsync(intent, cancellationToken));
    }

    private async Task<DualPaneSnapshot> MoveAsync(CancellationToken cancellationToken)
    {
        if (SessionOf(_activeSide).Current.Content is not PaneContentListed active ||
            SessionOf(_activeSide.Other).Current.Content is not PaneContentListed passive)
        {
            return Current;
        }
        IReadOnlyList<FileSystemPath> sources = SelectSources(active.State);
        if (sources.Count == 0)
        {
            return Current;
        }

        FileOperationRequestCreation creation = MoveRequest.Create(sources, passive.Listing.Location);
        if (creation is FileOperationRequestRejected rejected)
        {
            _operation = new OperationRequestRejected(rejected.Kind);
            return Current;
        }

        _operation = new OperationRunning();
        FileOperationOutcome outcome = await _gateway.ExecuteAsync(
            ((FileOperationRequestAccepted)creation).Request,
            cancellationToken);
        _operation = new OperationCompleted(outcome);
        _ = await _left.RefreshAsync(cancellationToken);
        _ = await _right.RefreshAsync(cancellationToken);
        return Current;
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
}
