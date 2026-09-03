using System;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Input;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Panes;

/// <summary>
/// Coordinates the two pane sessions and the sole active side. Only
/// <see cref="UserIntent.ActivateOtherPane"/> changes the active side; every other intent reaches
/// the active pane's session alone, so a read in flight always lands in the pane that started it.
/// </summary>
public sealed class DualPaneSession
{
    private readonly PaneSession _left;
    private readonly PaneSession _right;
    private PaneSide _activeSide;

    /// <summary>Initializes the coordinator over two distinct pane sessions with the left pane active.</summary>
    /// <param name="left">Left pane session.</param>
    /// <param name="right">Right pane session.</param>
    /// <exception cref="ArgumentException">Both sides share one session, which is a composition defect.</exception>
    public DualPaneSession(PaneSession left, PaneSession right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        if (ReferenceEquals(left, right))
        {
            throw new ArgumentException("Each pane side requires its own session.", nameof(right));
        }
        _left = left;
        _right = right;
        _activeSide = PaneSide.Left;
    }

    /// <summary>Gets the current immutable snapshot of both panes.</summary>
    public DualPaneSnapshot Current => new(_left.Current, _right.Current, _activeSide);

    /// <summary>Reads a location into one side regardless of which side is active.</summary>
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
        return ReportAfterAsync(SessionOf(side).NavigateAsync(location, cancellationToken));
    }

    /// <summary>
    /// Applies one intent: activation switches the active side without touching either pane's
    /// focus; every other intent is handled by the active pane's session.
    /// </summary>
    /// <param name="intent">Typed user intent; absence is rejected by the pane session.</param>
    /// <param name="cancellationToken">Token observed by any read the intent starts.</param>
    /// <returns>The resulting snapshot.</returns>
    public Task<DualPaneSnapshot> HandleAsync(UserIntent intent, CancellationToken cancellationToken)
    {
        if (intent == UserIntent.ActivateOtherPane)
        {
            _activeSide = _activeSide.Other;
            return Task.FromResult(Current);
        }
        return ReportAfterAsync(SessionOf(_activeSide).HandleAsync(intent, cancellationToken));
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
