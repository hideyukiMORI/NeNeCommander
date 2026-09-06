namespace NeNeCommander.Presentation.WinUI.Settings;

/// <summary>Represents the independent persistent settings-warning surface.</summary>
public sealed record SettingsWarningPresentation
{
    private SettingsWarningPresentation(string resourceKey)
    {
        ResourceKey = resourceKey;
    }

    /// <summary>Gets the hidden warning.</summary>
    public static SettingsWarningPresentation Hidden { get; } = new("SettingsWarningHidden");

    /// <summary>Gets the warning for a rejected startup document.</summary>
    public static SettingsWarningPresentation StartupRejected { get; } =
        new("SettingsWarningStartupRejected");

    /// <summary>Gets the warning for a choice that could not be persisted.</summary>
    public static SettingsWarningPresentation SaveFailed { get; } =
        new("SettingsWarningSaveFailed");

    /// <summary>Gets whether the independent warning surface is visible.</summary>
    public bool IsVisible => this != Hidden;

    /// <summary>Gets the localization resource key naming the warning.</summary>
    public string ResourceKey { get; }
}
