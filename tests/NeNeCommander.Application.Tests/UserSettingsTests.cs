using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves persisted user settings are constructor-complete and have one default.</summary>
[TestClass]
public sealed class UserSettingsTests
{
    /// <summary>Proves the default settings are the dark scheme with hidden entries omitted.</summary>
    [TestMethod]
    public void DefaultWhenNoDocumentExistsSelectsDarkSchemeAndHiddenEntries()
    {
        UserSettings settings = UserSettings.Default;

        Assert.AreSame(ColorScheme.NeNeDark, settings.Display.ColorScheme);
        Assert.AreSame(HiddenItemVisibility.Hidden, settings.Display.HiddenItemVisibility);
        Assert.AreSame(KeyHintVisibility.Shown, settings.Display.KeyHintVisibility);
        Assert.AreSame(BookmarkCatalog.Empty, settings.Bookmarks);
    }

    /// <summary>Proves created settings expose exactly the validated components they were given.</summary>
    [TestMethod]
    public void CreateWhenComponentsAreValidatedExposesThemUnchanged()
    {
        UserSettings settings = UserSettings.Create(
            DisplayPreferences.Create(ColorScheme.Dracula, HiddenItemVisibility.Shown, KeyHintVisibility.Shown),
            BookmarkCatalog.Empty, PaneSortPreferences.Default);

        Assert.AreSame(ColorScheme.Dracula, settings.Display.ColorScheme);
        Assert.AreSame(HiddenItemVisibility.Shown, settings.Display.HiddenItemVisibility);
        Assert.AreSame(BookmarkCatalog.Empty, settings.Bookmarks);
        Assert.AreNotEqual(UserSettings.Default, settings);
    }

    /// <summary>Proves no missing display choice can create partial preferences.</summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    public void CreateDisplayWhenAnyChoiceIsNullRejectsIncompleteValue(int missingIndex)
    {
        object?[] arguments = [ColorScheme.NeNeDark, HiddenItemVisibility.Hidden, KeyHintVisibility.Shown];
        arguments[missingIndex] = null;
        MethodInfo factory = typeof(DisplayPreferences).GetMethod(nameof(DisplayPreferences.Create))
            ?? throw new AssertFailedException("The display factory was not found.");

        TargetInvocationException exception = Assert.ThrowsExactly<TargetInvocationException>(() => factory.Invoke(null, arguments));

        _ = Assert.IsInstanceOfType<ArgumentNullException>(exception.InnerException);
    }

    /// <summary>Proves the read outcome carries settings, absence, and rejection as separate closed variants.</summary>
    [TestMethod]
    public void ReadOutcomeWhenCreatedCarriesExactlyOneClosedVariant()
    {
        UserSettings settings = UserSettings.Create(
            DisplayPreferences.Create(ColorScheme.NeNeLight, HiddenItemVisibility.Hidden, KeyHintVisibility.Shown),
            BookmarkCatalog.Empty, PaneSortPreferences.Default);

        Assert.AreSame(
            settings,
            Assert.IsInstanceOfType<SettingsRead>(SettingsReadOutcome.Read(settings)).Settings);
        _ = Assert.IsInstanceOfType<SettingsAbsent>(SettingsReadOutcome.Absent());
        Assert.AreSame(
            SettingsReadFailureKind.Unreadable,
            Assert.IsInstanceOfType<SettingsRejected>(
                SettingsReadOutcome.Rejected(SettingsReadFailureKind.Unreadable)).Kind);
    }
}
