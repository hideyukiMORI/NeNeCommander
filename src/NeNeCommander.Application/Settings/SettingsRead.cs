using System;

namespace NeNeCommander.Application.Settings;

/// <summary>
/// Represents a complete persisted document that became settings without partial application.
/// </summary>
public sealed record SettingsRead : SettingsReadOutcome
{
    internal SettingsRead(UserSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Settings = settings;
    }

    /// <summary>Gets the settings the document described.</summary>
    public UserSettings Settings { get; }
}
