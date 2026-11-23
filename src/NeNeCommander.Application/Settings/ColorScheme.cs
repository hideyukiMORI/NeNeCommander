using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NeNeCommander.Application.Settings;

/// <summary>
/// Identifies one approved color scheme. The set is closed: a scheme exists only when the
/// application ships a resource dictionary for it, so persisted text can never widen the set.
/// </summary>
public abstract record ColorScheme
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets every approved scheme in its documented order, without exposing storage.</summary>
    public static IReadOnlyList<ColorScheme> All { get; } = SchemeCatalog.Create();

    /// <summary>Gets the default dark scheme used when no valid persisted scheme exists.</summary>
    public static ColorScheme NeNeDark => Known("nene-dark");

    /// <summary>Gets the Ubuntu terminal scheme.</summary>
    public static ColorScheme Ubuntu => Known("ubuntu");

    /// <summary>Gets the Monokai scheme.</summary>
    public static ColorScheme Monokai => Known("monokai");

    /// <summary>Gets the dark Solarized scheme.</summary>
    public static ColorScheme SolarizedDark => Known("solarized-dark");

    /// <summary>Gets the light Solarized scheme.</summary>
    public static ColorScheme SolarizedLight => Known("solarized-light");

    /// <summary>Gets the Dracula scheme.</summary>
    public static ColorScheme Dracula => Known("dracula");

    /// <summary>Gets the pure-black variant of the default scheme.</summary>
    public static ColorScheme NeNeBlack => Known("nene-black");

    /// <summary>Gets the light variant of the default scheme.</summary>
    public static ColorScheme NeNeLight => Known("nene-light");

    private ColorScheme()
    {
    }

    /// <summary>Gets the persisted identifier of the scheme, which also names its resource dictionary.</summary>
    public abstract string Identifier { get; }

    /// <summary>Gets the closed appearance the framework-provided controls must follow.</summary>
    public abstract ColorSchemeAppearance Appearance { get; }

    /// <summary>
    /// Parses untrusted persisted scheme text exactly once at the settings boundary.
    /// </summary>
    /// <param name="identifier">Untrusted persisted identifier, or <see langword="null"/> when absent.</param>
    /// <returns>The accepted scheme or a typed rejection; unknown text never falls back silently.</returns>
    public static ColorSchemeParseOutcome Parse(string? identifier)
    {
        if (identifier is null)
        {
            return new ColorSchemeRejected(ColorSchemeFailureKind.Absent);
        }

        ColorScheme? scheme = All.FirstOrDefault(candidate =>
            string.Equals(candidate.Identifier, identifier, StringComparison.Ordinal));
        return scheme is null
            ? new ColorSchemeRejected(ColorSchemeFailureKind.Unknown)
            : new ColorSchemeAccepted(scheme);
    }

    private static ColorScheme Known(string identifier)
    {
        return All.Single(scheme => string.Equals(scheme.Identifier, identifier, StringComparison.Ordinal));
    }
    private sealed record SchemeCatalog : IReadOnlyList<ColorScheme>
    {
        private readonly ColorScheme[] _schemes =
        [
            new CatalogScheme("nene-dark", ColorSchemeAppearance.Dark),
            new CatalogScheme("ubuntu", ColorSchemeAppearance.Dark),
            new CatalogScheme("nene-black", ColorSchemeAppearance.Dark),
            new CatalogScheme("nene-light", ColorSchemeAppearance.Light),
            new CatalogScheme("ubuntu-aubergine", ColorSchemeAppearance.Dark),
            new CatalogScheme("neutral-light", ColorSchemeAppearance.Light),
            new CatalogScheme("solarized-dark", ColorSchemeAppearance.Dark),
            new CatalogScheme("solarized-light", ColorSchemeAppearance.Light),
            new CatalogScheme("monokai", ColorSchemeAppearance.Dark),
            new CatalogScheme("dracula", ColorSchemeAppearance.Dark),
            new CatalogScheme("one-dark", ColorSchemeAppearance.Dark),
            new CatalogScheme("night-owl", ColorSchemeAppearance.Dark),
            new CatalogScheme("night-owl-light", ColorSchemeAppearance.Light),
            new CatalogScheme("blue", ColorSchemeAppearance.Dark),
            new CatalogScheme("catppuccin", ColorSchemeAppearance.Dark),
            new CatalogScheme("darkblue", ColorSchemeAppearance.Dark),
            new CatalogScheme("default", ColorSchemeAppearance.Dark),
            new CatalogScheme("delek", ColorSchemeAppearance.Light),
            new CatalogScheme("desert", ColorSchemeAppearance.Dark),
            new CatalogScheme("elflord", ColorSchemeAppearance.Dark),
            new CatalogScheme("evening", ColorSchemeAppearance.Dark),
            new CatalogScheme("habamax", ColorSchemeAppearance.Dark),
            new CatalogScheme("industry", ColorSchemeAppearance.Dark),
            new CatalogScheme("koehler", ColorSchemeAppearance.Dark),
            new CatalogScheme("lunaperche", ColorSchemeAppearance.Dark),
            new CatalogScheme("morning", ColorSchemeAppearance.Light),
            new CatalogScheme("murphy", ColorSchemeAppearance.Dark),
            new CatalogScheme("novum", ColorSchemeAppearance.Dark),
            new CatalogScheme("pablo", ColorSchemeAppearance.Dark),
            new CatalogScheme("peachpuff", ColorSchemeAppearance.Light),
            new CatalogScheme("quiet", ColorSchemeAppearance.Dark),
            new CatalogScheme("retrobox", ColorSchemeAppearance.Dark),
            new CatalogScheme("ron", ColorSchemeAppearance.Dark),
            new CatalogScheme("shine", ColorSchemeAppearance.Light),
            new CatalogScheme("slate", ColorSchemeAppearance.Dark),
            new CatalogScheme("sorbet", ColorSchemeAppearance.Dark),
            new CatalogScheme("torte", ColorSchemeAppearance.Dark),
            new CatalogScheme("unokai", ColorSchemeAppearance.Dark),
            new CatalogScheme("wildcharm", ColorSchemeAppearance.Dark),
            new CatalogScheme("zaibatsu", ColorSchemeAppearance.Dark),
            new CatalogScheme("zellner", ColorSchemeAppearance.Light),
            new CatalogScheme("abyss", ColorSchemeAppearance.Dark),
            new CatalogScheme("vscode-dark-2026", ColorSchemeAppearance.Dark),
            new CatalogScheme("vscode-light-2026", ColorSchemeAppearance.Light),
            new CatalogScheme("vscode-dark-modern", ColorSchemeAppearance.Dark),
            new CatalogScheme("vscode-dark-plus", ColorSchemeAppearance.Dark),
            new CatalogScheme("vscode-dark", ColorSchemeAppearance.Dark),
            new CatalogScheme("vscode-high-contrast", ColorSchemeAppearance.Dark),
            new CatalogScheme("vscode-high-contrast-light", ColorSchemeAppearance.Light),
            new CatalogScheme("vscode-light-modern", ColorSchemeAppearance.Light),
            new CatalogScheme("vscode-light-plus", ColorSchemeAppearance.Light),
            new CatalogScheme("vscode-light", ColorSchemeAppearance.Light),
            new CatalogScheme("kimbie-dark", ColorSchemeAppearance.Dark),
            new CatalogScheme("monokai-dimmed", ColorSchemeAppearance.Dark),
            new CatalogScheme("quiet-light", ColorSchemeAppearance.Light),
            new CatalogScheme("red", ColorSchemeAppearance.Dark),
            new CatalogScheme("tomorrow-night-blue", ColorSchemeAppearance.Dark),
            new CatalogScheme("monalisa", ColorSchemeAppearance.Dark),
            new CatalogScheme("catppuccin-latte", ColorSchemeAppearance.Light),
            new CatalogScheme("catppuccin-frappe", ColorSchemeAppearance.Dark),
            new CatalogScheme("catppuccin-macchiato", ColorSchemeAppearance.Dark),
            new CatalogScheme("nord", ColorSchemeAppearance.Dark),
            new CatalogScheme("gruvbox", ColorSchemeAppearance.Dark),
            new CatalogScheme("gruvbox-light", ColorSchemeAppearance.Light),
            new CatalogScheme("tokyo-night", ColorSchemeAppearance.Dark),
            new CatalogScheme("tokyo-night-storm", ColorSchemeAppearance.Dark),
            new CatalogScheme("tokyo-night-moon", ColorSchemeAppearance.Dark),
            new CatalogScheme("tokyo-night-light", ColorSchemeAppearance.Light),
            new CatalogScheme("github-dark", ColorSchemeAppearance.Dark),
            new CatalogScheme("github-light", ColorSchemeAppearance.Light),
            new CatalogScheme("github-dark-dimmed", ColorSchemeAppearance.Dark),
            new CatalogScheme("one-light", ColorSchemeAppearance.Light),
            new CatalogScheme("intellij-light", ColorSchemeAppearance.Light),
            new CatalogScheme("darcula", ColorSchemeAppearance.Dark),
            new CatalogScheme("rose-pine", ColorSchemeAppearance.Dark),
            new CatalogScheme("rose-pine-moon", ColorSchemeAppearance.Dark),
            new CatalogScheme("rose-pine-dawn", ColorSchemeAppearance.Light),
            new CatalogScheme("everforest-dark", ColorSchemeAppearance.Dark),
            new CatalogScheme("everforest-light", ColorSchemeAppearance.Light),
            new CatalogScheme("kanagawa", ColorSchemeAppearance.Dark),
            new CatalogScheme("kanagawa-dragon", ColorSchemeAppearance.Dark),
            new CatalogScheme("kanagawa-lotus", ColorSchemeAppearance.Light),
            new CatalogScheme("material-oceanic", ColorSchemeAppearance.Dark),
            new CatalogScheme("material-darker", ColorSchemeAppearance.Dark),
            new CatalogScheme("material-lighter", ColorSchemeAppearance.Light),
            new CatalogScheme("material-palenight", ColorSchemeAppearance.Dark),
            new CatalogScheme("material-deep-ocean", ColorSchemeAppearance.Dark),
            new CatalogScheme("ayu-dark", ColorSchemeAppearance.Dark),
            new CatalogScheme("ayu-mirage", ColorSchemeAppearance.Dark),
            new CatalogScheme("ayu-light", ColorSchemeAppearance.Light),
            new CatalogScheme("cobalt2", ColorSchemeAppearance.Dark),
            new CatalogScheme("synthwave-84", ColorSchemeAppearance.Dark),
            new CatalogScheme("alloy-core", ColorSchemeAppearance.Dark),
            new CatalogScheme("verdant-rage", ColorSchemeAppearance.Dark),
            new CatalogScheme("scarlet-web", ColorSchemeAppearance.Dark),
            new CatalogScheme("relic-sand", ColorSchemeAppearance.Light),
            new CatalogScheme("soap-rebellion", ColorSchemeAppearance.Dark),
            new CatalogScheme("code-rain", ColorSchemeAppearance.Dark),
            new CatalogScheme("arcane-library", ColorSchemeAppearance.Dark),
            new CatalogScheme("royal-kinetic", ColorSchemeAppearance.Dark),
            new CatalogScheme("arena-sunset", ColorSchemeAppearance.Dark),
            new CatalogScheme("cosmic-mixtape", ColorSchemeAppearance.Dark),
            new CatalogScheme("prism-sanctum", ColorSchemeAppearance.Dark),
            new CatalogScheme("winter-sentinel", ColorSchemeAppearance.Dark),
            new CatalogScheme("infinite-dusk", ColorSchemeAppearance.Dark),
            new CatalogScheme("stellar-pilot", ColorSchemeAppearance.Dark),
            new CatalogScheme("quantum-honey", ColorSchemeAppearance.Dark),
            new CatalogScheme("crimson-mercenary", ColorSchemeAppearance.Dark),
            new CatalogScheme("last-outlaw", ColorSchemeAppearance.Dark),
            new CatalogScheme("ivory-agent", ColorSchemeAppearance.Light),
            new CatalogScheme("celestial-brass", ColorSchemeAppearance.Light),
            new CatalogScheme("amber-haze", ColorSchemeAppearance.Dark),
            new CatalogScheme("desert-oracle", ColorSchemeAppearance.Light),
            new CatalogScheme("chrome-wasteland", ColorSchemeAppearance.Dark),
            new CatalogScheme("neon-grid", ColorSchemeAppearance.Dark),
            new CatalogScheme("biolume-forest", ColorSchemeAppearance.Dark),
            new CatalogScheme("pastel-concierge", ColorSchemeAppearance.Light),
            new CatalogScheme("twilight-jazz", ColorSchemeAppearance.Dark),
            new CatalogScheme("noir-vigil", ColorSchemeAppearance.Dark),
            new CatalogScheme("carnival-ashes", ColorSchemeAppearance.Dark),
            new CatalogScheme("orbital-frost", ColorSchemeAppearance.Light),
            new CatalogScheme("folded-city", ColorSchemeAppearance.Dark),
            new CatalogScheme("midnight-drive", ColorSchemeAppearance.Dark),
            new CatalogScheme("editorial-cafe", ColorSchemeAppearance.Light),
            new CatalogScheme("striped-haunt", ColorSchemeAppearance.Light),
            new CatalogScheme("curious-garden", ColorSchemeAppearance.Light),
            new CatalogScheme("tidebound-gold", ColorSchemeAppearance.Dark),
            new CatalogScheme("velvet-cabaret", ColorSchemeAppearance.Dark),
            new CatalogScheme("lagoon-whisper", ColorSchemeAppearance.Dark),
        ];
        private SchemeCatalog()
        {
        }
        public int Count => _schemes.Length;
        public ColorScheme this[int index] => _schemes[index];
        internal static SchemeCatalog Create()
        {
            return new SchemeCatalog();
        }
        public IEnumerator<ColorScheme> GetEnumerator()
        {
            return ((IEnumerable<ColorScheme>)_schemes).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    private sealed record CatalogScheme : ColorScheme
    {
        internal CatalogScheme(string identifier, ColorSchemeAppearance appearance)
        {
            Identifier = identifier;
            Appearance = appearance;
        }
        public override string Identifier { get; }
        public override ColorSchemeAppearance Appearance { get; }
        private protected override void RestrictDerivation()
        {
        }
    }
}
