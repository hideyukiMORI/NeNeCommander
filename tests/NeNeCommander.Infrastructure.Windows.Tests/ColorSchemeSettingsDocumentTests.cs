using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;
using NeNeCommander.Infrastructure.Windows.Settings;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves expanded scheme values use the existing strict settings document mechanism.</summary>
[TestClass]
public sealed class ColorSchemeSettingsDocumentTests
{
    /// <summary>Proves every shipped identifier round trips with language and both independent sort orders.</summary>
    [TestMethod]
    public void SerializeWhenSchemeIsShippedRoundTripsCompleteCurrentSettings()
    {
        PaneSortPreferences sorts = PaneSortPreferences.Create(
            PaneSortOrder.Create(SortKey.Size, SortDirection.Descending),
            PaneSortOrder.Create(SortKey.Modified, SortDirection.Ascending));
        foreach (ColorScheme scheme in ColorScheme.All)
        {
            UserSettings source = UserSettings.Create(
                DisplayPreferences.Create(scheme, HiddenItemVisibility.Shown, KeyHintVisibility.Hidden, InterfaceLanguage.SimplifiedChinese),
                BookmarkCatalog.Empty, sorts, null);
            byte[] bytes = SettingsDocumentSerializer.Serialize(source);
            string document = Encoding.UTF8.GetString(bytes);
            UserSettings parsed = Assert.IsInstanceOfType<SettingsRead>(SettingsDocumentValidator.Validate(document)).Settings;
            StringAssert.StartsWith(document, "{\"schemaVersion\":6,");
            Assert.AreSame(scheme, parsed.Display.ColorScheme);
            Assert.AreEqual(source.Display, parsed.Display);
            Assert.AreEqual(sorts, parsed.PaneSortOrders);
            Assert.IsEmpty(parsed.Bookmarks.Bookmarks);
            CollectionAssert.AreEqual(bytes, SettingsDocumentSerializer.Serialize(parsed));
        }
    }

    /// <summary>Proves the eight historical stored identifiers remain readable in every earlier schema.</summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    public void ValidateWhenLegacyDocumentContainsExistingSchemeRetainsItsCanonicalValue(int version)
    {
        ColorScheme[] existing = [ColorScheme.NeNeDark, ColorScheme.Ubuntu, ColorScheme.Monokai, ColorScheme.SolarizedDark, ColorScheme.SolarizedLight, ColorScheme.Dracula, ColorScheme.NeNeBlack, ColorScheme.NeNeLight];
        foreach (ColorScheme scheme in existing)
        {
            UserSettings settings = Assert.IsInstanceOfType<SettingsRead>(SettingsDocumentValidator.Validate(Legacy(version, scheme.Identifier))).Settings;
            Assert.AreSame(scheme, settings.Display.ColorScheme);
            Assert.AreSame(InterfaceLanguage.System, settings.Display.InterfaceLanguage);
            Assert.AreSame(HiddenItemVisibility.Shown, settings.Display.HiddenItemVisibility);
        }
    }

    /// <summary>Proves hostile and differently cased scheme text rejects the whole document.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    [DataRow("DRACULA")]
    [DataRow("dracula ")]
    [DataRow("../dracula")]
    [DataRow("ms-appx:///Themes/Schemes/dracula.xaml")]
    [DataRow("new-unreviewed-theme")]
    public void ValidateWhenSchemeIsUnknownRejectsInsteadOfFallingBack(string identifier)
    {
        SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(SettingsDocumentValidator.Validate(Legacy(4, identifier)));
        Assert.AreSame(SettingsReadFailureKind.UnknownColorScheme, rejected.Kind);
    }

    private static string Legacy(int version, string identifier)
    {
        string document = "{\"schemaVersion\":" + version + ",\"showHiddenItems\":true,\"colorScheme\":\"" + identifier + "\"";
        if (version >= 2) { document += ",\"bookmarkCategories\":[],\"bookmarks\":[]"; }
        if (version >= 3)
        {
            const string order = "{\"key\":\"name\",\"direction\":\"ascending\"}";
            document += ",\"paneSortOrders\":{\"left\":" + order + ",\"right\":" + order + "}";
        }
        if (version >= 4) { document += ",\"showKeyHints\":true"; }
        return document + "}";
    }
}
