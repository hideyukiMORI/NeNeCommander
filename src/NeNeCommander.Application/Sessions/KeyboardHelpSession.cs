using System;
using System.Threading;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Sessions;

/// <summary>
/// Owns the keyboard help transient scope (ADR-0076): its state under one lock, its admission rule
/// and the expected-state validation of one qualified close. It knows no other owner, holds no
/// freeze predicate, performs no pane effect and dispatches nothing. Every operation is total: on
/// a state it does not expect it changes nothing.
/// </summary>
public sealed class KeyboardHelpSession
{
    private readonly Lock _sync = new();
    private KeyboardHelpState _state = KeyboardHelpState.Closed;

    /// <summary>Gets the current immutable keyboard help state.</summary>
    public KeyboardHelpState Current
    {
        get
        {
            lock (_sync)
            {
                return _state;
            }
        }
    }

    /// <summary>
    /// Opens the help over the pane that is active at entry when this scope may own input. Listed
    /// pane content is not required. Opening an already open help changes nothing.
    /// </summary>
    /// <param name="active">Pane side active at entry, captured for the one-time focus return.</param>
    /// <param name="ownership">Input ownership the session derived for this scope.</param>
    /// <returns>The state current after this admission decision.</returns>
    public KeyboardHelpState Open(PaneSide active, InteractionOwnership ownership)
    {
        ArgumentNullException.ThrowIfNull(active);
        ArgumentNullException.ThrowIfNull(ownership);
        lock (_sync)
        {
            if (ownership == InteractionOwnership.ScopeOwnsInput && _state is not KeyboardHelpOpen)
            {
                _state = new KeyboardHelpOpen(active);
            }
            return _state;
        }
    }

    /// <summary>
    /// Closes one qualified open help and requests the one-time focus return to the pane captured at
    /// entry. Closing cannot be refused, so the help never traps the user; a stale close changes
    /// nothing.
    /// </summary>
    /// <param name="expected">Exact open state the caller last rendered.</param>
    /// <returns>The state current after this decision.</returns>
    public KeyboardHelpState Close(KeyboardHelpOpen expected)
    {
        ArgumentNullException.ThrowIfNull(expected);
        lock (_sync)
        {
            if (ReferenceEquals(_state, expected))
            {
                _state = KeyboardHelpState.CloseFocusing(expected.ActiveSide);
            }
            return _state;
        }
    }
}
