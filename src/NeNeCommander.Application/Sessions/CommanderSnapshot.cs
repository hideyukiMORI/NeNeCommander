using System;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Sessions;

/// <summary>Represents the complete immutable pane and settings state of the application session.</summary>
public sealed record CommanderSnapshot
{
    internal CommanderSnapshot(
        DualPaneSnapshot panes,
        SettingsSnapshot settings,
        AddressEditorState addressEditor)
    {
        ArgumentNullException.ThrowIfNull(panes);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(addressEditor);
        Panes = panes;
        Settings = settings;
        AddressEditor = addressEditor;
    }

    /// <summary>Gets the dual-pane state and file-operation activity.</summary>
    public DualPaneSnapshot Panes { get; }

    /// <summary>Gets the settings editor and persistence state.</summary>
    public SettingsSnapshot Settings { get; }

    /// <summary>Gets the application-owned address editor state.</summary>
    public AddressEditorState AddressEditor { get; }
}
