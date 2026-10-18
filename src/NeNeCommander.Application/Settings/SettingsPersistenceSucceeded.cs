namespace NeNeCommander.Application.Settings;

/// <summary>Represents current settings with no pending persistence warning.</summary>
public sealed record SettingsPersistenceSucceeded : SettingsPersistenceState
{
    private protected override void RestrictDerivation()
    {
    }

    internal SettingsPersistenceSucceeded()
    {
    }
}
