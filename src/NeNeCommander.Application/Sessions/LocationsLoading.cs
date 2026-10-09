using System;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Sessions;

/// <summary>
/// Represents the Locations picker while both sections are being read. It owns input and routes
/// nothing until both reads have produced their outcomes.
/// </summary>
public sealed record LocationsLoading : LocationsState
{
    internal LocationsLoading(PaneSide activeSide)
    {
        ArgumentNullException.ThrowIfNull(activeSide);
        ActiveSide = activeSide;
    }

    /// <summary>Gets the pane that was active at open and that a selection navigates.</summary>
    public PaneSide ActiveSide { get; }
}
