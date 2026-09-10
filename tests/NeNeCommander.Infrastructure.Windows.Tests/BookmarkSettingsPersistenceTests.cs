using System.Collections.Generic;
using System.IO;
using System.Text;
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

        Assert.AreSame(ColorScheme.Ubuntu, settings.ColorScheme);
        Assert.IsEmpty(settings.Bookmarks.Categories);
        Assert.IsEmpty(settings.Bookmarks.Bookmarks);
        Assert.AreEqual(VersionOne, File.ReadAllText(documentPath));
        Assert.IsFalse(File.Exists(documentPath + ".tmp"));
    }

    /// <summary>Proves the next write after a valid version-one read publishes complete version two.</summary>
    [TestMethod]
    public async Task WriteAsyncAfterVersionOneReadPublishesExplicitEmptyVersionTwoCatalogAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.WriteFile(DocumentName, VersionOne);
        WindowsLocalSettingsStore store = CreateStore(root);
        UserSettings settings = Assert.IsInstanceOfType<SettingsRead>(
            await store.ReadAsync(CancellationToken.None)).Settings;

        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(
            await store.WriteAsync(settings, CancellationToken.None));

        Assert.AreEqual(
            "{\"schemaVersion\":2,\"showHiddenItems\":true,\"colorScheme\":\"ubuntu\"," +
            "\"bookmarkCategories\":[],\"bookmarks\":[]}",
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

    /// <summary>Proves malformed UTF-8 bytes never become replacement characters in settings.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenVersionTwoContainsInvalidUtf8RejectsWithoutRepairAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        byte[] prefix = Encoding.UTF8.GetBytes(
            "{\"schemaVersion\":2,\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"," +
            "\"bookmarkCategories\":[],\"bookmarks\":[{\"name\":\"");
        byte[] suffix = Encoding.UTF8.GetBytes(
            "\",\"path\":\"C:\\\\\",\"category\":null,\"shortcutSlot\":null}]}");
        byte[] document = new byte[prefix.Length + 1 + suffix.Length];
        prefix.CopyTo(document, 0);
        document[prefix.Length] = 0xff;
        suffix.CopyTo(document, prefix.Length + 1);
        string documentPath = root.Resolve(DocumentName);
        File.WriteAllBytes(documentPath, document);
        WindowsLocalSettingsStore store = CreateStore(root);

        SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(
            await store.ReadAsync(CancellationToken.None));

        Assert.AreSame(SettingsReadFailureKind.Malformed, rejected.Kind);
        CollectionAssert.AreEqual(document, File.ReadAllBytes(documentPath));
    }

    /// <summary>Proves an escaped unpaired surrogate is a malformed document rather than changed text.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenVersionTwoContainsEscapedUnpairedSurrogateRejectsWithoutRepairAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string Document =
            "{\"schemaVersion\":2,\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"," +
            "\"bookmarkCategories\":[],\"bookmarks\":[{" +
            "\"name\":\"\\uD800\",\"path\":\"C:\\\\\",\"category\":null,\"shortcutSlot\":null}]}";
        string documentPath = root.WriteFile(DocumentName, Document);
        WindowsLocalSettingsStore store = CreateStore(root);

        SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(
            await store.ReadAsync(CancellationToken.None));

        Assert.AreSame(SettingsReadFailureKind.Malformed, rejected.Kind);
        Assert.AreEqual(Document, File.ReadAllText(documentPath));
    }

    /// <summary>Proves an unpaired UTF-16 surrogate is rejected before string input is encoded.</summary>
    [TestMethod]
    public void ValidateWhenVersionTwoContainsUnpairedUtf16SurrogateRejectsMalformed()
    {
        string document = VersionTwoWithBookmark(
            "\"name\":\"" + '\ud800' +
            "\",\"path\":\"C:\\\\\",\"category\":null,\"shortcutSlot\":null");

        SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(
            SettingsDocumentValidator.Validate(document));

        Assert.AreSame(SettingsReadFailureKind.Malformed, rejected.Kind);
    }

    /// <summary>Proves a missing root schema version is classified as incomplete.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenSchemaVersionIsMissingRejectsIncompleteWithoutRepairAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string Document =
            "{\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"," +
            "\"bookmarkCategories\":[],\"bookmarks\":[]}";
        string documentPath = root.WriteFile(DocumentName, Document);

        SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(
            await CreateStore(root).ReadAsync(CancellationToken.None));

        Assert.AreSame(SettingsReadFailureKind.Incomplete, rejected.Kind);
        Assert.AreEqual(Document, File.ReadAllText(documentPath));
    }

    /// <summary>Proves conflicting root versions are rejected before an incomplete schema is considered.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenSchemaVersionsConflictRejectsUnexpectedPropertyWithoutRepairAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string Document =
            "{\"schemaVersion\":1,\"schemaVersion\":2,\"showHiddenItems\":false}";
        string documentPath = root.WriteFile(DocumentName, Document);

        SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(
            await CreateStore(root).ReadAsync(CancellationToken.None));

        Assert.AreSame(SettingsReadFailureKind.UnexpectedProperty, rejected.Kind);
        Assert.AreEqual(Document, File.ReadAllText(documentPath));
    }

    /// <summary>Proves a nonnumeric root version is malformed rather than an unknown numeric version.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenSchemaVersionHasWrongKindRejectsMalformedWithoutRepairAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string Document =
            "{\"schemaVersion\":\"2\",\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"," +
            "\"bookmarkCategories\":[],\"bookmarks\":[]}";
        string documentPath = root.WriteFile(DocumentName, Document);

        SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(
            await CreateStore(root).ReadAsync(CancellationToken.None));

        Assert.AreSame(SettingsReadFailureKind.Malformed, rejected.Kind);
        Assert.AreEqual(Document, File.ReadAllText(documentPath));
    }

    /// <summary>Proves an unsupported numeric root version remains an unknown version.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenSchemaVersionIsUnsupportedRejectsUnknownVersionWithoutRepairAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string Document =
            "{\"schemaVersion\":3,\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"," +
            "\"bookmarkCategories\":[],\"bookmarks\":[]}";
        string documentPath = root.WriteFile(DocumentName, Document);

        SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(
            await CreateStore(root).ReadAsync(CancellationToken.None));

        Assert.AreSame(SettingsReadFailureKind.UnknownVersion, rejected.Kind);
        Assert.AreEqual(Document, File.ReadAllText(documentPath));
    }

    /// <summary>Proves an unknown root property is reported even when another property is missing.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenVersionTwoHasUnknownAndMissingRootPropertiesRejectsUnexpectedPropertyAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string Document =
            "{\"schemaVersion\":2,\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"," +
            "\"bookmarkCategories\":[],\"unknown\":0}";
        string documentPath = root.WriteFile(DocumentName, Document);

        SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(
            await CreateStore(root).ReadAsync(CancellationToken.None));

        Assert.AreSame(SettingsReadFailureKind.UnexpectedProperty, rejected.Kind);
        Assert.AreEqual(Document, File.ReadAllText(documentPath));
    }

    /// <summary>Proves a document containing only the exact UTF-8 preamble is empty.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenDocumentIsOnlyUtf8PreambleRejectsEmptyWithoutRepairAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        byte[] document = [0xef, 0xbb, 0xbf];
        string documentPath = root.Resolve(DocumentName);
        File.WriteAllBytes(documentPath, document);

        SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(
            await CreateStore(root).ReadAsync(CancellationToken.None));

        Assert.AreSame(SettingsReadFailureKind.Empty, rejected.Kind);
        CollectionAssert.AreEqual(document, File.ReadAllBytes(documentPath));
    }

    /// <summary>Proves a truncated UTF-8 preamble is malformed without an out-of-range read.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenDocumentIsTruncatedUtf8PreambleRejectsMalformedWithoutRepairAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        byte[] document = [0xef, 0xbb];
        string documentPath = root.Resolve(DocumentName);
        File.WriteAllBytes(documentPath, document);

        SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(
            await CreateStore(root).ReadAsync(CancellationToken.None));

        Assert.AreSame(SettingsReadFailureKind.Malformed, rejected.Kind);
        CollectionAssert.AreEqual(document, File.ReadAllBytes(documentPath));
    }

    /// <summary>Proves all four JSON whitespace byte classes form an empty document.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenDocumentContainsEveryJsonWhitespaceByteRejectsEmptyWithoutRepairAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        byte[] document = [0x20, 0x09, 0x0a, 0x0d];
        string documentPath = root.Resolve(DocumentName);
        File.WriteAllBytes(documentPath, document);

        SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(
            await CreateStore(root).ReadAsync(CancellationToken.None));

        Assert.AreSame(SettingsReadFailureKind.Empty, rejected.Kind);
        CollectionAssert.AreEqual(document, File.ReadAllBytes(documentPath));
    }

    /// <summary>Proves the exact maximum number of categories remains readable.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenCategoryCountIsExactlyMaximumAcceptsCompleteCatalogAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string document = VersionTwo(CreateCategoryArray(BookmarkCatalog.MaximumCategoryCount), "[]");
        _ = root.WriteFile(DocumentName, document);

        UserSettings settings = Assert.IsInstanceOfType<SettingsRead>(
            await CreateStore(root).ReadAsync(CancellationToken.None)).Settings;

        Assert.HasCount(BookmarkCatalog.MaximumCategoryCount, settings.Bookmarks.Categories);
        Assert.IsEmpty(settings.Bookmarks.Bookmarks);
    }

    /// <summary>Proves one category beyond the maximum rejects the complete catalog.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenCategoryCountExceedsMaximumRejectsInvalidBookmarksAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string document = VersionTwo(CreateCategoryArray(BookmarkCatalog.MaximumCategoryCount + 1), "[]");
        string documentPath = root.WriteFile(DocumentName, document);

        SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(
            await CreateStore(root).ReadAsync(CancellationToken.None));

        Assert.AreSame(SettingsReadFailureKind.InvalidBookmarks, rejected.Kind);
        Assert.AreEqual(document, File.ReadAllText(documentPath));
    }

    /// <summary>Proves the exact maximum number of bookmarks remains readable.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenBookmarkCountIsExactlyMaximumAcceptsCompleteCatalogAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string document = VersionTwo("[]", CreateBookmarkArray(BookmarkCatalog.MaximumBookmarkCount));
        _ = root.WriteFile(DocumentName, document);

        UserSettings settings = Assert.IsInstanceOfType<SettingsRead>(
            await CreateStore(root).ReadAsync(CancellationToken.None)).Settings;

        Assert.IsEmpty(settings.Bookmarks.Categories);
        Assert.HasCount(BookmarkCatalog.MaximumBookmarkCount, settings.Bookmarks.Bookmarks);
    }

    /// <summary>Proves one bookmark beyond the maximum rejects the complete catalog.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenBookmarkCountExceedsMaximumRejectsInvalidBookmarksAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string document = VersionTwo("[]", CreateBookmarkArray(BookmarkCatalog.MaximumBookmarkCount + 1));
        string documentPath = root.WriteFile(DocumentName, document);

        SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(
            await CreateStore(root).ReadAsync(CancellationToken.None));

        Assert.AreSame(SettingsReadFailureKind.InvalidBookmarks, rejected.Kind);
        Assert.AreEqual(document, File.ReadAllText(documentPath));
    }

    /// <summary>Proves a null category array element is malformed and never repaired.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenCategoryElementIsNullRejectsMalformedWithoutRepairAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string document = VersionTwo("[null]", "[]");
        string documentPath = root.WriteFile(DocumentName, document);

        SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(
            await CreateStore(root).ReadAsync(CancellationToken.None));

        Assert.AreSame(SettingsReadFailureKind.Malformed, rejected.Kind);
        Assert.AreEqual(document, File.ReadAllText(documentPath));
    }

    /// <summary>Proves a null bookmark array element is malformed and never repaired.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenBookmarkElementIsNullRejectsMalformedWithoutRepairAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string document = VersionTwo("[]", "[null]");
        string documentPath = root.WriteFile(DocumentName, document);

        SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(
            await CreateStore(root).ReadAsync(CancellationToken.None));

        Assert.AreSame(SettingsReadFailureKind.Malformed, rejected.Kind);
        Assert.AreEqual(document, File.ReadAllText(documentPath));
    }

    /// <summary>Proves every version-two bookmark object has one exact closed property set.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenVersionTwoBookmarkShapeIsNotExactRejectsEachDocumentAsync()
    {
        (string Document, SettingsReadFailureKind Failure)[] cases =
        [
            (VersionTwoWithBookmark(
                "\"name\":\"A\",\"name\":\"B\",\"path\":\"C:\\\\\",\"category\":null,\"shortcutSlot\":null"),
                SettingsReadFailureKind.UnexpectedProperty),
            (VersionTwoWithBookmark("\"name\":\"A\",\"path\":\"C:\\\\\",\"category\":null"),
                SettingsReadFailureKind.Incomplete),
            (VersionTwoWithBookmark("\"name\":\"A\",\"path\":\"C:\\\\\",\"shortcutSlot\":null"),
                SettingsReadFailureKind.Incomplete),
            (VersionTwoWithBookmark(
                "\"name\":\"A\",\"path\":\"C:\\\\\",\"category\":null,\"shortcutSlot\":null,\"extra\":0"),
                SettingsReadFailureKind.UnexpectedProperty),
        ];

        foreach ((string document, SettingsReadFailureKind failure) in cases)
        {
            using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
            _ = root.WriteFile(DocumentName, document);
            WindowsLocalSettingsStore store = CreateStore(root);

            SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(
                await store.ReadAsync(CancellationToken.None));

            Assert.AreSame(failure, rejected.Kind);
        }
    }

    /// <summary>Proves each bookmark field rejects its own wrong JSON kind without partial state.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenVersionTwoBookmarkFieldHasWrongKindRejectsEachDocumentAsync()
    {
        string[] documents =
        [
            VersionTwoWithBookmark(
                "\"name\":0,\"path\":\"C:\\\\\",\"category\":null,\"shortcutSlot\":null"),
            VersionTwoWithBookmark(
                "\"name\":\"A\",\"path\":false,\"category\":null,\"shortcutSlot\":null"),
            VersionTwoWithBookmark(
                "\"name\":\"A\",\"path\":\"C:\\\\\",\"category\":[],\"shortcutSlot\":null"),
            VersionTwoWithBookmark(
                "\"name\":\"A\",\"path\":\"C:\\\\\",\"category\":null,\"shortcutSlot\":\"1\""),
        ];

        foreach (string document in documents)
        {
            using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
            _ = root.WriteFile(DocumentName, document);
            WindowsLocalSettingsStore store = CreateStore(root);

            SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(
                await store.ReadAsync(CancellationToken.None));

            Assert.AreSame(SettingsReadFailureKind.Malformed, rejected.Kind);
            Assert.AreEqual(document, File.ReadAllText(root.Resolve(DocumentName)));
        }
    }

    /// <summary>Proves invalid paths and inconsistent catalog keys never become partial settings.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenVersionTwoCatalogIsInvalidRejectsEveryCompleteDocumentAsync()
    {
        string[] documents =
        [
            VersionTwo("[\"Work\",\"work\"]", "[]"),
            VersionTwo("[]", "[" + Bookmark("A", "relative", null, null) + "]"),
            VersionTwo("[\"Work\"]", "[" + Bookmark("A", "C:\\\\", "Missing", null) + "]"),
            VersionTwo(
                "[]",
                "[" + Bookmark("A", "C:\\\\one", null, null) + "," +
                Bookmark("a", "C:\\\\two", null, null) + "]"),
            VersionTwo(
                "[]",
                "[" + Bookmark("A", "C:\\\\one", null, 1) + "," +
                Bookmark("B", "C:\\\\two", null, 1) + "]"),
            VersionTwo("[]", "[" + Bookmark("A", "C:\\\\", null, 0) + "]"),
        ];

        foreach (string document in documents)
        {
            using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
            string documentPath = root.WriteFile(DocumentName, document);
            WindowsLocalSettingsStore store = CreateStore(root);

            SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(
                await store.ReadAsync(CancellationToken.None));

            Assert.AreSame(SettingsReadFailureKind.InvalidBookmarks, rejected.Kind);
            Assert.AreEqual(document, File.ReadAllText(documentPath));
        }
    }

    /// <summary>Proves schema two accepts only its exact five-property root contract.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenVersionTwoRootShapeIsNotExactRejectsEachDocumentAsync()
    {
        (string Document, SettingsReadFailureKind Failure)[] cases =
        [
            ("{\"schemaVersion\":2,\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"," +
                "\"bookmarkCategories\":[],\"bookmarks\":[],\"extra\":0}",
                SettingsReadFailureKind.UnexpectedProperty),
            ("{\"schemaVersion\":2,\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"," +
                "\"bookmarkCategories\":[]}", SettingsReadFailureKind.Incomplete),
            ("{\"schemaVersion\":2,\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"," +
                "\"bookmarkCategories\":[],\"bookmarks\":[],\"bookmarks\":[]}",
                SettingsReadFailureKind.UnexpectedProperty),
        ];

        foreach ((string document, SettingsReadFailureKind failure) in cases)
        {
            using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
            _ = root.WriteFile(DocumentName, document);
            WindowsLocalSettingsStore store = CreateStore(root);

            SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(
                await store.ReadAsync(CancellationToken.None));

            Assert.AreSame(failure, rejected.Kind);
        }
    }

    /// <summary>Proves both version-two catalog roots must be arrays.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenVersionTwoCatalogRootHasWrongKindRejectsEachDocumentAsync()
    {
        string[] documents =
        [
            VersionTwo("{}", "[]"),
            VersionTwo("[]", "{}"),
        ];

        foreach (string document in documents)
        {
            using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
            _ = root.WriteFile(DocumentName, document);
            WindowsLocalSettingsStore store = CreateStore(root);

            SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(
                await store.ReadAsync(CancellationToken.None));

            Assert.AreSame(SettingsReadFailureKind.Malformed, rejected.Kind);
            Assert.AreEqual(document, File.ReadAllText(root.Resolve(DocumentName)));
        }
    }

    /// <summary>Proves an oversized complete value is rejected before any filesystem mutation.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenVersionTwoSerializationIsOversizedRejectsBeforeMutationAsync()
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
        UserSettings settings = CreateLargeSettings(32750, 32554);

        SettingsWriteRejected rejected = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(settings, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.TooLarge, rejected.Failure);
        Assert.AreSame(SettingsDirectoryEffect.NotAttempted, rejected.DirectoryEffect);
        Assert.AreSame(SettingsWriteEffect.None, rejected.TemporaryEffect);
        Assert.IsFalse(mutationBoundaryReached);
        Assert.IsFalse(Directory.Exists(root.Resolve("absent")));
        Assert.IsFalse(File.Exists(root.Resolve(NestedDocument)));
        Assert.IsFalse(File.Exists(root.Resolve(NestedDocument + ".tmp")));
    }

    /// <summary>Proves a complete schema-two document exactly at the byte boundary is accepted.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenVersionTwoSerializationIsExactlyAtBoundarySucceedsAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalSettingsStore store = CreateStore(root);
        UserSettings settings = CreateLargeSettings(32750, 32553);

        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(
            await store.WriteAsync(settings, CancellationToken.None));

        Assert.AreEqual(
            SettingsDocumentValidator.MaximumDocumentLength,
            File.ReadAllBytes(root.Resolve(DocumentName)).Length);
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + ".tmp")));
    }

    /// <summary>Proves the serializer writes schema two in deterministic preserved catalog order.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenBookmarksAreValidInstallsDeterministicSchemaTwoAsync()
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
            "{\"schemaVersion\":2,\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"," +
            "\"bookmarkCategories\":[\"Personal\",\"Work\"],\"bookmarks\":[{" +
            "\"name\":\"Notes\",\"path\":\"C:\\\\\\u8CC7\\u6599\\\\\\uD83D\\uDE00\"," +
            "\"category\":\"Personal\",\"shortcutSlot\":null},{" +
            "\"name\":\"Repository\\uD83D\\uDE00\",\"path\":\"C:\\\\work\\\\NeNeCommander\"," +
            "\"category\":\"Work\",\"shortcutSlot\":1}]}",
            File.ReadAllText(root.Resolve(DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + ".tmp")));

        UserSettings reloaded = Assert.IsInstanceOfType<SettingsRead>(
            await CreateStore(root).ReadAsync(CancellationToken.None)).Settings;
        Assert.AreEqual("Personal", reloaded.Bookmarks.Categories[0].Value);
        Assert.AreEqual("Work", reloaded.Bookmarks.Categories[1].Value);
        Assert.AreEqual("Notes", reloaded.Bookmarks.Bookmarks[0].Name.Value);
        Assert.AreEqual("C:\\資料\\😀", reloaded.Bookmarks.Bookmarks[0].Path.Value.CanonicalText);
        Assert.AreEqual("Repository😀", reloaded.Bookmarks.Bookmarks[1].Name.Value);
    }

    private static string VersionTwoWithBookmark(string properties)
    {
        return "{\"schemaVersion\":2,\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"," +
            "\"bookmarkCategories\":[],\"bookmarks\":[{" + properties + "}]}";
    }

    private static string VersionTwo(string categories, string bookmarks)
    {
        return "{\"schemaVersion\":2,\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"," +
            "\"bookmarkCategories\":" + categories + ",\"bookmarks\":" + bookmarks + "}";
    }

    private static string Bookmark(string name, string path, string? category, int? slot)
    {
        string categoryValue = category is null ? "null" : "\"" + category + "\"";
        string slotValue = slot is null
            ? "null"
            : slot.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return "{\"name\":\"" + name + "\",\"path\":\"" + path + "\",\"category\":" +
            categoryValue + ",\"shortcutSlot\":" + slotValue + "}";
    }

    private static string CreateCategoryArray(int count)
    {
        List<string> categories = new(count);
        for (int index = 0; index < count; index++)
        {
            categories.Add("\"Category" + index.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + "\"");
        }
        return "[" + string.Join(',', categories) + "]";
    }

    private static string CreateBookmarkArray(int count)
    {
        List<string> bookmarks = new(count);
        for (int index = 0; index < count; index++)
        {
            bookmarks.Add(Bookmark(
                "Bookmark" + index.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "C:\\\\shared",
                null,
                null));
        }
        return "[" + string.Join(',', bookmarks) + "]";
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
        return UserSettings.Create(ColorScheme.NeNeDark, HiddenItemVisibility.Hidden, catalog);
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
            new WindowsLocalIoExecutionBoundary());
    }

    private static WindowsLocalSettingsStore CreateStoreAt(
        TestOwnedTemporaryRoot root,
        string childName,
        ISettingsWriteTestHook hook)
    {
        return new WindowsLocalSettingsStore(
            ParseChild(root, childName),
            new WindowsLocalIoExecutionBoundary(),
            hook);
    }

    private static WindowsLocalPath ParseChild(TestOwnedTemporaryRoot root, string childName)
    {
        return Assert.IsInstanceOfType<WindowsLocalPath>(
            Assert.IsInstanceOfType<PathParseSuccess>(
                FileSystemPath.Parse(root.Resolve(childName))).Path);
    }
}
