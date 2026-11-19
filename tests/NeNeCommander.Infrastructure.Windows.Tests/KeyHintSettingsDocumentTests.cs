using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.Settings;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the complete version-four settings boundary for the footer preference.</summary>
[TestClass]
public sealed class KeyHintSettingsDocumentTests
{
    /// <summary>Proves both closed footer choices are accepted by the current schema.</summary>
    [TestMethod]
    [DataRow("true")]
    [DataRow("false")]
    public void ValidateWhenVersionFourContainsBooleanKeyHintChoiceAcceptsCompleteSettings(string value)
    {
        SettingsReadOutcome outcome = SettingsDocumentValidator.Validate(Document("\"showKeyHints\":" + value));

        UserSettings settings = Assert.IsInstanceOfType<SettingsRead>(outcome).Settings;
        Assert.AreSame(value == "true" ? KeyHintVisibility.Shown : KeyHintVisibility.Hidden, settings.Display.KeyHintVisibility);
    }

    /// <summary>Proves malformed choices never become a partial defaulted settings value.</summary>
    [TestMethod]
    [TestProperty("ThreatId", "ADV-012")]
    [TestCategory("Adversarial")]
    [DataRow("null")]
    [DataRow("0")]
    [DataRow("\"true\"")]
    [DataRow("{}")]
    [DataRow("[]")]
    public void ValidateWhenKeyHintChoiceHasWrongTypeRejectsMalformed(string value)
    {
        SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(
            SettingsDocumentValidator.Validate(Document("\"showKeyHints\":" + value)));

        Assert.AreSame(SettingsReadFailureKind.Malformed, rejected.Kind);
    }

    /// <summary>Proves the new schema requires the preference explicitly.</summary>
    [TestMethod]
    [TestProperty("ThreatId", "ADV-012")]
    [TestCategory("Adversarial")]
    public void ValidateWhenVersionFourOmitsKeyHintChoiceRejectsIncomplete()
    {
        SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(
            SettingsDocumentValidator.Validate(Document(string.Empty)));

        Assert.AreSame(SettingsReadFailureKind.Incomplete, rejected.Kind);
    }

    /// <summary>Proves exact names and one declaration are required at the root.</summary>
    [TestMethod]
    [TestProperty("ThreatId", "ADV-012")]
    [TestCategory("Adversarial")]
    [DataRow("\"showKeyHints\":true,\"showKeyHints\":false")]
    [DataRow("\"showKeyHints\":true,\"showKeyHints\":true")]
    [DataRow("\"ShowKeyHints\":true")]
    [DataRow("\"showKeyHints\":true,\"unexpected\":0")]
    public void ValidateWhenKeyHintPropertyIsDuplicateOrUnknownRejectsUnexpectedProperty(string property)
    {
        ArgumentNullException.ThrowIfNull(property);
        SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(
            SettingsDocumentValidator.Validate(Document(property)));

        Assert.AreSame(SettingsReadFailureKind.UnexpectedProperty, rejected.Kind);
    }

    /// <summary>Proves every supported old version gains visible hints without rewriting its bytes.</summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    public async Task ReadAsyncWhenLegacyVersionIsValidDefaultsHintsWithoutWritingAsync(int version)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string legacy = LegacyDocument(version);
        string path = root.WriteFile("settings.json", legacy);
        WindowsLocalSettingsStore store = Store(root);

        UserSettings settings = Assert.IsInstanceOfType<SettingsRead>(await store.ReadAsync(CancellationToken.None)).Settings;

        Assert.AreSame(KeyHintVisibility.Shown, settings.Display.KeyHintVisibility);
        Assert.AreSame(ColorScheme.Ubuntu, settings.Display.ColorScheme);
        Assert.AreSame(HiddenItemVisibility.Shown, settings.Display.HiddenItemVisibility);
        Assert.AreEqual(legacy, File.ReadAllText(path));
    }

    /// <summary>Proves either choice survives the real atomic writer and a fresh store instance.</summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public async Task WriteAsyncWhenKeyHintChoiceChangesRoundTripsCurrentSchemaAsync(int choice)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        KeyHintVisibility visibility = choice == 0 ? KeyHintVisibility.Shown : KeyHintVisibility.Hidden;
        UserSettings selected = UserSettings.Create(
            DisplayPreferences.Create(ColorScheme.Dracula, HiddenItemVisibility.Shown, visibility),
            BookmarkCatalog.Empty, PaneSortPreferences.Default);

        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(await Store(root).WriteAsync(selected, CancellationToken.None));
        UserSettings reloaded = Assert.IsInstanceOfType<SettingsRead>(await Store(root).ReadAsync(CancellationToken.None)).Settings;

        Assert.AreEqual(selected.Display, reloaded.Display);
        Assert.AreEqual(selected.PaneSortOrders, reloaded.PaneSortOrders);
        string expected = Document("\"showKeyHints\":" + (choice == 0 ? "true" : "false"))
            .Replace("\"showHiddenItems\":false", "\"showHiddenItems\":true", StringComparison.Ordinal)
            .Replace("nene-dark", "dracula", StringComparison.Ordinal);
        Assert.AreEqual(expected, File.ReadAllText(root.Resolve("settings.json")));
    }

    /// <summary>Proves a malformed current preference is preserved even when a writer requests defaults.</summary>
    [TestMethod]
    [TestProperty("ThreatId", "ADV-012")]
    [TestCategory("Adversarial")]
    public async Task WriteAsyncWhenCurrentDocumentHasInvalidChoicePreservesRejectedBytesAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string malformed = Document("\"showKeyHints\":null");
        string path = root.WriteFile("settings.json", malformed);
        WindowsLocalSettingsStore store = Store(root);

        _ = Assert.IsInstanceOfType<SettingsRejected>(await store.ReadAsync(CancellationToken.None));
        SettingsWriteRejected result = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.ExistingDocumentRejected, result.Failure);
        Assert.AreEqual(malformed, File.ReadAllText(path));
    }

    private static WindowsLocalSettingsStore Store(TestOwnedTemporaryRoot root)
    {
        WindowsLocalPath path = Assert.IsInstanceOfType<WindowsLocalPath>(
            Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(root.Resolve("settings.json"))).Path);
        return new WindowsLocalSettingsStore(path, new WindowsLocalIoExecutionBoundary(), new DeterministicIdentifierSource());
    }

    private static string LegacyDocument(int version)
    {
        const string preferences = ",\"showHiddenItems\":true,\"colorScheme\":\"ubuntu\"";
        const string catalog = ",\"bookmarkCategories\":[],\"bookmarks\":[]";
        const string order = "{\"key\":\"name\",\"direction\":\"ascending\"}";
        return version == 1 ? "{\"schemaVersion\":1" + preferences + "}"
            : version == 2 ? "{\"schemaVersion\":2" + preferences + catalog + "}"
            : "{\"schemaVersion\":3" + preferences + catalog + ",\"paneSortOrders\":{\"left\":" + order + ",\"right\":" + order + "}}";
    }

    private static string Document(string property)
    {
        const string order = "{\"key\":\"name\",\"direction\":\"ascending\"}";
        string prefix = "{\"schemaVersion\":4,\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"," +
            "\"bookmarkCategories\":[],\"bookmarks\":[],\"paneSortOrders\":{\"left\":" + order + ",\"right\":" + order + "}";
        return property.Length == 0 ? prefix + "}" : prefix + "," + property + "}";
    }
}
