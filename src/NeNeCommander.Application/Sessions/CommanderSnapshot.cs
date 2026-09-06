using System;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Sessions;

/// <summary>Represents the complete immutable pane and settings state of the application session.</summary>
public sealed record CommanderSnapshot
{
    internal CommanderSnapshot(DualPaneSnapshot panes, SettingsSnapshot settings)
    {
        ArgumentNullException.ThrowIfNull(panes);
        ArgumentNullException.ThrowIfNull(settings);
        Panes = panes;
        Settings = settings;
    }

    /// <summary>Gets the dual-pane state and file-operation activity.</summary>
    public DualPaneSnapshot Panes { get; }

    /// <summary>Gets the settings editor and persistence state.</summary>
    public SettingsSnapshot Settings { get; }
}
