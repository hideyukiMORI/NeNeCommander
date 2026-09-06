namespace NeNeCommander.Application.Settings;

/// <summary>Represents a current settings value whose write was cancelled before mutation.</summary>
public sealed record SettingsPersistenceCancelled : SettingsPersistenceState
{
    internal SettingsPersistenceCancelled()
    {
    }
}
