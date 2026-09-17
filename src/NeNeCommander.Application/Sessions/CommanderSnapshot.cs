using System;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Sessions;

/// <summary>Represents the complete immutable pane, settings, and transient scope state of the application session.</summary>
public sealed record CommanderSnapshot
{
    internal CommanderSnapshot(
        DualPaneSnapshot panes,
        SettingsSnapshot settings,
        TransientScopeSnapshot scopes)
    {
        ArgumentNullException.ThrowIfNull(panes);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(scopes);
        Panes = panes;
        Settings = settings;
        Scopes = scopes;
    }

    /// <summary>Gets the dual-pane state and file-operation activity.</summary>
    public DualPaneSnapshot Panes { get; }

    /// <summary>Gets the settings editor and persistence state.</summary>
    public SettingsSnapshot Settings { get; }

    /// <summary>Gets the state of every transient scope the session coordinates.</summary>
    public TransientScopeSnapshot Scopes { get; }
}
