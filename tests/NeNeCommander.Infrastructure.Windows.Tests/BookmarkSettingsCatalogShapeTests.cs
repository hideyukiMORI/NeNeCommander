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

/// <summary>
/// Proves the bookmark catalog inside a settings document is accepted only at its exact count
/// bounds and closed shape, and that every other catalog is rejected whole without repair.
/// </summary>
[TestClass]
public sealed class BookmarkSettingsCatalogShapeTests
{
    private const string DocumentName = "settings.json";

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

    private static WindowsLocalSettingsStore CreateStore(TestOwnedTemporaryRoot root)
    {
        return new WindowsLocalSettingsStore(
            ParseChild(root, DocumentName),
            new WindowsLocalIoExecutionBoundary(),
            new DeterministicIdentifierSource());
    }

    private static WindowsLocalPath ParseChild(TestOwnedTemporaryRoot root, string childName)
    {
        return Assert.IsInstanceOfType<WindowsLocalPath>(
            Assert.IsInstanceOfType<PathParseSuccess>(
                FileSystemPath.Parse(root.Resolve(childName))).Path);
    }
}
