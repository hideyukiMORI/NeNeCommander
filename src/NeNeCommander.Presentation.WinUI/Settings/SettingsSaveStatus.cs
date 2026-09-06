namespace NeNeCommander.Presentation.WinUI.Settings;

/// <summary>Identifies the localized save-on-change status rendered inside the settings modal.</summary>
public sealed record SettingsSaveStatus
{
    /// <summary>Gets the settled status.</summary>
    public static SettingsSaveStatus Succeeded { get; } = new("SettingsSaveStatusSucceeded");

    /// <summary>Gets the status shown while a complete value waits for persistence.</summary>
    public static SettingsSaveStatus Pending { get; } = new("SettingsSaveStatusPending");

    /// <summary>Gets the status shown when the current session value was not persisted.</summary>
    public static SettingsSaveStatus Failed { get; } = new("SettingsSaveStatusFailed");

    private SettingsSaveStatus(string resourceKey)
    {
        ResourceKey = resourceKey;
    }

    /// <summary>Gets the localization resource key naming this status.</summary>
    public string ResourceKey { get; }
}
