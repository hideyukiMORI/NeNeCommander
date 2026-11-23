using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.App.Themes;

/// <summary>
/// Projects one approved color scheme onto the framework resources the host merges. The scheme
/// dictionary is chosen by an exhaustive closed mapping, so persisted text never composes a
/// resource address and an unmapped scheme fails closed instead of loading nothing.
/// </summary>
public static class ColorSchemeResources
{
    private static readonly FrozenDictionary<string, Uri> DictionaryAddresses =
        new Dictionary<string, Uri>(StringComparer.Ordinal)
        {
            ["nene-dark"] = new Uri("ms-appx:///Themes/Schemes/nene-dark.xaml"),
            ["ubuntu"] = new Uri("ms-appx:///Themes/Schemes/ubuntu.xaml"),
            ["nene-black"] = new Uri("ms-appx:///Themes/Schemes/nene-black.xaml"),
            ["nene-light"] = new Uri("ms-appx:///Themes/Schemes/nene-light.xaml"),
            ["ubuntu-aubergine"] = new Uri("ms-appx:///Themes/Schemes/ubuntu-aubergine.xaml"),
            ["neutral-light"] = new Uri("ms-appx:///Themes/Schemes/neutral-light.xaml"),
            ["solarized-dark"] = new Uri("ms-appx:///Themes/Schemes/solarized-dark.xaml"),
            ["solarized-light"] = new Uri("ms-appx:///Themes/Schemes/solarized-light.xaml"),
            ["monokai"] = new Uri("ms-appx:///Themes/Schemes/monokai.xaml"),
            ["dracula"] = new Uri("ms-appx:///Themes/Schemes/dracula.xaml"),
            ["one-dark"] = new Uri("ms-appx:///Themes/Schemes/one-dark.xaml"),
            ["night-owl"] = new Uri("ms-appx:///Themes/Schemes/night-owl.xaml"),
            ["night-owl-light"] = new Uri("ms-appx:///Themes/Schemes/night-owl-light.xaml"),
            ["blue"] = new Uri("ms-appx:///Themes/Schemes/blue.xaml"),
            ["catppuccin"] = new Uri("ms-appx:///Themes/Schemes/catppuccin.xaml"),
            ["darkblue"] = new Uri("ms-appx:///Themes/Schemes/darkblue.xaml"),
            ["default"] = new Uri("ms-appx:///Themes/Schemes/default.xaml"),
            ["delek"] = new Uri("ms-appx:///Themes/Schemes/delek.xaml"),
            ["desert"] = new Uri("ms-appx:///Themes/Schemes/desert.xaml"),
            ["elflord"] = new Uri("ms-appx:///Themes/Schemes/elflord.xaml"),
            ["evening"] = new Uri("ms-appx:///Themes/Schemes/evening.xaml"),
            ["habamax"] = new Uri("ms-appx:///Themes/Schemes/habamax.xaml"),
            ["industry"] = new Uri("ms-appx:///Themes/Schemes/industry.xaml"),
            ["koehler"] = new Uri("ms-appx:///Themes/Schemes/koehler.xaml"),
            ["lunaperche"] = new Uri("ms-appx:///Themes/Schemes/lunaperche.xaml"),
            ["morning"] = new Uri("ms-appx:///Themes/Schemes/morning.xaml"),
            ["murphy"] = new Uri("ms-appx:///Themes/Schemes/murphy.xaml"),
            ["novum"] = new Uri("ms-appx:///Themes/Schemes/novum.xaml"),
            ["pablo"] = new Uri("ms-appx:///Themes/Schemes/pablo.xaml"),
            ["peachpuff"] = new Uri("ms-appx:///Themes/Schemes/peachpuff.xaml"),
            ["quiet"] = new Uri("ms-appx:///Themes/Schemes/quiet.xaml"),
            ["retrobox"] = new Uri("ms-appx:///Themes/Schemes/retrobox.xaml"),
            ["ron"] = new Uri("ms-appx:///Themes/Schemes/ron.xaml"),
            ["shine"] = new Uri("ms-appx:///Themes/Schemes/shine.xaml"),
            ["slate"] = new Uri("ms-appx:///Themes/Schemes/slate.xaml"),
            ["sorbet"] = new Uri("ms-appx:///Themes/Schemes/sorbet.xaml"),
            ["torte"] = new Uri("ms-appx:///Themes/Schemes/torte.xaml"),
            ["unokai"] = new Uri("ms-appx:///Themes/Schemes/unokai.xaml"),
            ["wildcharm"] = new Uri("ms-appx:///Themes/Schemes/wildcharm.xaml"),
            ["zaibatsu"] = new Uri("ms-appx:///Themes/Schemes/zaibatsu.xaml"),
            ["zellner"] = new Uri("ms-appx:///Themes/Schemes/zellner.xaml"),
            ["abyss"] = new Uri("ms-appx:///Themes/Schemes/abyss.xaml"),
            ["vscode-dark-2026"] = new Uri("ms-appx:///Themes/Schemes/vscode-dark-2026.xaml"),
            ["vscode-light-2026"] = new Uri("ms-appx:///Themes/Schemes/vscode-light-2026.xaml"),
            ["vscode-dark-modern"] = new Uri("ms-appx:///Themes/Schemes/vscode-dark-modern.xaml"),
            ["vscode-dark-plus"] = new Uri("ms-appx:///Themes/Schemes/vscode-dark-plus.xaml"),
            ["vscode-dark"] = new Uri("ms-appx:///Themes/Schemes/vscode-dark.xaml"),
            ["vscode-high-contrast"] = new Uri("ms-appx:///Themes/Schemes/vscode-high-contrast.xaml"),
            ["vscode-high-contrast-light"] = new Uri("ms-appx:///Themes/Schemes/vscode-high-contrast-light.xaml"),
            ["vscode-light-modern"] = new Uri("ms-appx:///Themes/Schemes/vscode-light-modern.xaml"),
            ["vscode-light-plus"] = new Uri("ms-appx:///Themes/Schemes/vscode-light-plus.xaml"),
            ["vscode-light"] = new Uri("ms-appx:///Themes/Schemes/vscode-light.xaml"),
            ["kimbie-dark"] = new Uri("ms-appx:///Themes/Schemes/kimbie-dark.xaml"),
            ["monokai-dimmed"] = new Uri("ms-appx:///Themes/Schemes/monokai-dimmed.xaml"),
            ["quiet-light"] = new Uri("ms-appx:///Themes/Schemes/quiet-light.xaml"),
            ["red"] = new Uri("ms-appx:///Themes/Schemes/red.xaml"),
            ["tomorrow-night-blue"] = new Uri("ms-appx:///Themes/Schemes/tomorrow-night-blue.xaml"),
            ["monalisa"] = new Uri("ms-appx:///Themes/Schemes/monalisa.xaml"),
            ["catppuccin-latte"] = new Uri("ms-appx:///Themes/Schemes/catppuccin-latte.xaml"),
            ["catppuccin-frappe"] = new Uri("ms-appx:///Themes/Schemes/catppuccin-frappe.xaml"),
            ["catppuccin-macchiato"] = new Uri("ms-appx:///Themes/Schemes/catppuccin-macchiato.xaml"),
            ["nord"] = new Uri("ms-appx:///Themes/Schemes/nord.xaml"),
            ["gruvbox"] = new Uri("ms-appx:///Themes/Schemes/gruvbox.xaml"),
            ["gruvbox-light"] = new Uri("ms-appx:///Themes/Schemes/gruvbox-light.xaml"),
            ["tokyo-night"] = new Uri("ms-appx:///Themes/Schemes/tokyo-night.xaml"),
            ["tokyo-night-storm"] = new Uri("ms-appx:///Themes/Schemes/tokyo-night-storm.xaml"),
            ["tokyo-night-moon"] = new Uri("ms-appx:///Themes/Schemes/tokyo-night-moon.xaml"),
            ["tokyo-night-light"] = new Uri("ms-appx:///Themes/Schemes/tokyo-night-light.xaml"),
            ["github-dark"] = new Uri("ms-appx:///Themes/Schemes/github-dark.xaml"),
            ["github-light"] = new Uri("ms-appx:///Themes/Schemes/github-light.xaml"),
            ["github-dark-dimmed"] = new Uri("ms-appx:///Themes/Schemes/github-dark-dimmed.xaml"),
            ["one-light"] = new Uri("ms-appx:///Themes/Schemes/one-light.xaml"),
            ["intellij-light"] = new Uri("ms-appx:///Themes/Schemes/intellij-light.xaml"),
            ["darcula"] = new Uri("ms-appx:///Themes/Schemes/darcula.xaml"),
            ["rose-pine"] = new Uri("ms-appx:///Themes/Schemes/rose-pine.xaml"),
            ["rose-pine-moon"] = new Uri("ms-appx:///Themes/Schemes/rose-pine-moon.xaml"),
            ["rose-pine-dawn"] = new Uri("ms-appx:///Themes/Schemes/rose-pine-dawn.xaml"),
            ["everforest-dark"] = new Uri("ms-appx:///Themes/Schemes/everforest-dark.xaml"),
            ["everforest-light"] = new Uri("ms-appx:///Themes/Schemes/everforest-light.xaml"),
            ["kanagawa"] = new Uri("ms-appx:///Themes/Schemes/kanagawa.xaml"),
            ["kanagawa-dragon"] = new Uri("ms-appx:///Themes/Schemes/kanagawa-dragon.xaml"),
            ["kanagawa-lotus"] = new Uri("ms-appx:///Themes/Schemes/kanagawa-lotus.xaml"),
            ["material-oceanic"] = new Uri("ms-appx:///Themes/Schemes/material-oceanic.xaml"),
            ["material-darker"] = new Uri("ms-appx:///Themes/Schemes/material-darker.xaml"),
            ["material-lighter"] = new Uri("ms-appx:///Themes/Schemes/material-lighter.xaml"),
            ["material-palenight"] = new Uri("ms-appx:///Themes/Schemes/material-palenight.xaml"),
            ["material-deep-ocean"] = new Uri("ms-appx:///Themes/Schemes/material-deep-ocean.xaml"),
            ["ayu-dark"] = new Uri("ms-appx:///Themes/Schemes/ayu-dark.xaml"),
            ["ayu-mirage"] = new Uri("ms-appx:///Themes/Schemes/ayu-mirage.xaml"),
            ["ayu-light"] = new Uri("ms-appx:///Themes/Schemes/ayu-light.xaml"),
            ["cobalt2"] = new Uri("ms-appx:///Themes/Schemes/cobalt2.xaml"),
            ["synthwave-84"] = new Uri("ms-appx:///Themes/Schemes/synthwave-84.xaml"),
            ["alloy-core"] = new Uri("ms-appx:///Themes/Schemes/alloy-core.xaml"),
            ["verdant-rage"] = new Uri("ms-appx:///Themes/Schemes/verdant-rage.xaml"),
            ["scarlet-web"] = new Uri("ms-appx:///Themes/Schemes/scarlet-web.xaml"),
            ["relic-sand"] = new Uri("ms-appx:///Themes/Schemes/relic-sand.xaml"),
            ["soap-rebellion"] = new Uri("ms-appx:///Themes/Schemes/soap-rebellion.xaml"),
            ["code-rain"] = new Uri("ms-appx:///Themes/Schemes/code-rain.xaml"),
            ["arcane-library"] = new Uri("ms-appx:///Themes/Schemes/arcane-library.xaml"),
            ["royal-kinetic"] = new Uri("ms-appx:///Themes/Schemes/royal-kinetic.xaml"),
            ["arena-sunset"] = new Uri("ms-appx:///Themes/Schemes/arena-sunset.xaml"),
            ["cosmic-mixtape"] = new Uri("ms-appx:///Themes/Schemes/cosmic-mixtape.xaml"),
            ["prism-sanctum"] = new Uri("ms-appx:///Themes/Schemes/prism-sanctum.xaml"),
            ["winter-sentinel"] = new Uri("ms-appx:///Themes/Schemes/winter-sentinel.xaml"),
            ["infinite-dusk"] = new Uri("ms-appx:///Themes/Schemes/infinite-dusk.xaml"),
            ["stellar-pilot"] = new Uri("ms-appx:///Themes/Schemes/stellar-pilot.xaml"),
            ["quantum-honey"] = new Uri("ms-appx:///Themes/Schemes/quantum-honey.xaml"),
            ["crimson-mercenary"] = new Uri("ms-appx:///Themes/Schemes/crimson-mercenary.xaml"),
            ["last-outlaw"] = new Uri("ms-appx:///Themes/Schemes/last-outlaw.xaml"),
            ["ivory-agent"] = new Uri("ms-appx:///Themes/Schemes/ivory-agent.xaml"),
            ["celestial-brass"] = new Uri("ms-appx:///Themes/Schemes/celestial-brass.xaml"),
            ["amber-haze"] = new Uri("ms-appx:///Themes/Schemes/amber-haze.xaml"),
            ["desert-oracle"] = new Uri("ms-appx:///Themes/Schemes/desert-oracle.xaml"),
            ["chrome-wasteland"] = new Uri("ms-appx:///Themes/Schemes/chrome-wasteland.xaml"),
            ["neon-grid"] = new Uri("ms-appx:///Themes/Schemes/neon-grid.xaml"),
            ["biolume-forest"] = new Uri("ms-appx:///Themes/Schemes/biolume-forest.xaml"),
            ["pastel-concierge"] = new Uri("ms-appx:///Themes/Schemes/pastel-concierge.xaml"),
            ["twilight-jazz"] = new Uri("ms-appx:///Themes/Schemes/twilight-jazz.xaml"),
            ["noir-vigil"] = new Uri("ms-appx:///Themes/Schemes/noir-vigil.xaml"),
            ["carnival-ashes"] = new Uri("ms-appx:///Themes/Schemes/carnival-ashes.xaml"),
            ["orbital-frost"] = new Uri("ms-appx:///Themes/Schemes/orbital-frost.xaml"),
            ["folded-city"] = new Uri("ms-appx:///Themes/Schemes/folded-city.xaml"),
            ["midnight-drive"] = new Uri("ms-appx:///Themes/Schemes/midnight-drive.xaml"),
            ["editorial-cafe"] = new Uri("ms-appx:///Themes/Schemes/editorial-cafe.xaml"),
            ["striped-haunt"] = new Uri("ms-appx:///Themes/Schemes/striped-haunt.xaml"),
            ["curious-garden"] = new Uri("ms-appx:///Themes/Schemes/curious-garden.xaml"),
            ["tidebound-gold"] = new Uri("ms-appx:///Themes/Schemes/tidebound-gold.xaml"),
            ["velvet-cabaret"] = new Uri("ms-appx:///Themes/Schemes/velvet-cabaret.xaml"),
            ["lagoon-whisper"] = new Uri("ms-appx:///Themes/Schemes/lagoon-whisper.xaml"),
        }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>Resolves the packaged resource address of the scheme's dictionary.</summary>
    /// <param name="scheme">Approved color scheme.</param>
    /// <returns>The absolute packaged address of the scheme dictionary.</returns>
    /// <exception cref="InvalidOperationException">The scheme has no shipped dictionary, which is a defect.</exception>
    public static Uri ResolveDictionaryAddress(ColorScheme scheme)
    {
        ArgumentNullException.ThrowIfNull(scheme);
        return DictionaryAddresses.TryGetValue(scheme.Identifier, out Uri? address)
            ? address
            : throw new InvalidOperationException("The color scheme has no shipped resource dictionary.");
    }

