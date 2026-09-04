namespace NeNeCommander.Application.Settings;

/// <summary>
/// Represents the closed result of reading the persisted settings document: complete settings,
/// no stored document at all, or a typed rejection that keeps the stored document untouched.
/// </summary>
public abstract record SettingsReadOutcome
{
    internal SettingsReadOutcome()
    {
    }

    /// <summary>Creates the outcome for a complete accepted document.</summary>
    /// <param name="settings">Validated settings.</param>
    /// <returns>The read outcome.</returns>
    public static SettingsReadOutcome Read(UserSettings settings)
    {
        return new SettingsRead(settings);
    }

    /// <summary>Creates the outcome for a location that stores no document yet.</summary>
    /// <returns>The absent outcome.</returns>
    public static SettingsReadOutcome Absent()
    {
        return new SettingsAbsent();
    }

    /// <summary>Creates the outcome for a document that could not be read or accepted.</summary>
    /// <param name="kind">Closed rejection reason.</param>
    /// <returns>The rejected outcome.</returns>
    public static SettingsReadOutcome Rejected(SettingsReadFailureKind kind)
    {
        return new SettingsRejected(kind);
    }
}
