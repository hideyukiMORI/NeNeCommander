using System.Collections.Generic;
using System.Collections.ObjectModel;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Tests;

internal sealed class RecordingDualPaneObserver : IDualPaneProgressObserver
{
    private readonly List<DualPaneSnapshot> _snapshots;

    private RecordingDualPaneObserver()
    {
        _snapshots = [];
    }

    internal IReadOnlyList<DualPaneSnapshot> Snapshots => new ReadOnlyCollection<DualPaneSnapshot>(_snapshots);

    internal static RecordingDualPaneObserver Create()
    {
        return new RecordingDualPaneObserver();
    }

    public void OperationProgressed(DualPaneSnapshot snapshot)
    {
        _snapshots.Add(snapshot);
    }
}
