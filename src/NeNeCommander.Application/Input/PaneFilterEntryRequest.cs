using System;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Input;

/// <summary>Qualifies a filter surface action through the existing pane activation interaction.</summary>
public sealed record PaneFilterEntryRequest : UserIntent
{
    private protected override void RestrictDerivation()
    {
    }

    internal PaneFilterEntryRequest(PaneSide side, PaneSnapshot expectedSnapshot, PaneFilterEntryAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        Interaction = new PaneSurfaceInteraction(side, expectedSnapshot, PaneSurfaceAction.Activate);
        Action = action;
    }

    /// <summary>Gets the sole qualified pane activation request used before the filter action.</summary>
    public PaneSurfaceInteraction Interaction { get; }

    /// <summary>Gets the closed entry action.</summary>
    public PaneFilterEntryAction Action { get; }
}
