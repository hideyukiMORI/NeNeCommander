using System;

namespace NeNeCommander.Application.Settings;

/// <summary>Represents current session settings whose latest completed write was rejected.</summary>
public sealed record SettingsPersistenceFailed : SettingsPersistenceState
{
    internal SettingsPersistenceFailed(SettingsWriteRejected rejection)
    {
        ArgumentNullException.ThrowIfNull(rejection);
        Rejection = rejection;
    }

    /// <summary>Gets the closed write rejection.</summary>
    public SettingsWriteRejected Rejection { get; }
}
