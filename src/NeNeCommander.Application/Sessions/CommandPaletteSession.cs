using System;
using System.Linq;
using System.Threading;
using NeNeCommander.Application.Commands;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Sessions;

/// <summary>
/// Owns the command palette transient scope: its closed state under one lock, its admission rule,
/// and the expected-state validation of one qualified submission. It knows no other owner, holds
/// no freeze predicate, performs no pane effect, and dispatches nothing.
/// </summary>
public sealed class CommandPaletteSession
{
    private readonly Lock _sync = new();
    private CommandPaletteState _state = CommandPaletteState.Closed;

    /// <summary>Gets the current immutable command palette state.</summary>
    public CommandPaletteState Current
    {
        get
        {
            lock (_sync)
            {
                return _state;
            }
        }
    }

    /// <summary>Opens the palette over both captured endpoints when this scope may own input.</summary>
    /// <param name="panes">Pane snapshot captured by the scope and compared on every later decision.</param>
    /// <param name="ownership">Input ownership the session derived for this scope.</param>
    /// <returns>The state current after this admission decision.</returns>
    public CommandPaletteState Open(DualPaneSnapshot panes, InteractionOwnership ownership)
    {
        ArgumentNullException.ThrowIfNull(panes);
        ArgumentNullException.ThrowIfNull(ownership);
        lock (_sync)
        {
            if (ownership == InteractionOwnership.ScopeOwnsInput &&
                panes.Of(panes.ActiveSide).Content is PaneContentListed)
            {
                _state = new CommandPaletteOpen(
                    panes.Left,
                    panes.Right,
                    panes.ActiveSide,
                    CommandCatalog.Capture(panes));
            }
            return _state;
        }
    }

    /// <summary>Validates one expected-state-qualified palette intent against the current state.</summary>
    /// <param name="intent">Intent offered while this scope owns precedence.</param>
    /// <param name="panes">Pane snapshot current at validation time.</param>
    /// <param name="ownership">Input ownership the session derived for this scope.</param>
    /// <returns>The catalog intent the session must dispatch, or nothing to dispatch.</returns>
    public CommandPaletteValidation Validate(
        UserIntent intent,
        DualPaneSnapshot panes,
        InteractionOwnership ownership)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(panes);
        ArgumentNullException.ThrowIfNull(ownership);
        lock (_sync)
        {
            if (intent is CommandPaletteCancellation cancellation)
            {
                Cancel(cancellation.ExpectedState, panes, ownership);
                return CommandPaletteValidation.NothingToDispatch;
            }
            return intent is CommandPaletteSubmission submission
                ? Submit(submission, panes, ownership)
                : CommandPaletteValidation.NothingToDispatch;
        }
    }

    private void Cancel(
        CommandPaletteOpen expected,
        DualPaneSnapshot panes,
        InteractionOwnership ownership)
    {
        if (!ReferenceEquals(_state, expected))
        {
            return;
        }
        _state = RetainsInput(expected, panes, ownership)
            ? CommandPaletteState.CloseFocusing(expected.ActiveSide)
            : CommandPaletteState.Closed;
    }

    private CommandPaletteValidation Submit(
        CommandPaletteSubmission submission,
        DualPaneSnapshot panes,
        InteractionOwnership ownership)
    {
        CommandPaletteOpen expected = submission.ExpectedState;
        if (!ReferenceEquals(_state, expected) || !CommandCatalog.Contains(submission.SelectedIntent))
        {
            return CommandPaletteValidation.NothingToDispatch;
        }
        if (!RetainsInput(expected, panes, ownership))
        {
            _state = CommandPaletteState.Closed;
            return CommandPaletteValidation.NothingToDispatch;
        }
        CommandCandidate candidate = expected.Candidates.First(candidate =>
            candidate.Intent == submission.SelectedIntent);
        if (candidate.Availability != CommandAvailability.Available)
        {
            return CommandPaletteValidation.NothingToDispatch;
        }
        _state = CommandPaletteState.Closed;
        return new CommandPaletteIntentAccepted(submission.SelectedIntent);
    }

    private static bool RetainsInput(
        CommandPaletteOpen expected,
        DualPaneSnapshot panes,
        InteractionOwnership ownership)
    {
        return ownership == InteractionOwnership.ScopeOwnsInput &&
            panes.ActiveSide == expected.ActiveSide &&
            ReferenceEquals(panes.Left, expected.Left) &&
            ReferenceEquals(panes.Right, expected.Right);
    }
}
