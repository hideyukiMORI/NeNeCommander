using System;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Input;

/// <summary>Requests address editing for the pane whose native control received focus.</summary>
public sealed record AddressFocusSubmission : UserIntent
{
    internal AddressFocusSubmission(PaneSide side)
    {
        ArgumentNullException.ThrowIfNull(side);
        Side = side;
    }

    /// <summary>Gets the exact pane side whose address received focus.</summary>
    public PaneSide Side { get; }
}
