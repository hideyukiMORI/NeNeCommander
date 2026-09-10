using System;
using System.Collections.Generic;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Presentation.WinUI.Settings;

/// <summary>Projects application-owned settings state into one deterministic modal presentation.</summary>
public static class SettingsPresenter
{
    /// <summary>Projects one complete settings snapshot without changing it.</summary>
    public static SettingsPresentation Present(SettingsSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        List<SettingsColorSchemeOption> schemes = [];
        foreach (ColorScheme scheme in ColorScheme.All)
        {
            schemes.Add(new SettingsColorSchemeOption(
                scheme,
                NameResourceKey(scheme),
                snapshot.Settings.ColorScheme));
        }
        return new SettingsPresentation(
            snapshot.Editor,
            snapshot.Settings.HiddenItemVisibility,
            schemes,
            PresentSaveStatus(snapshot.Persistence),
            PresentWarning(snapshot.Persistence));
    }

    internal static SettingsSaveStatus PresentSaveStatus(SettingsPersistenceState persistence)
    {
        return persistence is SettingsPersistencePending
            ? SettingsSaveStatus.Pending
            : persistence is SettingsPersistenceSucceeded
                ? SettingsSaveStatus.Succeeded
                : SettingsSaveStatus.Failed;
    }

    internal static SettingsWarningPresentation PresentWarning(SettingsPersistenceState persistence)
    {
        return persistence is SettingsPersistenceStartupRejected
            ? SettingsWarningPresentation.StartupRejected
            : persistence is SettingsPersistenceFailed or SettingsPersistenceCancelled
                ? SettingsWarningPresentation.SaveFailed
                : SettingsWarningPresentation.Hidden;
    }

    private static string NameResourceKey(ColorScheme scheme)
    {
        return scheme.Identifier switch
        {
            "nene-dark" => "ColorSchemeNameNeNeDark",
            "ubuntu" => "ColorSchemeNameUbuntu",
            "monokai" => "ColorSchemeNameMonokai",
            "solarized-dark" => "ColorSchemeNameSolarizedDark",
            "solarized-light" => "ColorSchemeNameSolarizedLight",
            "dracula" => "ColorSchemeNameDracula",
            "nene-black" => "ColorSchemeNameNeNeBlack",
            "nene-light" => "ColorSchemeNameNeNeLight",
            _ => throw new InvalidOperationException("The settings editor received an unknown color scheme."),
        };
    }
}
