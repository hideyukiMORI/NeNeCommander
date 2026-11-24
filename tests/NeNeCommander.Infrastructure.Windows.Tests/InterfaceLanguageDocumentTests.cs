using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Settings;
using NeNeCommander.Infrastructure.Windows.Settings;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the strict next-launch language document boundary.</summary>
[TestClass]
public sealed class InterfaceLanguageDocumentTests
{
    /// <summary>Proves every supported language is accepted as part of the whole version-five document.</summary>
    [TestMethod]
    [DataRow("system")]
    [DataRow("ja-JP")]
    [DataRow("en-US")]
    [DataRow("zh-Hans")]
    public void ValidateWhenVersionFiveContainsApprovedLanguageAcceptsCompleteSettings(string identifier)
    {
        string document = Document("\"uiLanguage\":\"" + identifier + "\"");

        UserSettings settings = Assert.IsInstanceOfType<SettingsRead>(SettingsDocumentValidator.Validate(document)).Settings;

        Assert.AreEqual(identifier, settings.Display.InterfaceLanguage.Identifier);
        Assert.AreSame(ColorScheme.Ubuntu, settings.Display.ColorScheme);
        Assert.AreSame(HiddenItemVisibility.Shown, settings.Display.HiddenItemVisibility);
        Assert.AreSame(KeyHintVisibility.Hidden, settings.Display.KeyHintVisibility);
    }

    /// <summary>Proves identifiers are exact and cannot widen the shipped language set.</summary>
    [TestMethod]
    [TestProperty("ThreatId", "ADV-012")]
    [TestCategory("Adversarial")]
    [DataRow("")]
    [DataRow("SYSTEM")]
    [DataRow("JA-jp")]
    [DataRow("ja")]
    [DataRow("zh-CN")]
    [DataRow("zh-Hant")]
    [DataRow("fr-FR")]
    [DataRow(" en-US")]
    [DataRow("en-US ")]
    public void ValidateWhenLanguageIsUnknownRejectsWholeDocument(string identifier)
    {
        SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(
            SettingsDocumentValidator.Validate(Document("\"uiLanguage\":\"" + identifier + "\"")));

        Assert.AreSame(SettingsReadFailureKind.UnknownInterfaceLanguage, rejected.Kind);
    }

    /// <summary>Proves JSON types never coerce into a language.</summary>
    [TestMethod]
    [TestProperty("ThreatId", "ADV-012")]
    [TestCategory("Adversarial")]
    [DataRow("null")]
    [DataRow("0")]
    [DataRow("true")]
    [DataRow("{}")]
    [DataRow("[]")]
    public void ValidateWhenLanguageHasWrongTypeRejectsMalformed(string value)
    {
        SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(
            SettingsDocumentValidator.Validate(Document("\"uiLanguage\":" + value)));

        Assert.AreSame(SettingsReadFailureKind.Malformed, rejected.Kind);
    }

    /// <summary>Proves missing language cannot default within schema five.</summary>
    [TestMethod]
    public void ValidateWhenCurrentLanguageIsMissingRejectsIncomplete()
    {
        SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(
            SettingsDocumentValidator.Validate(Document(string.Empty)));

        Assert.AreSame(SettingsReadFailureKind.Incomplete, rejected.Kind);
    }

    /// <summary>Proves case, duplicate and extra-property rules precede language parsing.</summary>
    [TestMethod]
    [TestProperty("ThreatId", "ADV-012")]
    [TestCategory("Adversarial")]
    [DataRow("\"UiLanguage\":\"system\"")]
    [DataRow("\"uiLanguage\":\"system\",\"uiLanguage\":\"system\"")]
    [DataRow("\"uiLanguage\":\"system\",\"uiLanguage\":\"ja-JP\"")]
    [DataRow("\"uiLanguage\":\"unknown\",\"extra\":0")]
    public void ValidateWhenLanguagePropertyShapeIsInvalidRejectsUnexpectedProperty(string properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(
            SettingsDocumentValidator.Validate(Document(properties)));

        Assert.AreSame(SettingsReadFailureKind.UnexpectedProperty, rejected.Kind);
    }

    /// <summary>Proves the byte bound is applied before interpretation, including multibyte values.</summary>
    [TestMethod]
    public void ValidateWhenLanguageMakesUtf8OversizedRejectsTooLarge()
    {
        string document = Document("\"uiLanguage\":\"" + new string('語', 22000) + "\"");
        Assert.IsLessThan(SettingsDocumentValidator.MaximumDocumentLength, document.Length);
        Assert.IsGreaterThan(SettingsDocumentValidator.MaximumDocumentLength, Encoding.UTF8.GetByteCount(document));

        SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(SettingsDocumentValidator.Validate(document));

        Assert.AreSame(SettingsReadFailureKind.TooLarge, rejected.Kind);
    }

    private static string Document(string properties)
    {
        const string order = "{\"key\":\"name\",\"direction\":\"ascending\"}";
        string prefix = "{\"schemaVersion\":5,\"showHiddenItems\":true,\"colorScheme\":\"ubuntu\"," +
            "\"bookmarkCategories\":[],\"bookmarks\":[],\"paneSortOrders\":{\"left\":" + order + ",\"right\":" + order + "}," +
            "\"showKeyHints\":false";
        return properties.Length == 0 ? prefix + "}" : prefix + "," + properties + "}";
    }
}
