namespace NeNeCommander.Application.Settings;

/// <summary>
/// Represents a settings location that stores no document. The caller uses the default settings
/// and the store writes nothing, so an absent document never becomes a silent first write.
/// </summary>
public sealed record SettingsAbsent : SettingsReadOutcome
{
    internal SettingsAbsent()
    {
    }
}
