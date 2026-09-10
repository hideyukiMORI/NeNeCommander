using System;
using NeNeCommander.Application.Panes;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Sessions;

/// <summary>Represents address editing for one captured pane and canonical origin location.</summary>
public sealed record AddressEditing : AddressEditorState
{
    internal AddressEditing(PaneSide side, FileSystemPath originalLocation)
    {
        ArgumentNullException.ThrowIfNull(side);
        ArgumentNullException.ThrowIfNull(originalLocation);
        Side = side;
        OriginalLocation = originalLocation;
    }

    /// <summary>Gets the pane whose address is being edited.</summary>
    public PaneSide Side { get; }

    /// <summary>Gets the canonical location shown when editing began.</summary>
    public FileSystemPath OriginalLocation { get; }
}
