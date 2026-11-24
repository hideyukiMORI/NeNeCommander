using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Settings;
using NeNeCommander.Application.Windowing;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.Settings;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves placement uses the existing atomic document store without migration or rejected-byte loss.</summary>
[TestClass]
public sealed class WindowPlacementPersistenceTests
{
    /// <summary>Proves every old schema reads as no saved placement without modifying its bytes.</summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    public async Task ReadAsyncWhenLegacySchemaExistsDoesNotMigrateOnDiskAsync(int version)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string document = LegacyDocument(version);
        string path = root.WriteFile("settings.json", document);

        UserSettings settings = Assert.IsInstanceOfType<SettingsRead>(await Store(root).ReadAsync(CancellationToken.None)).Settings;

        Assert.IsNull(settings.WindowPlacement);
        Assert.AreEqual(document, File.ReadAllText(path));
        Assert.HasCount(1, Directory.GetFiles(root.Path.CanonicalText));
    }

    /// <summary>Proves both non-minimized states round trip through an independent store instance.</summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public async Task WriteAsyncWhenPlacementIsSavedRoundTripsAllSettingsAsync(int mode)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        UserSettings existing = Assert.IsInstanceOfType<SettingsRead>(SettingsDocumentValidator.Validate(LegacyDocument(5))).Settings;
        SavedWindowPlacement placement = SavedWindowPlacement.Create(WindowBounds.Create(-1500, -20, 1000, 700),
            mode == 0 ? WindowPresenterState.Restored : WindowPresenterState.Maximized);
        UserSettings selected = UserSettings.Create(existing.Display, existing.Bookmarks, existing.PaneSortOrders, placement);

        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(await Store(root).WriteAsync(selected, CancellationToken.None));
        UserSettings restored = Assert.IsInstanceOfType<SettingsRead>(await Store(root).ReadAsync(CancellationToken.None)).Settings;

        Assert.AreEqual(selected.Display, restored.Display);
        Assert.AreEqual(selected.PaneSortOrders, restored.PaneSortOrders);
        Assert.AreEqual(selected.WindowPlacement, restored.WindowPlacement);
        StringAssert.Contains(File.ReadAllText(root.Resolve("settings.json")), "\"schemaVersion\":6");
        Assert.HasCount(1, Directory.GetFiles(root.Path.CanonicalText));
    }

    /// <summary>Proves a malformed saved placement blocks replacement and preserves the exact document.</summary>
    [TestMethod]
    [TestProperty("ThreatId", "ADV-012")]
    [TestCategory("Adversarial")]
    public async Task WriteAsyncWhenExistingPlacementIsMalformedPreservesRejectedBytesAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string document = LegacyDocument(5).Replace("\"schemaVersion\":5", "\"schemaVersion\":6", StringComparison.Ordinal);
        document = document[..^1] + ",\"windowPlacement\":{\"left\":0,\"top\":0,\"width\":0,\"height\":700,\"state\":\"restored\"}}";
        string path = root.WriteFile("settings.json", document);
        WindowsLocalSettingsStore store = Store(root);

        _ = Assert.IsInstanceOfType<SettingsRejected>(await store.ReadAsync(CancellationToken.None));
        SettingsWriteRejected write = Assert.IsInstanceOfType<SettingsWriteRejected>(await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.ExistingDocumentRejected, write.Failure);
        Assert.AreEqual(document, File.ReadAllText(path));
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
        string document = "{\"schemaVersion\":" + version + ",\"showHiddenItems\":true,\"colorScheme\":\"ubuntu\"";
        if (version >= 2)
        {
            document += ",\"bookmarkCategories\":[],\"bookmarks\":[]";
        }
        if (version >= 3)
        {
            document += ",\"paneSortOrders\":{\"left\":" + order + ",\"right\":" + order + "}";
        }
        if (version >= 4)
        {
            document += ",\"showKeyHints\":false";
        }
        return version == 5 ? document + ",\"uiLanguage\":\"ja-JP\"}" : document + "}";
    }
}
