namespace NeNeCommander.Application.Settings;

/// <summary>
/// Represents a settings location that stores no document. A read uses the default settings and
/// never creates a document; only an explicit later selection may enter the write boundary.
/// </summary>
public sealed record SettingsAbsent : SettingsReadOutcome
{
    internal SettingsAbsent()
    {
    }
}
