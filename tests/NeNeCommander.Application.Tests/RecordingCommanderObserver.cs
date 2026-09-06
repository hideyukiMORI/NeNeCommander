using System.Collections.Generic;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

internal sealed class RecordingCommanderObserver : ICommanderProgressObserver
{
    private readonly List<SettingsSnapshot> _settings = [];

    internal IReadOnlyList<SettingsSnapshot> Settings => _settings;

    public void OperationProgressed(DualPaneSnapshot snapshot)
    {
    }

    public void SettingsProgressed(SettingsSnapshot snapshot)
    {
        _settings.Add(snapshot);
    }
}
