using System;
using System.Collections.Generic;

namespace NeNeCommander.Application.Settings;

/// <summary>
/// Identifies one approved color scheme. The set is closed: a scheme exists only when the
/// application ships a resource dictionary for it, so persisted text can never widen the set.
/// </summary>
public abstract record ColorScheme
{
    /// <summary>Gets the default dark scheme used when no valid persisted scheme exists.</summary>
    public static ColorScheme NeNeDark { get; } = new NeNeDarkScheme();

    /// <summary>Gets the Ubuntu terminal scheme.</summary>
    public static ColorScheme Ubuntu { get; } = new UbuntuScheme();

    /// <summary>Gets the Monokai scheme.</summary>
    public static ColorScheme Monokai { get; } = new MonokaiScheme();

    /// <summary>Gets the dark Solarized scheme.</summary>
    public static ColorScheme SolarizedDark { get; } = new SolarizedDarkScheme();

    /// <summary>Gets the light Solarized scheme.</summary>
    public static ColorScheme SolarizedLight { get; } = new SolarizedLightScheme();

    /// <summary>Gets the Dracula scheme.</summary>
    public static ColorScheme Dracula { get; } = new DraculaScheme();

    /// <summary>Gets the pure-black variant of the default scheme.</summary>
    public static ColorScheme NeNeBlack { get; } = new NeNeBlackScheme();

    /// <summary>Gets the light variant of the default scheme.</summary>
    public static ColorScheme NeNeLight { get; } = new NeNeLightScheme();

    /// <summary>Gets every approved scheme in its documented order.</summary>
    public static IReadOnlyList<ColorScheme> All { get; } =
    [
        NeNeDark,
        Ubuntu,
        Monokai,
        SolarizedDark,
        SolarizedLight,
        Dracula,
        NeNeBlack,
        NeNeLight,
    ];

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

        foreach (ColorScheme scheme in All)
        {
            if (string.Equals(scheme.Identifier, identifier, StringComparison.Ordinal))
            {
                return new ColorSchemeAccepted(scheme);
            }
        }

        return new ColorSchemeRejected(ColorSchemeFailureKind.Unknown);
    }

    private sealed record NeNeDarkScheme : ColorScheme
    {
        public override string Identifier => "nene-dark";
        public override ColorSchemeAppearance Appearance => ColorSchemeAppearance.Dark;
    }

    private sealed record UbuntuScheme : ColorScheme
    {
        public override string Identifier => "ubuntu";
        public override ColorSchemeAppearance Appearance => ColorSchemeAppearance.Dark;
    }

    private sealed record MonokaiScheme : ColorScheme
    {
        public override string Identifier => "monokai";
        public override ColorSchemeAppearance Appearance => ColorSchemeAppearance.Dark;
    }

    private sealed record SolarizedDarkScheme : ColorScheme
    {
        public override string Identifier => "solarized-dark";
        public override ColorSchemeAppearance Appearance => ColorSchemeAppearance.Dark;
    }

    private sealed record SolarizedLightScheme : ColorScheme
    {
        public override string Identifier => "solarized-light";
        public override ColorSchemeAppearance Appearance => ColorSchemeAppearance.Light;
    }

    private sealed record DraculaScheme : ColorScheme
    {
        public override string Identifier => "dracula";
        public override ColorSchemeAppearance Appearance => ColorSchemeAppearance.Dark;
    }

    private sealed record NeNeBlackScheme : ColorScheme
    {
        public override string Identifier => "nene-black";
        public override ColorSchemeAppearance Appearance => ColorSchemeAppearance.Dark;
    }

    private sealed record NeNeLightScheme : ColorScheme
    {
        public override string Identifier => "nene-light";
        public override ColorSchemeAppearance Appearance => ColorSchemeAppearance.Light;
    }
}
