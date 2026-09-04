using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.App.Themes;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Architecture.Tests;

/// <summary>
/// Proves the composition root maps every approved color scheme onto exactly one shipped
/// resource dictionary and onto the framework element theme its appearance requires.
/// </summary>
[TestClass]
public sealed class ColorSchemeResourceTests
{
    /// <summary>Proves each scheme resolves to its own dictionary named after its identifier.</summary>
    [TestMethod]
    public void ResolveDictionaryAddressWhenSchemeIsApprovedNamesItsOwnPackagedDictionary()
    {
        HashSet<string> addresses = [];
        foreach (ColorScheme scheme in ColorScheme.All)
        {
            Uri address = ColorSchemeResources.ResolveDictionaryAddress(scheme);

            Assert.IsTrue(address.IsAbsoluteUri, scheme.Identifier);
            Assert.AreEqual("ms-appx:///Themes/Schemes/" + scheme.Identifier + ".xaml", address.OriginalString);
            Assert.IsTrue(addresses.Add(address.OriginalString), scheme.Identifier);
        }

        Assert.HasCount(8, addresses);
    }

    /// <summary>Proves the closed appearance selects the matching framework element theme.</summary>
    [TestMethod]
    public void ResolveElementThemeWhenAppearanceIsClosedSelectsTheMatchingFrameworkTheme()
    {
        Assert.AreEqual(ElementTheme.Dark, ColorSchemeResources.ResolveElementTheme(ColorSchemeAppearance.Dark));
        Assert.AreEqual(ElementTheme.Light, ColorSchemeResources.ResolveElementTheme(ColorSchemeAppearance.Light));
    }

    /// <summary>Proves absent arguments are rejected before any resource address is composed.</summary>
    [TestMethod]
    public void ResolveWhenArgumentIsNullThrowsArgumentNullException()
    {
        AssertNullGuard(nameof(ColorSchemeResources.ResolveDictionaryAddress));
        AssertNullGuard(nameof(ColorSchemeResources.ResolveElementTheme));
    }

    private static void AssertNullGuard(string methodName)
    {
        MethodInfo method = typeof(ColorSchemeResources).GetMethod(methodName) ??
            throw new AssertFailedException("The public resolver was not found.");

        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => method.Invoke(null, [null]));

        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }
}
