namespace NeNeCommander.Application.Settings;

/// <summary>Represents current settings with no pending persistence warning.</summary>
public sealed record SettingsPersistenceSucceeded : SettingsPersistenceState
{
    internal SettingsPersistenceSucceeded()
    {
    }
}
