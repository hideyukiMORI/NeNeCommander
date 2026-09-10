using System;
using System.Collections.Generic;
using NeNeCommander.Application.Commands;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Sessions;

/// <summary>Captures both pane endpoints and candidates for one open command palette.</summary>
public sealed record CommandPaletteOpen : CommandPaletteState
{
    internal CommandPaletteOpen(
        PaneSnapshot left,
        PaneSnapshot right,
        PaneSide activeSide,
        IReadOnlyList<CommandCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ArgumentNullException.ThrowIfNull(activeSide);
        ArgumentNullException.ThrowIfNull(candidates);
        Left = left;
        Right = right;
        ActiveSide = activeSide;
        Candidates = candidates;
    }

    /// <summary>Gets the exact captured left pane snapshot.</summary>
    public PaneSnapshot Left { get; }

    /// <summary>Gets the exact captured right pane snapshot.</summary>
    public PaneSnapshot Right { get; }

    /// <summary>Gets the active command target captured at open.</summary>
    public PaneSide ActiveSide { get; }

    /// <summary>Gets the stable catalog candidates and captured availability.</summary>
    public IReadOnlyList<CommandCandidate> Candidates { get; }
}
