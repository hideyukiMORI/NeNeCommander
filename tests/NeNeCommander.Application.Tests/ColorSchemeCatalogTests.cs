using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves the expanded catalog's exact order, appearances, immutable ownership and value semantics.</summary>
[TestClass]
public sealed class ColorSchemeCatalogTests
{
    private static readonly string[] ExpectedIdentifiers =
    [
        "nene-dark",
        "ubuntu",
        "nene-black",
        "nene-light",
        "ubuntu-aubergine",
        "neutral-light",
        "solarized-dark",
        "solarized-light",
        "monokai",
        "dracula",
        "one-dark",
        "night-owl",
        "night-owl-light",
        "blue",
        "catppuccin",
        "darkblue",
        "default",
        "delek",
        "desert",
        "elflord",
        "evening",
        "habamax",
        "industry",
        "koehler",
        "lunaperche",
        "morning",
        "murphy",
        "novum",
        "pablo",
        "peachpuff",
        "quiet",
        "retrobox",
        "ron",
        "shine",
        "slate",
        "sorbet",
        "torte",
        "unokai",
        "wildcharm",
        "zaibatsu",
        "zellner",
        "abyss",
        "vscode-dark-2026",
        "vscode-light-2026",
        "vscode-dark-modern",
        "vscode-dark-plus",
        "vscode-dark",
        "vscode-high-contrast",
        "vscode-high-contrast-light",
        "vscode-light-modern",
        "vscode-light-plus",
        "vscode-light",
        "kimbie-dark",
        "monokai-dimmed",
        "quiet-light",
        "red",
        "tomorrow-night-blue",
        "monalisa",
        "catppuccin-latte",
        "catppuccin-frappe",
        "catppuccin-macchiato",
        "nord",
        "gruvbox",
        "gruvbox-light",
        "tokyo-night",
        "tokyo-night-storm",
        "tokyo-night-moon",
        "tokyo-night-light",
        "github-dark",
        "github-light",
        "github-dark-dimmed",
        "one-light",
        "intellij-light",
        "darcula",
        "rose-pine",
        "rose-pine-moon",
        "rose-pine-dawn",
        "everforest-dark",
        "everforest-light",
        "kanagawa",
        "kanagawa-dragon",
        "kanagawa-lotus",
        "material-oceanic",
        "material-darker",
        "material-lighter",
        "material-palenight",
        "material-deep-ocean",
        "ayu-dark",
        "ayu-mirage",
        "ayu-light",
        "cobalt2",
        "synthwave-84",
        "alloy-core",
        "verdant-rage",
        "scarlet-web",
        "relic-sand",
        "soap-rebellion",
        "code-rain",
        "arcane-library",
        "royal-kinetic",
        "arena-sunset",
        "cosmic-mixtape",
        "prism-sanctum",
        "winter-sentinel",
        "infinite-dusk",
        "stellar-pilot",
        "quantum-honey",
        "crimson-mercenary",
        "last-outlaw",
        "ivory-agent",
        "celestial-brass",
        "amber-haze",
        "desert-oracle",
        "chrome-wasteland",
        "neon-grid",
        "biolume-forest",
        "pastel-concierge",
        "twilight-jazz",
        "noir-vigil",
        "carnival-ashes",
        "orbital-frost",
        "folded-city",
        "midnight-drive",
        "editorial-cafe",
        "striped-haunt",
        "curious-garden",
        "tidebound-gold",
        "velvet-cabaret",
        "lagoon-whisper",
    ];

