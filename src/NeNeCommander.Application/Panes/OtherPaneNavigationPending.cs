using System;

namespace NeNeCommander.Application.Panes;

/// <summary>Names the exact existing pane read that one opposite-pane command may abandon.</summary>
internal sealed record OtherPaneNavigationPending : OtherPaneNavigationState
{
    private protected override void RestrictDerivation()
    {
    }

    internal OtherPaneNavigationPending(PaneSide side, PaneLoading read)
    {
        ArgumentNullException.ThrowIfNull(side);
        ArgumentNullException.ThrowIfNull(read);
        Side = side;
        Read = read;
    }

    internal PaneSide Side { get; }

    internal PaneLoading Read { get; }
}
