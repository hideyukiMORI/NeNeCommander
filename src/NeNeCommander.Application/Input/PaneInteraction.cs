using System;
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
            current.Activity is not (PaneLoading or PaneLaunching) && MatchesContent(current);
    }

    /// <summary>Checks the concrete interaction's owned content after common snapshot/activity admission.</summary>
    private protected abstract bool MatchesContent(PaneSnapshot current);

    /// <summary>Names the existing pane command to run only after qualified admission.</summary>
    internal abstract UserIntent? NavigationIntent { get; }

    /// <summary>Gets the pane explicitly addressed by the gesture.</summary>
    public PaneSide Side { get; }

    /// <summary>Gets the exact pane snapshot rendered for the gesture.</summary>
    public PaneSnapshot ExpectedSnapshot { get; }
}
