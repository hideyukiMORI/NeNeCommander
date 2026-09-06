namespace NeNeCommander.Application.Settings;

/// <summary>Represents the closed persistence state of the session's current settings revision.</summary>
public abstract record SettingsPersistenceState
{
    internal SettingsPersistenceState()
    {
    }

    /// <summary>Gets the state whose current settings are settled without a persistence warning.</summary>
    public static SettingsPersistenceState Succeeded { get; } = new SettingsPersistenceSucceeded();

    /// <summary>Gets the state whose current settings are queued for persistence.</summary>
    public static SettingsPersistenceState Pending { get; } = new SettingsPersistencePending();

    /// <summary>Gets the state whose pending write was cancelled before mutation.</summary>
    public static SettingsPersistenceState Cancelled { get; } = new SettingsPersistenceCancelled();

    /// <summary>Creates the state for a rejected startup document.</summary>
    /// <param name="failure">Closed read rejection.</param>
    /// <returns>The startup-rejected state.</returns>
    public static SettingsPersistenceState StartupRejected(SettingsReadFailureKind failure)
    {
        return new SettingsPersistenceStartupRejected(failure);
    }

    /// <summary>Creates the state for a rejected write.</summary>
    /// <param name="rejection">Closed write rejection.</param>
    /// <returns>The failed state.</returns>
    public static SettingsPersistenceState Failed(SettingsWriteRejected rejection)
    {
        return new SettingsPersistenceFailed(rejection);
    }
}
