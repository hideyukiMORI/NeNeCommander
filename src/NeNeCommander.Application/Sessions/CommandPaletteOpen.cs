using System;
using System.Collections.Generic;
using NeNeCommander.Application.Commands;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Sessions;

/// <summary>Captures both pane endpoints, surface and candidates for one command selection.</summary>
public sealed record CommandPaletteOpen : CommandPaletteState
{
    private protected override void RestrictDerivation()
    {
    }

    internal CommandPaletteOpen(
        DualPaneSnapshot panes,
        CommandSurface surface,
        IReadOnlyList<CommandCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(panes);
        ArgumentNullException.ThrowIfNull(surface);
        ArgumentNullException.ThrowIfNull(candidates);
        Left = panes.Left;
        Right = panes.Right;
        ActiveSide = panes.ActiveSide;
        Surface = surface;
        Candidates = candidates;
    }

    /// <summary>Gets the exact captured left pane snapshot.</summary>
    public PaneSnapshot Left { get; }

    /// <summary>Gets the exact captured right pane snapshot.</summary>
    public PaneSnapshot Right { get; }

    /// <summary>Gets the active command target captured at open.</summary>
    public PaneSide ActiveSide { get; }

    /// <summary>Gets the sole surface permitted to render this captured selection.</summary>
    public CommandSurface Surface { get; }

    /// <summary>Gets the stable catalog candidates and captured availability.</summary>
    public IReadOnlyList<CommandCandidate> Candidates { get; }
}
