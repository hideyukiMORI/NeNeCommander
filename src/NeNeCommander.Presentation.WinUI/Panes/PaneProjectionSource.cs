using System;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Captures the exact snapshot and activation frame that qualify reusable pane rows.</summary>
internal sealed record PaneProjectionSource
{
    private PaneProjectionSource(PaneSnapshot snapshot, PaneFrame frame, PaneCounts? counts)
    {
        Snapshot = snapshot;
        Frame = frame;
        Counts = counts;
    }

    internal PaneSnapshot Snapshot { get; }
    internal PaneFrame Frame { get; }
    internal PaneCounts? Counts { get; }

    internal static PaneProjectionSource Create(PaneSnapshot snapshot, PaneFrame frame, PanePresentation? previous)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(frame);
        return new PaneProjectionSource(snapshot, frame,
            snapshot.Content is PaneContentListed listed ? PaneCounts.Create(listed, previous) : null);
    }
}
