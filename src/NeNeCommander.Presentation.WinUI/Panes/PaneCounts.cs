using System;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Projects visible and loaded direct-entry counts plus explicit selection. Loaded means admitted
/// entries in this listing, never an unqualified total for a bounded or incomplete directory read.
/// </summary>
internal sealed record PaneCounts
{
    private PaneCounts(PaneEntryCounts visible, PaneEntryCounts loaded, int selected)
    {
        Visible = visible;
        Loaded = loaded;
        Selected = selected;
    }

    /// <summary>Gets counts for the rows currently displayed.</summary>
    public PaneEntryCounts Visible { get; }

    /// <summary>Gets counts for every admitted entry, including entries currently hidden.</summary>
    public PaneEntryCounts Loaded { get; }

    /// <summary>Gets the explicit selection count, independent of the single focused item.</summary>
    public int Selected { get; }

    internal static PaneCounts Create(PaneContentListed listed, PanePresentation? previous)
    {
        ArgumentNullException.ThrowIfNull(listed);
        PaneContentListed? prior = previous?.SourceSnapshot.Content as PaneContentListed;
        PaneCounts? priorCounts = previous?.Counts;
        PaneEntryCounts loaded = prior is not null && priorCounts is not null &&
            ReferenceEquals(prior.Listing, listed.Listing)
                ? priorCounts.Loaded
                : PaneEntryCounts.Create(listed.Listing.Entries);
        PaneEntryCounts visible = prior is not null && priorCounts is not null &&
            ReferenceEquals(prior.State.VisibleEntries, listed.State.VisibleEntries)
                ? priorCounts.Visible
                : listed.State.VisibleEntries.Count == loaded.Total
                    ? loaded
                    : PaneEntryCounts.Create(listed.State.VisibleEntries);
        int selected = listed.State.Selection.Count;
        return priorCounts is not null && ReferenceEquals(priorCounts.Loaded, loaded) &&
            ReferenceEquals(priorCounts.Visible, visible) && priorCounts.Selected == selected
                ? priorCounts
                : new PaneCounts(visible, loaded, selected);
    }
}
