using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.Settings;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>
/// Proves a bookmark settings document whose encoding, preamble, whitespace, or root schema version
/// is not exact is rejected with its own reason and is never repaired on disk.
/// </summary>
[TestClass]
public sealed class BookmarkSettingsDocumentRejectionTests
{
    private const string DocumentName = "settings.json";

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
            "{\"schemaVersion\":4,\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"," +
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

    private static string VersionTwoWithBookmark(string properties)
    {
        return "{\"schemaVersion\":2,\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"," +
            "\"bookmarkCategories\":[],\"bookmarks\":[{" + properties + "}]}";
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