    /// <summary>
    /// Copies one scheme's brush colors into the merged scheme dictionary without replacing it. Each
    /// brush keeps its instance and receives the selected color, so every element that already
    /// resolved it through <c>StaticResource</c> or a resource lookup repaints with the selected
    /// scheme. A dictionary loaded from a <c>Source</c> rejects local values, so the plain
    /// <c>Color</c> entries keep the first scheme's values; they are consumed only by the brushes of
    /// the scheme dictionaries themselves, never by a view (ARC-012).
    /// </summary>
    /// <param name="selected">Freshly loaded dictionary of the selected scheme.</param>
    /// <param name="merged">The one scheme dictionary merged into the application resources.</param>
    /// <exception cref="InvalidOperationException">The dictionaries do not declare the same brushes, which is a defect.</exception>
    public static void CopySchemeInPlace(ResourceDictionary selected, ResourceDictionary merged)
    {
        ArgumentNullException.ThrowIfNull(selected);
        ArgumentNullException.ThrowIfNull(merged);
        foreach (KeyValuePair<object, object> entry in selected.Where(entry => entry.Value is SolidColorBrush))
        {
            SolidColorBrush source = (SolidColorBrush)entry.Value;
            SolidColorBrush target = merged.TryGetValue(entry.Key, out object? existing) &&
                existing is SolidColorBrush brush
                    ? brush
                    : throw new InvalidOperationException("The merged scheme dictionary lacks a declared brush.");
            target.Color = source.Color;
        }
    }

    /// <summary>
    /// Translates the closed scheme appearance into the framework element theme so that
    /// framework-provided controls follow the scheme instead of the operating-system theme.
    /// </summary>
    /// <param name="appearance">Closed scheme appearance.</param>
    /// <returns>The matching element theme.</returns>
    /// <exception cref="InvalidOperationException">The appearance has no framework theme, which is a defect.</exception>
    public static ElementTheme ResolveElementTheme(ColorSchemeAppearance appearance)
    {
        ArgumentNullException.ThrowIfNull(appearance);
        return appearance == ColorSchemeAppearance.Dark
            ? ElementTheme.Dark
            : appearance == ColorSchemeAppearance.Light
                ? ElementTheme.Light
                : throw new InvalidOperationException("The scheme appearance has no framework element theme.");
    }
}
