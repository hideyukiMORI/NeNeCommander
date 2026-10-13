using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.Settings;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves strict sort migration and round trips through the existing atomic settings store.</summary>
[TestClass]
public sealed class PaneSortSettingsPersistenceTests
{
    private const string DefaultOrder = "{\"key\":\"name\",\"direction\":\"ascending\"}";

    /// <summary>Proves all eight orders on either side survive the real owned-root atomic write/read path.</summary>
    [TestMethod]
    [DataRow(0, "name", "ascending")]
    [DataRow(1, "name", "descending")]
    [DataRow(2, "extension", "ascending")]
    [DataRow(3, "extension", "descending")]
    [DataRow(4, "size", "ascending")]
    [DataRow(5, "size", "descending")]
    [DataRow(6, "modified", "ascending")]
    [DataRow(7, "modified", "descending")]
    public async Task WriteAsyncWhenBothSortPreferencesAreSetRoundTripsBothAsync(int choice, string expectedKey, string expectedDirection)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalSettingsStore store = Store(root);
        PaneSortPreferences orders = PaneSortPreferences.Create(Order(choice), Order(7 - choice));
        UserSettings selected = UserSettings.Create(ColorScheme.Dracula, HiddenItemVisibility.Shown, Catalog(), orders);

        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(await store.WriteAsync(selected, CancellationToken.None));
        UserSettings restored = Assert.IsInstanceOfType<SettingsRead>(await store.ReadAsync(CancellationToken.None)).Settings;

