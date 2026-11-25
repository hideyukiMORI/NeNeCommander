using System;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Sessions;

/// <summary>Captures the exact side and listed pane currently owned by one native editor.</summary>
public sealed record PaneFilterEditing : PaneFilterState
{
    private protected override void RestrictDerivation()
    {
    }

    internal PaneFilterEditing(PaneSide side, PaneSnapshot sourcePane)
    {
        ArgumentNullException.ThrowIfNull(side);
        ArgumentNullException.ThrowIfNull(sourcePane);
        if (sourcePane.Content is not PaneContentListed)
        {
            throw new ArgumentException("A filter editor requires a listed pane.", nameof(sourcePane));
        }
        Side = side;
        SourcePane = sourcePane;
    }

    /// <summary>Gets the pane addressed by this editor.</summary>
    public PaneSide Side { get; }

    /// <summary>Gets the exact listed pane whose query is edited.</summary>
    public PaneSnapshot SourcePane { get; }

    /// <summary>Gets the sole applied query from the captured pane without a second stored value.</summary>
    public PaneNameFilterQuery Query => ((PaneContentListed)SourcePane.Content).State.NameFilter;
}
