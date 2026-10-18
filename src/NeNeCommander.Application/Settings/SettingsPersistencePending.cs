namespace NeNeCommander.Application.Settings;

/// <summary>Represents current settings whose complete value is queued for persistence.</summary>
public sealed record SettingsPersistencePending : SettingsPersistenceState
{
    private protected override void RestrictDerivation()
    {
    }

    internal SettingsPersistencePending()
    {
    }
}
