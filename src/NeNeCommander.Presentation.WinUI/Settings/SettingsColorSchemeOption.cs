using System;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Presentation.WinUI.Settings;

/// <summary>Represents one localized radio option for an approved color scheme.</summary>
public sealed record SettingsColorSchemeOption
{
    internal SettingsColorSchemeOption(
        ColorScheme scheme,
        string nameResourceKey,
        ColorScheme selectedScheme)
    {
        ArgumentNullException.ThrowIfNull(scheme);
        ArgumentException.ThrowIfNullOrWhiteSpace(nameResourceKey);
        ArgumentNullException.ThrowIfNull(selectedScheme);
        Scheme = scheme;
        NameResourceKey = nameResourceKey;
        IsSelected = scheme == selectedScheme;
    }

    /// <summary>Gets the approved scheme emitted when this option is selected.</summary>
    public ColorScheme Scheme { get; }

    /// <summary>Gets the localization resource key naming the option.</summary>
    public string NameResourceKey { get; }

    /// <summary>Gets whether this option represents the session's current scheme.</summary>
    public bool IsSelected { get; }
}
