using System;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Projects the dual-pane snapshot onto two pane presentations and their activation frames.
/// </summary>
public static class DualPanePresenter
{
    /// <summary>Translates both panes and the active side without changing any state.</summary>
    /// <param name="snapshot">Current dual-pane snapshot.</param>
    /// <returns>A render-ready presentation.</returns>
    public static DualPanePresentation Present(DualPaneSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        PaneFrame leftFrame = snapshot.ActiveSide == PaneSide.Left ? PaneFrame.Active : PaneFrame.Passive;
        PaneFrame rightFrame = snapshot.ActiveSide == PaneSide.Right ? PaneFrame.Active : PaneFrame.Passive;
        return new DualPanePresentation(
            PaneListingPresenter.Present(snapshot.Left),
            leftFrame,
            PaneListingPresenter.Present(snapshot.Right),
            rightFrame,
            snapshot.ActiveSide);
    }
}
