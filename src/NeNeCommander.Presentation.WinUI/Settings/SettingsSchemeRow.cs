using System;
using System.Globalization;
using System.Text;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Presentation.WinUI.Settings;

/// <summary>Owns one localized theme row without a stale copy of the applied selection.</summary>
public sealed record SettingsSchemeRow
{
    private SettingsSchemeRow(ColorScheme scheme, string name, string appearance, string descriptionFormat)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(appearance);
        ArgumentException.ThrowIfNullOrWhiteSpace(descriptionFormat);
        Scheme = scheme;
        Name = name;
        Appearance = appearance;
        Description = string.Format(CultureInfo.InvariantCulture, CompositeFormat.Parse(descriptionFormat), name, appearance);
    }

    /// <summary>Gets the existing closed scheme, used by the sole settings selection intent.</summary>
    public ColorScheme Scheme { get; }

    /// <summary>Gets the complete localized or source proper name.</summary>
    public string Name { get; }

    /// <summary>Gets the localized light/dark label.</summary>
    public string Appearance { get; }

    /// <summary>Gets the full accessible name and tooltip, including appearance.</summary>
    public string Description { get; }

    /// <summary>Gets the stable automation identity for this closed scheme.</summary>
    public string AutomationId => "SettingsScheme_" + Scheme.Identifier;

    internal static SettingsSchemeRow Create(SettingsColorSchemeOption option, Func<string, string> localize)
    {
        ArgumentNullException.ThrowIfNull(option);
        ArgumentNullException.ThrowIfNull(localize);
        return new SettingsSchemeRow(
            option.Scheme,
            localize(option.NameResourceKey),
            localize(option.Scheme.Appearance == ColorSchemeAppearance.Light ? "SettingsSchemeLight" : "SettingsSchemeDark"),
            localize("SettingsSchemeDescriptionFormat"));
    }
}
