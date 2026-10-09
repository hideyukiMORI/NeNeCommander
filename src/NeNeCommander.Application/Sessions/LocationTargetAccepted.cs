using System;
using NeNeCommander.Application.Panes;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Sessions;

/// <summary>
/// Carries the one navigation the session must now perform over an already closed Locations
/// picker: the pane active at open and the selected root.
/// </summary>
public sealed record LocationTargetAccepted : LocationsValidation
{
    internal LocationTargetAccepted(PaneSide side, FileSystemPath target)
    {
        ArgumentNullException.ThrowIfNull(side);
        ArgumentNullException.ThrowIfNull(target);
        Side = side;
        Target = target;
    }

    /// <summary>Gets the pane the selection navigates.</summary>
    public PaneSide Side { get; }

    /// <summary>Gets the selected root.</summary>
    public FileSystemPath Target { get; }
}
