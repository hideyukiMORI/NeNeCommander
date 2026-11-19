using System;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Input;

/// <summary>Requests explicit address editing for the captured pane.</summary>
public sealed record AddressFocusSubmission : UserIntent
{
    private protected override void RestrictDerivation()
    {
    }

    internal AddressFocusSubmission(PaneSide side)
    {
        ArgumentNullException.ThrowIfNull(side);
        Side = side;
    }

    /// <summary>Gets the exact pane side for the requested address edit.</summary>
    public PaneSide Side { get; }
}
