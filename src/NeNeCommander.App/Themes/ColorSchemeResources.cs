using System;
using Microsoft.UI.Xaml;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.App.Themes;

/// <summary>
/// Projects one approved color scheme onto the framework resources the host merges. The scheme
/// dictionary is chosen by an exhaustive closed mapping, so persisted text never composes a
/// resource address and an unmapped scheme fails closed instead of loading nothing.
/// </summary>
public static class ColorSchemeResources
{
    /// <summary>Resolves the packaged resource address of the scheme's dictionary.</summary>
    /// <param name="scheme">Approved color scheme.</param>
    /// <returns>The absolute packaged address of the scheme dictionary.</returns>
    /// <exception cref="InvalidOperationException">The scheme has no shipped dictionary, which is a defect.</exception>
    public static Uri ResolveDictionaryAddress(ColorScheme scheme)
    {
        ArgumentNullException.ThrowIfNull(scheme);
        return scheme.Identifier switch
        {
            "nene-dark" => new Uri("ms-appx:///Themes/Schemes/nene-dark.xaml"),
            "ubuntu" => new Uri("ms-appx:///Themes/Schemes/ubuntu.xaml"),
            "monokai" => new Uri("ms-appx:///Themes/Schemes/monokai.xaml"),
            "solarized-dark" => new Uri("ms-appx:///Themes/Schemes/solarized-dark.xaml"),
            "solarized-light" => new Uri("ms-appx:///Themes/Schemes/solarized-light.xaml"),
            "dracula" => new Uri("ms-appx:///Themes/Schemes/dracula.xaml"),
            "nene-black" => new Uri("ms-appx:///Themes/Schemes/nene-black.xaml"),
            "nene-light" => new Uri("ms-appx:///Themes/Schemes/nene-light.xaml"),
            _ => throw new InvalidOperationException("The color scheme has no shipped resource dictionary."),
        };
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