    private static readonly HashSet<string> LightIdentifiers =
    [
        "nene-light",
        "neutral-light",
        "solarized-light",
        "night-owl-light",
        "delek",
        "morning",
        "peachpuff",
        "shine",
        "zellner",
        "vscode-light-2026",
        "vscode-high-contrast-light",
        "vscode-light-modern",
        "vscode-light-plus",
        "vscode-light",
        "quiet-light",
        "catppuccin-latte",
        "gruvbox-light",
        "tokyo-night-light",
        "github-light",
        "one-light",
        "intellij-light",
        "rose-pine-dawn",
        "everforest-light",
        "kanagawa-lotus",
        "material-lighter",
        "ayu-light",
        "relic-sand",
        "ivory-agent",
        "celestial-brass",
        "desert-oracle",
        "pastel-concierge",
        "orbital-frost",
        "editorial-cafe",
        "striped-haunt",
        "curious-garden",
    ];

    /// <summary>Proves all fixed identifiers and appearances, including the corrected shared light palette.</summary>
    [TestMethod]
    public void AllWhenEnumeratedKeepsFixedOrderAppearanceAndCanonicalParse()
    {
        Assert.HasCount(ExpectedIdentifiers.Length, ColorScheme.All);
        for (int index = 0; index < ExpectedIdentifiers.Length; index++)
        {
            ColorScheme scheme = ColorScheme.All[index];
            Assert.AreEqual(ExpectedIdentifiers[index], scheme.Identifier);
            Assert.AreSame(LightIdentifiers.Contains(scheme.Identifier) ? ColorSchemeAppearance.Light : ColorSchemeAppearance.Dark, scheme.Appearance);
            Assert.AreSame(scheme, Assert.IsInstanceOfType<ColorSchemeAccepted>(ColorScheme.Parse(scheme.Identifier)).Scheme);
        }
    }

    /// <summary>Proves no mutable list or synchronization-root contract can disclose the owned array.</summary>
    [TestMethod]
    public void AllWhenReadThroughCollectionContractsKeepsStoragePrivateAndBoundsExact()
    {
        Assert.IsFalse(ColorScheme.All is ICollection);
        Assert.IsFalse(ColorScheme.All is IList<ColorScheme>);
        IEnumerator iterator = ((IEnumerable)ColorScheme.All).GetEnumerator();
        int index = 0;
        while (iterator.MoveNext())
        {
            Assert.AreSame(ColorScheme.All[index], iterator.Current);
            index++;
        }
        Assert.AreEqual(129, index);
        _ = Assert.ThrowsExactly<IndexOutOfRangeException>(() => ColorScheme.All[-1]);
        _ = Assert.ThrowsExactly<IndexOutOfRangeException>(() => ColorScheme.All[129]);
    }

    /// <summary>Proves known record copies preserve values without adding cases or changing parsing identity.</summary>
    [TestMethod]
    public void CopyWhenSchemeIsKnownPreservesEqualityHashAndClosedValues()
    {
        foreach (ColorScheme scheme in ColorScheme.All)
        {
            ColorScheme copy = scheme with { };
            Assert.AreNotSame(scheme, copy);
            Assert.AreEqual(scheme, copy);
            Assert.AreEqual(scheme.GetHashCode(), copy.GetHashCode());
            Assert.AreSame(scheme.Appearance, copy.Appearance);
            Assert.AreSame(scheme, Assert.IsInstanceOfType<ColorSchemeAccepted>(ColorScheme.Parse(copy.Identifier)).Scheme);
        }
        Assert.AreNotEqual(ColorScheme.NeNeDark, ColorScheme.Ubuntu);
    }

    /// <summary>Proves catalog growth admits no aliases, normalization, paths or executable resource text.</summary>
    [TestMethod]
    [DataRow("DRACULA")]
    [DataRow("dracula ")]
    [DataRow("\u0000dracula")]
    [DataRow("ms-appx:///Themes/Schemes/dracula.xaml")]
    [DataRow("../dracula")]
    [DataRow("not-a-shipped-theme")]
    public void ParseWhenIdentifierIsNotExactRejectsInsteadOfLoading(string identifier)
    {
        Assert.AreSame(ColorSchemeFailureKind.Unknown, Assert.IsInstanceOfType<ColorSchemeRejected>(ColorScheme.Parse(identifier)).Kind);
    }
}
