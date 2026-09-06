namespace NeNeCommander.Application.Settings;

/// <summary>Represents the closed result of writing one complete settings document.</summary>
public abstract record SettingsWriteOutcome
{
    internal SettingsWriteOutcome()
    {
    }

    /// <summary>Creates the outcome for an installed complete document.</summary>
    /// <returns>The successful outcome.</returns>
    public static SettingsWriteOutcome Succeeded()
    {
        return new SettingsWriteSucceeded();
    }

    /// <summary>Creates the outcome for a rejected write and its separate filesystem effects.</summary>
    /// <param name="failure">Closed rejection reason.</param>
    /// <param name="directoryEffect">Observed settings-directory effect.</param>
    /// <param name="temporaryEffect">Temporary-artifact residue.</param>
    /// <returns>The rejected outcome.</returns>
    public static SettingsWriteOutcome Rejected(
        SettingsWriteFailureKind failure,
        SettingsDirectoryEffect directoryEffect,
        SettingsWriteEffect temporaryEffect)
    {
        return new SettingsWriteRejected(failure, directoryEffect, temporaryEffect);
    }
}
