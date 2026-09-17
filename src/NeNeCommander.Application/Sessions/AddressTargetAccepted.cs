using System;
using NeNeCommander.Application.Panes;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Sessions;

/// <summary>Carries the parsed target the session must now read into the captured pane.</summary>
public sealed record AddressTargetAccepted : AddressEditorValidation
{
    internal AddressTargetAccepted(PaneSide side, FileSystemPath target)
    {
        ArgumentNullException.ThrowIfNull(side);
        ArgumentNullException.ThrowIfNull(target);
        Side = side;
        Target = target;
    }

    /// <summary>Gets the pane side captured when editing began.</summary>
    public PaneSide Side { get; }

    /// <summary>Gets the canonical target parsed from the accepted submission.</summary>
    public FileSystemPath Target { get; }
}
