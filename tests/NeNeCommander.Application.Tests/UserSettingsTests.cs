using Microsoft.VisualStudio.TestTools.UnitTesting;
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

        Assert.AreSame(ColorScheme.NeNeDark, settings.ColorScheme);
        Assert.AreSame(HiddenItemVisibility.Hidden, settings.HiddenItemVisibility);
    }

    /// <summary>Proves created settings expose exactly the validated components they were given.</summary>
    [TestMethod]
    public void CreateWhenComponentsAreValidatedExposesThemUnchanged()
    {
        UserSettings settings = UserSettings.Create(ColorScheme.Dracula, HiddenItemVisibility.Shown);

        Assert.AreSame(ColorScheme.Dracula, settings.ColorScheme);
        Assert.AreSame(HiddenItemVisibility.Shown, settings.HiddenItemVisibility);
        Assert.AreNotEqual(UserSettings.Default, settings);
    }

    /// <summary>Proves the read outcome carries settings, absence, and rejection as separate closed variants.</summary>
    [TestMethod]
    public void ReadOutcomeWhenCreatedCarriesExactlyOneClosedVariant()
    {
        UserSettings settings = UserSettings.Create(ColorScheme.NeNeLight, HiddenItemVisibility.Hidden);

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
