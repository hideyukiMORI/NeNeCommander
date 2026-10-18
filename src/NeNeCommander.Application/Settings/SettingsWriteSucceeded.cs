namespace NeNeCommander.Application.Settings;

/// <summary>Represents a complete settings document installed at the sole location.</summary>
public sealed record SettingsWriteSucceeded : SettingsWriteOutcome
{
    private protected override void RestrictDerivation()
    {
    }

    internal SettingsWriteSucceeded()
    {
    }
}
