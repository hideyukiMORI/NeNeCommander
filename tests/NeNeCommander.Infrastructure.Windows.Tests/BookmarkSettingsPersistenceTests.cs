using System.Collections.Generic;
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

/// <summary>Proves bookmark settings use the one strict, bounded settings document.</summary>
[TestClass]
public sealed class BookmarkSettingsPersistenceTests
{
    private const string DocumentName = "settings.json";
    private const string TemporarySuffix = ".00000000000000000000000000000001.tmp";
    /*lang=json,strict*/
    private const string VersionOne =
        "{\"schemaVersion\":1,\"showHiddenItems\":true,\"colorScheme\":\"ubuntu\"}";

    /// <summary>Proves a valid version-one document migrates only in memory to an empty catalog.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenVersionOneIsValidReturnsEmptyCatalogWithoutWritingAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string documentPath = root.WriteFile(DocumentName, VersionOne);
        WindowsLocalSettingsStore store = CreateStore(root);

        UserSettings settings = Assert.IsInstanceOfType<SettingsRead>(
            await store.ReadAsync(CancellationToken.None)).Settings;

        Assert.AreSame(ColorScheme.Ubuntu, settings.Display.ColorScheme);
        Assert.IsEmpty(settings.Bookmarks.Categories);
        Assert.IsEmpty(settings.Bookmarks.Bookmarks);
        Assert.AreEqual(VersionOne, File.ReadAllText(documentPath));
        Assert.IsFalse(File.Exists(documentPath + TemporarySuffix));
    }

    /// <summary>Proves the next write after a valid version-one read publishes complete version four.</summary>
    [TestMethod]
    public async Task WriteAsyncAfterVersionOneReadPublishesExplicitEmptyVersionFourCatalogAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.WriteFile(DocumentName, VersionOne);
        WindowsLocalSettingsStore store = CreateStore(root);
        UserSettings settings = Assert.IsInstanceOfType<SettingsRead>(
            await store.ReadAsync(CancellationToken.None)).Settings;

        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(
            await store.WriteAsync(settings, CancellationToken.None));

        Assert.AreEqual(
            "{\"schemaVersion\":4,\"showHiddenItems\":true,\"colorScheme\":\"ubuntu\"," +
            "\"bookmarkCategories\":[],\"bookmarks\":[],\"paneSortOrders\":{\"left\":{\"key\":\"name\",\"direction\":\"ascending\"},\"right\":{\"key\":\"name\",\"direction\":\"ascending\"}},\"showKeyHints\":true}",
            File.ReadAllText(root.Resolve(DocumentName)));
    }

    /// <summary>Proves valid multibyte paths and names round-trip with canonical category spelling.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenVersionTwoIsValidReturnsCanonicalReboundBookmarksAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string Document =
            "{\"schemaVersion\":2,\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"," +
            "\"bookmarkCategories\":[\"Work😀\"],\"bookmarks\":[" +
            "{\"name\":\"Local😀\",\"path\":\"C:\\\\資料\\\\😀\",\"category\":\"work😀\",\"shortcutSlot\":1}," +
            "{\"name\":\"Share\",\"path\":\"\\\\\\\\server\\\\share\\\\資料\",\"category\":null,\"shortcutSlot\":null}," +
            "{\"name\":\"Linux\",\"path\":\"\\\\\\\\wsl.localhost\\\\Ubuntu\\\\home\\\\😀\",\"category\":null,\"shortcutSlot\":9}]}";
        _ = root.WriteFile(DocumentName, Document);
        WindowsLocalSettingsStore store = CreateStore(root);

        UserSettings settings = Assert.IsInstanceOfType<SettingsRead>(
            await store.ReadAsync(CancellationToken.None)).Settings;

        Assert.HasCount(1, settings.Bookmarks.Categories);
        Assert.HasCount(3, settings.Bookmarks.Bookmarks);
        BookmarkCategoryName category = settings.Bookmarks.Categories[0];
        Assert.AreEqual("Work😀", category.Value);
        Assert.AreSame(category, settings.Bookmarks.Bookmarks[0].Category);
        Assert.AreEqual("C:\\資料\\😀", settings.Bookmarks.Bookmarks[0].Path.Value.CanonicalText);
        _ = Assert.IsInstanceOfType<WindowsUncPath>(settings.Bookmarks.Bookmarks[1].Path.Value);
        _ = Assert.IsInstanceOfType<WslPath>(settings.Bookmarks.Bookmarks[2].Path.Value);
        Assert.AreSame(BookmarkShortcutSlot.One, settings.Bookmarks.Bookmarks[0].ShortcutSlot);
        Assert.AreSame(BookmarkShortcutSlot.Nine, settings.Bookmarks.Bookmarks[2].ShortcutSlot);
    }

    /// <summary>Proves an oversized complete value is rejected before any filesystem mutation.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenVersionFourSerializationIsOversizedRejectsBeforeMutationAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        bool mutationBoundaryReached = false;
        const string NestedDocument = "absent\\settings.json";
        WindowsLocalSettingsStore store = CreateStoreAt(
            root,
            NestedDocument,
            new ScriptedSettingsWriteTestHook
            {
                OnBeforeDirectoryCreation = _ => mutationBoundaryReached = true,
                OnBeforeTemporaryCreation = _ => mutationBoundaryReached = true,
            });
        UserSettings settings = CreateLargeSettings(32750, 32422);
        Assert.AreEqual(SettingsDocumentValidator.MaximumDocumentLength + 1, SettingsDocumentSerializer.Serialize(settings).Length);

        SettingsWriteRejected rejected = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(settings, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.TooLarge, rejected.Failure);
        Assert.AreSame(SettingsDirectoryEffect.NotAttempted, rejected.DirectoryEffect);
        Assert.AreSame(SettingsWriteEffect.None, rejected.TemporaryEffect);
        Assert.IsFalse(mutationBoundaryReached);
        Assert.IsFalse(Directory.Exists(root.Resolve("absent")));
        Assert.IsFalse(File.Exists(root.Resolve(NestedDocument)));
        Assert.IsFalse(File.Exists(root.Resolve(NestedDocument + TemporarySuffix)));
    }

    /// <summary>Proves a complete schema-four document exactly at the byte boundary is accepted.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenVersionFourSerializationIsExactlyAtBoundarySucceedsAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalSettingsStore store = CreateStore(root);
        UserSettings settings = CreateLargeSettings(32750, 32421);

        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(
            await store.WriteAsync(settings, CancellationToken.None));

        Assert.AreEqual(
            SettingsDocumentValidator.MaximumDocumentLength,
            File.ReadAllBytes(root.Resolve(DocumentName)).Length);
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
    }

    /// <summary>Proves the serializer writes schema four in deterministic preserved catalog order.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenBookmarksAreValidInstallsDeterministicSchemaFourAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalSettingsStore store = CreateStore(root);
        BookmarkCategoryName work = ParseCategory("Work");
        BookmarkCategoryName personal = ParseCategory("Personal");
        BookmarkEntry repository = BookmarkEntry.Create(
            ParseName("Repository😀"),
            ParseBookmarkPath("C:\\work\\NeNeCommander"),
            work,
            BookmarkShortcutSlot.One);
        BookmarkEntry notes = BookmarkEntry.Create(
            ParseName("Notes"),
            ParseBookmarkPath("C:\\資料\\😀"),
            personal,
            null);
        UserSettings settings = CreateSettings([personal, work], [notes, repository]);

        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(
            await store.WriteAsync(settings, CancellationToken.None));

        Assert.AreEqual(
            "{\"schemaVersion\":4,\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"," +
            "\"bookmarkCategories\":[\"Personal\",\"Work\"],\"bookmarks\":[{" +
            "\"name\":\"Notes\",\"path\":\"C:\\\\\\u8CC7\\u6599\\\\\\uD83D\\uDE00\"," +
            "\"category\":\"Personal\",\"shortcutSlot\":null},{" +
            "\"name\":\"Repository\\uD83D\\uDE00\",\"path\":\"C:\\\\work\\\\NeNeCommander\"," +
            "\"category\":\"Work\",\"shortcutSlot\":1}],\"paneSortOrders\":{\"left\":{\"key\":\"name\",\"direction\":\"ascending\"},\"right\":{\"key\":\"name\",\"direction\":\"ascending\"}},\"showKeyHints\":true}",
            File.ReadAllText(root.Resolve(DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));

        UserSettings reloaded = Assert.IsInstanceOfType<SettingsRead>(
            await CreateStore(root).ReadAsync(CancellationToken.None)).Settings;
        Assert.AreEqual("Personal", reloaded.Bookmarks.Categories[0].Value);
        Assert.AreEqual("Work", reloaded.Bookmarks.Categories[1].Value);
        Assert.AreEqual("Notes", reloaded.Bookmarks.Bookmarks[0].Name.Value);
        Assert.AreEqual("C:\\資料\\😀", reloaded.Bookmarks.Bookmarks[0].Path.Value.CanonicalText);
        Assert.AreEqual("Repository😀", reloaded.Bookmarks.Bookmarks[1].Name.Value);
    }

    private static UserSettings CreateLargeSettings(int firstLength, int secondLength)
    {
        List<BookmarkEntry> bookmarks =
        [
            BookmarkEntry.Create(ParseName("A"), ParseBookmarkPath("C:\\" + new string('a', firstLength)), null, null),
            BookmarkEntry.Create(ParseName("B"), ParseBookmarkPath("C:\\" + new string('b', secondLength)), null, null),
        ];
        return CreateSettings([], bookmarks);
    }

    private static UserSettings CreateSettings(
        IReadOnlyList<BookmarkCategoryName> categories,
        IReadOnlyList<BookmarkEntry> bookmarks)
    {
        BookmarkCatalog catalog = Assert.IsInstanceOfType<BookmarkCatalogAccepted>(
            BookmarkCatalog.Create(categories, bookmarks)).Catalog;
        return UserSettings.Create(DisplayPreferences.Create(ColorScheme.NeNeDark, HiddenItemVisibility.Hidden, KeyHintVisibility.Shown), catalog, PaneSortPreferences.Default);
    }

    private static BookmarkCategoryName ParseCategory(string text)
    {
        return Assert.IsInstanceOfType<BookmarkCategoryNameAccepted>(
            BookmarkCategoryName.Parse(text)).Name;
    }

    private static BookmarkDisplayName ParseName(string text)
    {
        return Assert.IsInstanceOfType<BookmarkDisplayNameAccepted>(
            BookmarkDisplayName.Parse(text)).Name;
    }

    private static BookmarkPath ParseBookmarkPath(string text)
    {
        return Assert.IsInstanceOfType<BookmarkPathAccepted>(BookmarkPath.Parse(text)).Path;
    }

    private static WindowsLocalSettingsStore CreateStore(TestOwnedTemporaryRoot root)
    {
        return new WindowsLocalSettingsStore(
            ParseChild(root, DocumentName),
            new WindowsLocalIoExecutionBoundary(),
            new DeterministicIdentifierSource());
    }

    private static WindowsLocalSettingsStore CreateStoreAt(
        TestOwnedTemporaryRoot root,
        string childName,
        ISettingsWriteTestHook hook)
    {
        return new WindowsLocalSettingsStore(
            ParseChild(root, childName),
            new WindowsLocalIoExecutionBoundary(),
            new DeterministicIdentifierSource(),
            hook);
    }

    private static WindowsLocalPath ParseChild(TestOwnedTemporaryRoot root, string childName)
    {
        return Assert.IsInstanceOfType<WindowsLocalPath>(
            Assert.IsInstanceOfType<PathParseSuccess>(
                FileSystemPath.Parse(root.Resolve(childName))).Path);
    }
}
