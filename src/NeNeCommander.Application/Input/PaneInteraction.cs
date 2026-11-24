using System;
using System.Linq;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Input;

/// <summary>Qualifies one pointer interaction by its rendered pane owner.</summary>
public abstract record PaneInteraction : UserIntent
{
    /// <summary>Retains the inaccessible abstract boundary on this intermediate public root.</summary>
    private protected abstract override void RestrictDerivation();

    private protected PaneInteraction(PaneSide side, PaneSnapshot expectedSnapshot)
    {
        ArgumentNullException.ThrowIfNull(side);
        ArgumentNullException.ThrowIfNull(expectedSnapshot);
        Side = side;
        ExpectedSnapshot = expectedSnapshot;
    }

    /// <summary>Checks live ownership before either active-side or pane-local state can change.</summary>
    internal bool Matches(PaneSnapshot current)
    {
        return ReferenceEquals(current, ExpectedSnapshot) &&
            current.Activity is not (PaneLoading or PaneLaunching) &&
            (this is not PaneSurfaceInteraction surface || surface.Action != PaneSurfaceAction.NavigateParent || current.CanNavigateParent) &&
            (this is not PaneRowInteraction row ||
                (current.Content is PaneContentListed listed &&
                    listed.State.VisibleEntries.Any(entry => ReferenceEquals(entry, row.Entry))));
    }

    /// <summary>Names the existing pane command to run only after qualified admission.</summary>
    internal UserIntent? NavigationIntent
    {
        get
        {
            if (this is PaneRowInteraction row)
            {
                return row.Action == PaneEntryAction.Open ? UserIntent.OpenFocused : null;
            }
            PaneSurfaceInteraction surface = (PaneSurfaceInteraction)this;
            return surface.Action == PaneSurfaceAction.NavigateParent ? UserIntent.NavigateParent : null;
        }
    }

    /// <summary>Gets the pane explicitly addressed by the gesture.</summary>
    public PaneSide Side { get; }

    /// <summary>Gets the exact pane snapshot rendered for the gesture.</summary>
    public PaneSnapshot ExpectedSnapshot { get; }
}