        StringAssert.Contains(File.ReadAllText(root.Resolve("settings.json")), "\"left\":{\"key\":\"" + expectedKey + "\",\"direction\":\"" + expectedDirection + "\"}");
        Assert.AreEqual(orders, restored.PaneSortOrders);
        Assert.HasCount(1, restored.Bookmarks.Bookmarks);
        Assert.AreEqual("Saved", restored.Bookmarks.Bookmarks[0].Name.Value);
        Assert.AreEqual("C:\\saved", restored.Bookmarks.Bookmarks[0].Path.Value.CanonicalText);
        Assert.AreSame(BookmarkShortcutSlot.One, restored.Bookmarks.Bookmarks[0].ShortcutSlot);
        Assert.AreEqual("Work", restored.Bookmarks.Categories[0].Value);
        Assert.AreSame(ColorScheme.Dracula, restored.ColorScheme);
        Assert.AreSame(HiddenItemVisibility.Shown, restored.HiddenItemVisibility);
        Assert.IsFalse(File.Exists(root.Resolve("settings.json.tmp")));
    }

    /// <summary>Proves valid legacy documents gain defaults in memory and change bytes only on explicit write.</summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    public async Task ReadAsyncWhenLegacySchemaIsValidMigratesWithoutWritingAsync(int version)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string legacy = version == 1
            ? "{\"schemaVersion\":1,\"showHiddenItems\":true,\"colorScheme\":\"ubuntu\"}"
            : "{\"schemaVersion\":2,\"showHiddenItems\":true,\"colorScheme\":\"ubuntu\",\"bookmarkCategories\":[],\"bookmarks\":[]}";
        string path = root.WriteFile("settings.json", legacy);
        WindowsLocalSettingsStore store = Store(root);

        UserSettings migrated = Assert.IsInstanceOfType<SettingsRead>(await store.ReadAsync(CancellationToken.None)).Settings;

        Assert.AreEqual(PaneSortPreferences.Default, migrated.PaneSortOrders);
        Assert.AreEqual(legacy, File.ReadAllText(path));
        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(await store.WriteAsync(migrated, CancellationToken.None));
        Assert.AreEqual(Document("{\"left\":" + DefaultOrder + ",\"right\":" + DefaultOrder + "}"), File.ReadAllText(path));
    }

    /// <summary>Proves a missing new property never silently defaults the incomplete side.</summary>
    [TestMethod]
    [TestProperty("ThreatId", "ADV-012")]
    [TestCategory("Adversarial")]
    [DataRow("{}")]
    [DataRow("{\"left\":{\"key\":\"name\",\"direction\":\"ascending\"}}")]
    [DataRow("{\"left\":{},\"right\":{\"key\":\"name\",\"direction\":\"ascending\"}}")]
    [DataRow("{\"left\":{\"direction\":\"ascending\"},\"right\":{\"key\":\"name\",\"direction\":\"ascending\"}}")]
    [DataRow("{\"left\":{\"key\":\"name\"},\"right\":{\"key\":\"name\",\"direction\":\"ascending\"}}")]
    public void ValidateWhenSortFieldsAreMissingRejectsIncomplete(string input)
    {
        SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(SettingsDocumentValidator.Validate(Document(input)));
        Assert.AreSame(SettingsReadFailureKind.Incomplete, rejected.Kind);
    }

    /// <summary>Proves duplicate and unknown fields are rejected at each new object boundary.</summary>
    [TestMethod]
    [TestProperty("ThreatId", "ADV-012")]
    [TestCategory("Adversarial")]
    [DataRow("{\"left\":{\"key\":\"name\",\"direction\":\"ascending\"},\"right\":{\"key\":\"name\",\"direction\":\"ascending\"},\"extra\":0}")]
    [DataRow("{\"left\":{\"key\":\"name\",\"direction\":\"ascending\"},\"left\":{},\"right\":{\"key\":\"name\",\"direction\":\"ascending\"}}")]
    [DataRow("{\"left\":{\"key\":\"name\",\"key\":\"name\",\"direction\":\"ascending\"},\"right\":{\"key\":\"name\",\"direction\":\"ascending\"}}")]
    [DataRow("{\"left\":{\"key\":\"name\",\"direction\":\"ascending\",\"direction\":\"ascending\"},\"right\":{\"key\":\"name\",\"direction\":\"ascending\"}}")]
    [DataRow("{\"left\":{\"key\":\"name\",\"direction\":\"ascending\",\"extra\":0},\"right\":{\"key\":\"name\",\"direction\":\"ascending\"}}")]
    public void ValidateWhenSortFieldsAreDuplicateOrUnknownRejectsUnexpectedProperty(string input)
    {
        SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(SettingsDocumentValidator.Validate(Document(input)));
        Assert.AreSame(SettingsReadFailureKind.UnexpectedProperty, rejected.Kind);
    }

    /// <summary>Proves bad types, unknown choices and casing preserve the whole rejected document.</summary>
    [TestMethod]
    [TestProperty("ThreatId", "ADV-012")]
    [TestCategory("Adversarial")]
    [DataRow("null")]
    [DataRow("[]")]
    [DataRow("{\"left\":null,\"right\":{\"key\":\"name\",\"direction\":\"ascending\"}}")]
    [DataRow("{\"left\":[],\"right\":{\"key\":\"name\",\"direction\":\"ascending\"}}")]
    [DataRow("{\"left\":{\"key\":4,\"direction\":\"ascending\"},\"right\":{\"key\":\"name\",\"direction\":\"ascending\"}}")]
    [DataRow("{\"left\":{\"key\":\"name\",\"direction\":null},\"right\":{\"key\":\"name\",\"direction\":\"ascending\"}}")]
    [DataRow("{\"left\":{\"key\":\"Name\",\"direction\":\"ascending\"},\"right\":{\"key\":\"name\",\"direction\":\"ascending\"}}")]
    [DataRow("{\"left\":{\"key\":\"random\",\"direction\":\"ascending\"},\"right\":{\"key\":\"name\",\"direction\":\"ascending\"}}")]
    [DataRow("{\"left\":{\"key\":\"name\",\"direction\":\"Descending\"},\"right\":{\"key\":\"name\",\"direction\":\"ascending\"}}")]
    [DataRow("{\"left\":{\"key\":\"name\",\"direction\":\"up\"},\"right\":{\"key\":\"name\",\"direction\":\"ascending\"}}")]
    [DataRow("{\"left\":{\"key\":\"name\",\"direction\":\"ascending\"},\"right\":{\"key\":\"size\",\"direction\":false}}")]
    public async Task ReadAsyncWhenSortChoiceIsInvalidRejectsWithoutRepairAsync(string input)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string document = Document(input);
        string path = root.WriteFile("settings.json", document);
        WindowsLocalSettingsStore store = Store(root);

        SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(await store.ReadAsync(CancellationToken.None));
        SettingsWriteRejected failedWrite = Assert.IsInstanceOfType<SettingsWriteRejected>(await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsReadFailureKind.Malformed, rejected.Kind);
        Assert.AreSame(SettingsWriteFailureKind.ExistingDocumentRejected, failedWrite.Failure);
        Assert.AreEqual(document, File.ReadAllText(path));
        Assert.IsFalse(File.Exists(root.Resolve("settings.json.tmp")));
    }

    /// <summary>Proves the new root member is mandatory and can occur exactly once.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    [DataRow(0)]
    [DataRow(1)]
    public void ValidateWhenSortRootIsMissingOrDuplicateRejectsWholeDocument(int variant)
    {
        string orders = "{\"left\":" + DefaultOrder + ",\"right\":" + DefaultOrder + "}";
        string valid = Document(orders);
        string input = variant == 0
            ? "{\"schemaVersion\":3,\"showHiddenItems\":true,\"colorScheme\":\"ubuntu\",\"bookmarkCategories\":[],\"bookmarks\":[]}"
            : valid[..^1] + ",\"paneSortOrders\":" + orders + "}";

        SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(SettingsDocumentValidator.Validate(input));

        Assert.AreSame(variant == 0 ? SettingsReadFailureKind.Incomplete : SettingsReadFailureKind.UnexpectedProperty, rejected.Kind);
    }

    private static string Document(string orders)
    {
        return "{\"schemaVersion\":3,\"showHiddenItems\":true,\"colorScheme\":\"ubuntu\",\"bookmarkCategories\":[],\"bookmarks\":[],\"paneSortOrders\":" + orders + "}";
    }

    private static BookmarkCatalog Catalog()
    {
        BookmarkCategoryName category = Assert.IsInstanceOfType<BookmarkCategoryNameAccepted>(BookmarkCategoryName.Parse("Work")).Name;
        BookmarkDisplayName name = Assert.IsInstanceOfType<BookmarkDisplayNameAccepted>(BookmarkDisplayName.Parse("Saved")).Name;
        BookmarkPath path = Assert.IsInstanceOfType<BookmarkPathAccepted>(BookmarkPath.Parse("C:\\saved")).Path;
        BookmarkEntry entry = BookmarkEntry.Create(name, path, category, BookmarkShortcutSlot.One);
        return Assert.IsInstanceOfType<BookmarkCatalogAccepted>(BookmarkCatalog.Create([category], [entry])).Catalog;
    }

    private static PaneSortOrder Order(int choice)
    {
        SortKey[] keys = [SortKey.Name, SortKey.Extension, SortKey.Size, SortKey.Modified];
        return PaneSortOrder.Create(keys[choice / 2], choice % 2 == 0 ? SortDirection.Ascending : SortDirection.Descending);
    }

    private static WindowsLocalSettingsStore Store(TestOwnedTemporaryRoot root)
    {
        WindowsLocalPath path = Assert.IsInstanceOfType<WindowsLocalPath>(Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(root.Resolve("settings.json"))).Path);
        return new WindowsLocalSettingsStore(path, new WindowsLocalIoExecutionBoundary());
    }
}
