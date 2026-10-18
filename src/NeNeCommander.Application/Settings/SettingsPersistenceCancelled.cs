namespace NeNeCommander.Application.Settings;

/// <summary>Represents a current settings value whose write was cancelled before mutation.</summary>
public sealed record SettingsPersistenceCancelled : SettingsPersistenceState
{
    private protected override void RestrictDerivation()
    {
    }

    internal SettingsPersistenceCancelled()
    {
    }
}
