using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves the closed color-scheme set parses persisted text without widening itself.</summary>
[TestClass]
public sealed class ColorSchemeTests
{
    /// <summary>Proves every approved identifier parses to its own scheme with its documented appearance.</summary>
    [TestMethod]
    public void ParseWhenIdentifierIsApprovedReturnsThatSchemeWithItsAppearance()
    {
        AssertScheme("nene-dark", ColorScheme.NeNeDark, ColorSchemeAppearance.Dark);
        AssertScheme("ubuntu", ColorScheme.Ubuntu, ColorSchemeAppearance.Dark);
        AssertScheme("monokai", ColorScheme.Monokai, ColorSchemeAppearance.Dark);
        AssertScheme("solarized-dark", ColorScheme.SolarizedDark, ColorSchemeAppearance.Dark);
        AssertScheme("solarized-light", ColorScheme.SolarizedLight, ColorSchemeAppearance.Light);
        AssertScheme("dracula", ColorScheme.Dracula, ColorSchemeAppearance.Dark);
        AssertScheme("nene-black", ColorScheme.NeNeBlack, ColorSchemeAppearance.Dark);
        AssertScheme("nene-light", ColorScheme.NeNeLight, ColorSchemeAppearance.Light);
    }

    /// <summary>Proves absent and unknown text is rejected instead of falling back to a scheme.</summary>
    [TestMethod]
    public void ParseWhenTextNamesNoApprovedSchemeReturnsExactRejection()
    {
        Assert.AreSame(
            ColorSchemeFailureKind.Absent,
            Assert.IsInstanceOfType<ColorSchemeRejected>(ColorScheme.Parse(null)).Kind);
        Assert.AreSame(
            ColorSchemeFailureKind.Unknown,
            Assert.IsInstanceOfType<ColorSchemeRejected>(ColorScheme.Parse(string.Empty)).Kind);
        Assert.AreSame(
            ColorSchemeFailureKind.Unknown,
            Assert.IsInstanceOfType<ColorSchemeRejected>(ColorScheme.Parse("Nene-Dark")).Kind);
        Assert.AreSame(
            ColorSchemeFailureKind.Unknown,
            Assert.IsInstanceOfType<ColorSchemeRejected>(ColorScheme.Parse(" nene-dark ")).Kind);
        Assert.AreSame(
            ColorSchemeFailureKind.Unknown,
            Assert.IsInstanceOfType<ColorSchemeRejected>(ColorScheme.Parse("../../Themes/Schemes/nene-dark")).Kind);
    }

    /// <summary>Proves the published set is exactly the eight approved schemes with unique identifiers.</summary>
    [TestMethod]
    public void AllWhenEnumeratedContainsEveryApprovedSchemeExactlyOnce()
    {
        IReadOnlyList<ColorScheme> schemes = ColorScheme.All;

        Assert.HasCount(8, schemes);
        HashSet<string> identifiers = [];
        foreach (ColorScheme scheme in schemes)
        {
            Assert.IsTrue(identifiers.Add(scheme.Identifier), scheme.Identifier);
            Assert.AreSame(
                scheme,
                Assert.IsInstanceOfType<ColorSchemeAccepted>(ColorScheme.Parse(scheme.Identifier)).Scheme);
        }
    }

    private static void AssertScheme(string identifier, ColorScheme expected, ColorSchemeAppearance appearance)
    {
        ColorSchemeAccepted accepted = Assert.IsInstanceOfType<ColorSchemeAccepted>(ColorScheme.Parse(identifier));

        Assert.AreSame(expected, accepted.Scheme);
        Assert.AreEqual(identifier, accepted.Scheme.Identifier);
        Assert.AreSame(appearance, accepted.Scheme.Appearance);
    }
}
