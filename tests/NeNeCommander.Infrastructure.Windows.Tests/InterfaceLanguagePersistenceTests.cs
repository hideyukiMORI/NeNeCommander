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

/// <summary>Proves language migration, exact atomic round trips and rejected-byte preservation.</summary>
[TestClass]
public sealed class InterfaceLanguagePersistenceTests
{
    /// <summary>Proves every legacy schema selects System without touching the existing bytes.</summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    public async Task ReadAsyncWhenLegacyDocumentIsValidDefaultsLanguageWithoutWritingAsync(int version)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string legacy = LegacyDocument(version);
        string path = root.WriteFile("settings.json", legacy);

        UserSettings settings = Assert.IsInstanceOfType<SettingsRead>(await Store(root).ReadAsync(CancellationToken.None)).Settings;

        Assert.AreSame(InterfaceLanguage.System, settings.Display.InterfaceLanguage);
        Assert.AreSame(ColorScheme.Ubuntu, settings.Display.ColorScheme);
        Assert.AreEqual(legacy, File.ReadAllText(path));
        Assert.HasCount(1, Directory.GetFiles(root.Path.CanonicalText));
    }

    /// <summary>Proves each choice survives a new store instance through the real atomic writer.</summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    public async Task WriteAsyncWhenLanguageIsSelectedRoundTripsExactChoiceAsync(int index)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        InterfaceLanguage language = InterfaceLanguage.All[index];
        UserSettings selected = UserSettings.Create(
            DisplayPreferences.Create(ColorScheme.Dracula, HiddenItemVisibility.Shown, KeyHintVisibility.Hidden, language),
            BookmarkCatalog.Empty, PaneSortPreferences.Default);

        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(await Store(root).WriteAsync(selected, CancellationToken.None));
        UserSettings restored = Assert.IsInstanceOfType<SettingsRead>(await Store(root).ReadAsync(CancellationToken.None)).Settings;

        Assert.AreSame(language, restored.Display.InterfaceLanguage);
        Assert.AreEqual(selected.Display, restored.Display);
        Assert.AreEqual(selected.PaneSortOrders, restored.PaneSortOrders);
        StringAssert.Contains(File.ReadAllText(root.Resolve("settings.json")), "\"showKeyHints\":false,\"uiLanguage\":\"" + language.Identifier + "\"}");
        Assert.HasCount(1, Directory.GetFiles(root.Path.CanonicalText));
    }

    /// <summary>Proves an invalid language blocks later writes and preserves the original file.</summary>
    [TestMethod]
    [TestProperty("ThreatId", "ADV-012")]
    [TestCategory("Adversarial")]
    public async Task WriteAsyncWhenExistingLanguageIsUnknownPreservesRejectedBytesAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string malformed = LegacyDocument(4).Replace("\"schemaVersion\":4", "\"schemaVersion\":5", StringComparison.Ordinal);
        malformed = malformed[..^1] + ",\"uiLanguage\":\"zh-Hant\"}";
        string path = root.WriteFile("settings.json", malformed);
        WindowsLocalSettingsStore store = Store(root);

        SettingsRejected read = Assert.IsInstanceOfType<SettingsRejected>(await store.ReadAsync(CancellationToken.None));
        SettingsWriteRejected write = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsReadFailureKind.UnknownInterfaceLanguage, read.Kind);
        Assert.AreSame(SettingsWriteFailureKind.ExistingDocumentRejected, write.Failure);
        Assert.AreEqual(malformed, File.ReadAllText(path));
        Assert.HasCount(1, Directory.GetFiles(root.Path.CanonicalText));
    }

    private static WindowsLocalSettingsStore Store(TestOwnedTemporaryRoot root)
    {
        WindowsLocalPath path = Assert.IsInstanceOfType<WindowsLocalPath>(
            Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(root.Resolve("settings.json"))).Path);
        return new WindowsLocalSettingsStore(path, new WindowsLocalIoExecutionBoundary(), new DeterministicIdentifierSource());
    }

    private static string LegacyDocument(int version)
    {
        const string order = "{\"key\":\"name\",\"direction\":\"ascending\"}";
        string prefix = "{\"schemaVersion\":" + version + ",\"showHiddenItems\":true,\"colorScheme\":\"ubuntu\"";
        if (version >= 2)
        {
            prefix += ",\"bookmarkCategories\":[],\"bookmarks\":[]";
        }
        if (version >= 3)
        {
            prefix += ",\"paneSortOrders\":{\"left\":" + order + ",\"right\":" + order + "}";
        }
        return version == 4 ? prefix + ",\"showKeyHints\":false}" : prefix + "}";
    }
}
