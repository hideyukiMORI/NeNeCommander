using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Presentation.WinUI.Settings;

/// <summary>Represents all render-ready state of the settings modal and its separate warning.</summary>
public sealed record SettingsPresentation
{
    private readonly ReadOnlyCollection<SettingsColorSchemeOption> _schemes;

    internal SettingsPresentation(
        SettingsSnapshot source,
        IReadOnlyList<SettingsColorSchemeOption> schemes,
        SettingsSaveStatus saveStatus,
        SettingsWarningPresentation warning)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(schemes);
        ArgumentNullException.ThrowIfNull(saveStatus);
        ArgumentNullException.ThrowIfNull(warning);
        IsOpen = source.Editor == SettingsEditorState.Open;
        ShowHiddenItemsAtLaunch = source.Settings.HiddenItemVisibility == HiddenItemVisibility.Shown;
        _schemes = new List<SettingsColorSchemeOption>(schemes).AsReadOnly();
        SaveStatus = saveStatus;
        Warning = warning;
    }

    /// <summary>Gets whether the settings modal is open.</summary>
    public bool IsOpen { get; }

    /// <summary>Gets the current next-launch hidden-item checkbox value.</summary>
    public bool ShowHiddenItemsAtLaunch { get; }

    /// <summary>Gets all eight scheme options in canonical order.</summary>
    public IReadOnlyList<SettingsColorSchemeOption> Schemes => _schemes;

    /// <summary>Gets the localized save-on-change status.</summary>
    public SettingsSaveStatus SaveStatus { get; }

    /// <summary>Gets the independent persistent warning.</summary>
    public SettingsWarningPresentation Warning { get; }
}
