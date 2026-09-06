using System;

namespace NeNeCommander.Application.Settings;

/// <summary>Represents the complete immutable settings interaction state of the application session.</summary>
public sealed record SettingsSnapshot
{
    internal SettingsSnapshot(
        UserSettings settings,
        SettingsEditorState editor,
        SettingsPersistenceState persistence)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(persistence);
        Settings = settings;
        Editor = editor;
        Persistence = persistence;
    }

    /// <summary>Gets the current complete settings selected for this session.</summary>
    public UserSettings Settings { get; }

    /// <summary>Gets the closed editor state.</summary>
    public SettingsEditorState Editor { get; }

    /// <summary>Gets the persistence state of the current revision.</summary>
    public SettingsPersistenceState Persistence { get; }
}
