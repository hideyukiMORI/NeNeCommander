using System;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Input;

/// <summary>Requests one qualified action from a pane background or header control.</summary>
public sealed record PaneSurfaceInteraction : PaneInteraction
{
    private protected override void RestrictDerivation()
    {
    }

    internal PaneSurfaceInteraction(PaneSide side, PaneSnapshot expectedSnapshot, PaneSurfaceAction action)
        : base(side, expectedSnapshot)
    {
        ArgumentNullException.ThrowIfNull(action);
        Action = action;
    }

    /// <summary>Gets the closed pane-surface action.</summary>
    public PaneSurfaceAction Action { get; }
}
