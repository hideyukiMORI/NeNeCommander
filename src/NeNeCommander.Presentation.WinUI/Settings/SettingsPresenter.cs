using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Presentation.WinUI.Settings;

/// <summary>Projects application-owned settings state into one deterministic modal presentation.</summary>
public static class SettingsPresenter
{
    private static readonly FrozenDictionary<string, string> NameResourceKeys =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["nene-dark"] = "ColorSchemeNameNeNeDark",
            ["ubuntu"] = "ColorSchemeNameUbuntu",
            ["nene-black"] = "ColorSchemeNameNeNeBlack",
            ["nene-light"] = "ColorSchemeNameNeNeLight",
            ["ubuntu-aubergine"] = "ColorSchemeNameCatalogUbuntuAubergine",
            ["neutral-light"] = "ColorSchemeNameCatalogNeutralLight",
            ["solarized-dark"] = "ColorSchemeNameSolarizedDark",
            ["solarized-light"] = "ColorSchemeNameSolarizedLight",
            ["monokai"] = "ColorSchemeNameMonokai",
            ["dracula"] = "ColorSchemeNameDracula",
            ["one-dark"] = "ColorSchemeNameCatalogOneDark",
            ["night-owl"] = "ColorSchemeNameCatalogNightOwl",
            ["night-owl-light"] = "ColorSchemeNameCatalogNightOwlLight",
            ["blue"] = "ColorSchemeNameCatalogBlue",
            ["catppuccin"] = "ColorSchemeNameCatalogCatppuccin",
            ["darkblue"] = "ColorSchemeNameCatalogDarkblue",
            ["default"] = "ColorSchemeNameCatalogDefault",
            ["delek"] = "ColorSchemeNameCatalogDelek",
            ["desert"] = "ColorSchemeNameCatalogDesert",
            ["elflord"] = "ColorSchemeNameCatalogElflord",
            ["evening"] = "ColorSchemeNameCatalogEvening",
            ["habamax"] = "ColorSchemeNameCatalogHabamax",
            ["industry"] = "ColorSchemeNameCatalogIndustry",
            ["koehler"] = "ColorSchemeNameCatalogKoehler",
            ["lunaperche"] = "ColorSchemeNameCatalogLunaperche",
            ["morning"] = "ColorSchemeNameCatalogMorning",
            ["murphy"] = "ColorSchemeNameCatalogMurphy",
            ["novum"] = "ColorSchemeNameCatalogNovum",
            ["pablo"] = "ColorSchemeNameCatalogPablo",
            ["peachpuff"] = "ColorSchemeNameCatalogPeachpuff",
            ["quiet"] = "ColorSchemeNameCatalogQuiet",
            ["retrobox"] = "ColorSchemeNameCatalogRetrobox",
            ["ron"] = "ColorSchemeNameCatalogRon",
            ["shine"] = "ColorSchemeNameCatalogShine",
            ["slate"] = "ColorSchemeNameCatalogSlate",
            ["sorbet"] = "ColorSchemeNameCatalogSorbet",
            ["torte"] = "ColorSchemeNameCatalogTorte",
            ["unokai"] = "ColorSchemeNameCatalogUnokai",
            ["wildcharm"] = "ColorSchemeNameCatalogWildcharm",
            ["zaibatsu"] = "ColorSchemeNameCatalogZaibatsu",
            ["zellner"] = "ColorSchemeNameCatalogZellner",
            ["abyss"] = "ColorSchemeNameCatalogAbyss",
            ["vscode-dark-2026"] = "ColorSchemeNameCatalogVscodeDark2026",
            ["vscode-light-2026"] = "ColorSchemeNameCatalogVscodeLight2026",
            ["vscode-dark-modern"] = "ColorSchemeNameCatalogVscodeDarkModern",
            ["vscode-dark-plus"] = "ColorSchemeNameCatalogVscodeDarkPlus",
            ["vscode-dark"] = "ColorSchemeNameCatalogVscodeDark",
            ["vscode-high-contrast"] = "ColorSchemeNameCatalogVscodeHighContrast",
            ["vscode-high-contrast-light"] = "ColorSchemeNameCatalogVscodeHighContrastLight",
            ["vscode-light-modern"] = "ColorSchemeNameCatalogVscodeLightModern",
            ["vscode-light-plus"] = "ColorSchemeNameCatalogVscodeLightPlus",
            ["vscode-light"] = "ColorSchemeNameCatalogVscodeLight",
            ["kimbie-dark"] = "ColorSchemeNameCatalogKimbieDark",
            ["monokai-dimmed"] = "ColorSchemeNameCatalogMonokaiDimmed",
            ["quiet-light"] = "ColorSchemeNameCatalogQuietLight",
            ["red"] = "ColorSchemeNameCatalogRed",
            ["tomorrow-night-blue"] = "ColorSchemeNameCatalogTomorrowNightBlue",
            ["monalisa"] = "ColorSchemeNameCatalogMonalisa",
            ["catppuccin-latte"] = "ColorSchemeNameCatalogCatppuccinLatte",
            ["catppuccin-frappe"] = "ColorSchemeNameCatalogCatppuccinFrappe",
            ["catppuccin-macchiato"] = "ColorSchemeNameCatalogCatppuccinMacchiato",
            ["nord"] = "ColorSchemeNameCatalogNord",
            ["gruvbox"] = "ColorSchemeNameCatalogGruvbox",
            ["gruvbox-light"] = "ColorSchemeNameCatalogGruvboxLight",
            ["tokyo-night"] = "ColorSchemeNameCatalogTokyoNight",
            ["tokyo-night-storm"] = "ColorSchemeNameCatalogTokyoNightStorm",
            ["tokyo-night-moon"] = "ColorSchemeNameCatalogTokyoNightMoon",
            ["tokyo-night-light"] = "ColorSchemeNameCatalogTokyoNightLight",
            ["github-dark"] = "ColorSchemeNameCatalogGithubDark",
            ["github-light"] = "ColorSchemeNameCatalogGithubLight",
            ["github-dark-dimmed"] = "ColorSchemeNameCatalogGithubDarkDimmed",
            ["one-light"] = "ColorSchemeNameCatalogOneLight",
            ["intellij-light"] = "ColorSchemeNameCatalogIntellijLight",
            ["darcula"] = "ColorSchemeNameCatalogDarcula",
            ["rose-pine"] = "ColorSchemeNameCatalogRosePine",
            ["rose-pine-moon"] = "ColorSchemeNameCatalogRosePineMoon",
            ["rose-pine-dawn"] = "ColorSchemeNameCatalogRosePineDawn",
            ["everforest-dark"] = "ColorSchemeNameCatalogEverforestDark",
            ["everforest-light"] = "ColorSchemeNameCatalogEverforestLight",
            ["kanagawa"] = "ColorSchemeNameCatalogKanagawa",
            ["kanagawa-dragon"] = "ColorSchemeNameCatalogKanagawaDragon",
            ["kanagawa-lotus"] = "ColorSchemeNameCatalogKanagawaLotus",
            ["material-oceanic"] = "ColorSchemeNameCatalogMaterialOceanic",
            ["material-darker"] = "ColorSchemeNameCatalogMaterialDarker",
            ["material-lighter"] = "ColorSchemeNameCatalogMaterialLighter",
            ["material-palenight"] = "ColorSchemeNameCatalogMaterialPalenight",
            ["material-deep-ocean"] = "ColorSchemeNameCatalogMaterialDeepOcean",
            ["ayu-dark"] = "ColorSchemeNameCatalogAyuDark",
            ["ayu-mirage"] = "ColorSchemeNameCatalogAyuMirage",
            ["ayu-light"] = "ColorSchemeNameCatalogAyuLight",
            ["cobalt2"] = "ColorSchemeNameCatalogCobalt2",
            ["synthwave-84"] = "ColorSchemeNameCatalogSynthwave84",
            ["alloy-core"] = "ColorSchemeNameCatalogAlloyCore",
            ["verdant-rage"] = "ColorSchemeNameCatalogVerdantRage",
            ["scarlet-web"] = "ColorSchemeNameCatalogScarletWeb",
            ["relic-sand"] = "ColorSchemeNameCatalogRelicSand",
            ["soap-rebellion"] = "ColorSchemeNameCatalogSoapRebellion",
            ["code-rain"] = "ColorSchemeNameCatalogCodeRain",
            ["arcane-library"] = "ColorSchemeNameCatalogArcaneLibrary",
            ["royal-kinetic"] = "ColorSchemeNameCatalogRoyalKinetic",
            ["arena-sunset"] = "ColorSchemeNameCatalogArenaSunset",
            ["cosmic-mixtape"] = "ColorSchemeNameCatalogCosmicMixtape",
            ["prism-sanctum"] = "ColorSchemeNameCatalogPrismSanctum",
            ["winter-sentinel"] = "ColorSchemeNameCatalogWinterSentinel",
            ["infinite-dusk"] = "ColorSchemeNameCatalogInfiniteDusk",
            ["stellar-pilot"] = "ColorSchemeNameCatalogStellarPilot",
            ["quantum-honey"] = "ColorSchemeNameCatalogQuantumHoney",
            ["crimson-mercenary"] = "ColorSchemeNameCatalogCrimsonMercenary",
            ["last-outlaw"] = "ColorSchemeNameCatalogLastOutlaw",
            ["ivory-agent"] = "ColorSchemeNameCatalogIvoryAgent",
            ["celestial-brass"] = "ColorSchemeNameCatalogCelestialBrass",
            ["amber-haze"] = "ColorSchemeNameCatalogAmberHaze",
            ["desert-oracle"] = "ColorSchemeNameCatalogDesertOracle",
            ["chrome-wasteland"] = "ColorSchemeNameCatalogChromeWasteland",
            ["neon-grid"] = "ColorSchemeNameCatalogNeonGrid",
            ["biolume-forest"] = "ColorSchemeNameCatalogBiolumeForest",
            ["pastel-concierge"] = "ColorSchemeNameCatalogPastelConcierge",
            ["twilight-jazz"] = "ColorSchemeNameCatalogTwilightJazz",
            ["noir-vigil"] = "ColorSchemeNameCatalogNoirVigil",
            ["carnival-ashes"] = "ColorSchemeNameCatalogCarnivalAshes",
            ["orbital-frost"] = "ColorSchemeNameCatalogOrbitalFrost",
            ["folded-city"] = "ColorSchemeNameCatalogFoldedCity",
            ["midnight-drive"] = "ColorSchemeNameCatalogMidnightDrive",
            ["editorial-cafe"] = "ColorSchemeNameCatalogEditorialCafe",
            ["striped-haunt"] = "ColorSchemeNameCatalogStripedHaunt",
            ["curious-garden"] = "ColorSchemeNameCatalogCuriousGarden",
            ["tidebound-gold"] = "ColorSchemeNameCatalogTideboundGold",
            ["velvet-cabaret"] = "ColorSchemeNameCatalogVelvetCabaret",
            ["lagoon-whisper"] = "ColorSchemeNameCatalogLagoonWhisper",
        }.ToFrozenDictionary(StringComparer.Ordinal);

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
                snapshot.Settings.Display.ColorScheme));
        }
        return new SettingsPresentation(
            snapshot,
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
        return NameResourceKeys.TryGetValue(scheme.Identifier, out string? key)
            ? key
            : throw new InvalidOperationException("The settings editor received an unknown color scheme.");
    }
}
