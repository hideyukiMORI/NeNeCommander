using System;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Captures the exact snapshot and activation frame that qualify reusable pane rows.</summary>
internal sealed record PaneProjectionSource
{
    private PaneProjectionSource(PaneSnapshot snapshot, PaneFrame frame)
    {
        Snapshot = snapshot;
        Frame = frame;
    }

    internal PaneSnapshot Snapshot { get; }
    internal PaneFrame Frame { get; }

    internal static PaneProjectionSource Create(PaneSnapshot snapshot, PaneFrame frame)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(frame);
        return new PaneProjectionSource(snapshot, frame);
    }
}
