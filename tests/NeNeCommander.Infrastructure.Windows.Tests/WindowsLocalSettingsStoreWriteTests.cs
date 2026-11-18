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

/// <summary>Proves the Windows settings store installs complete writes, orders its own writes, and creates a missing parent once.</summary>
[TestClass]
public sealed class WindowsLocalSettingsStoreWriteTests
{
    private const string DocumentName = "settings.json";
    private const string TemporarySuffix = ".00000000000000000000000000000001.tmp";
    private const string SecondTemporarySuffix = ".00000000000000000000000000000002.tmp";
    private const string ThirdTemporarySuffix = ".00000000000000000000000000000003.tmp";

    /// <summary>Proves an absent document is installed with the exact schema and no temporary residue.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenDocumentIsAbsentInstallsExactCompleteDocumentAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalSettingsStore store = CreateStore(root);
        UserSettings settings = UserSettings.Create(ColorScheme.Dracula, HiddenItemVisibility.Shown, BookmarkCatalog.Empty, PaneSortPreferences.Default);

        SettingsWriteOutcome outcome = await store.WriteAsync(settings, CancellationToken.None);

        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(outcome);
        byte[] bytes = File.ReadAllBytes(root.Resolve(DocumentName));
        CollectionAssert.AreEqual(
            Encoding.UTF8.GetBytes(
                "{\"schemaVersion\":3,\"showHiddenItems\":true,\"colorScheme\":\"dracula\"," +
                "\"bookmarkCategories\":[],\"bookmarks\":[],\"paneSortOrders\":{\"left\":{\"key\":\"name\",\"direction\":\"ascending\"},\"right\":{\"key\":\"name\",\"direction\":\"ascending\"}}}"),
            bytes);
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
    }

    /// <summary>Proves a valid existing document is atomically replaced by the complete new value.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenDocumentIsValidReplacesItAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.WriteFile(
            DocumentName,
            "{\"schemaVersion\":1,\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"}");
        WindowsLocalSettingsStore store = CreateStore(root);

        SettingsWriteOutcome outcome = await store.WriteAsync(
            UserSettings.Create(ColorScheme.SolarizedLight, HiddenItemVisibility.Hidden, BookmarkCatalog.Empty, PaneSortPreferences.Default),
            CancellationToken.None);

        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(outcome);
        Assert.AreEqual(
            "{\"schemaVersion\":3,\"showHiddenItems\":false,\"colorScheme\":\"solarized-light\"," +
            "\"bookmarkCategories\":[],\"bookmarks\":[],\"paneSortOrders\":{\"left\":{\"key\":\"name\",\"direction\":\"ascending\"},\"right\":{\"key\":\"name\",\"direction\":\"ascending\"}}}",
            File.ReadAllText(root.Resolve(DocumentName)));
    }

    /// <summary>Proves a direct write cannot replace an oversized existing document.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenExistingDocumentExceedsBoundaryRejectsWithoutMutationAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string oversized = new('x', SettingsDocumentValidator.MaximumDocumentLength + 1);
        string documentPath = root.WriteFile(DocumentName, oversized);
        WindowsLocalSettingsStore store = CreateStore(root);

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.ExistingDocumentRejected, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.None, outcome.TemporaryEffect);
        Assert.AreEqual(oversized, File.ReadAllText(documentPath));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
    }

    /// <summary>Proves a valid document at the exact size boundary remains replaceable.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenExistingDocumentIsExactlyAtBoundaryReplacesItAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string Document =
            "{\"schemaVersion\":1,\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"}";
        string bounded = Document + new string(
            ' ',
            SettingsDocumentValidator.MaximumDocumentLength - Document.Length);
        _ = root.WriteFile(DocumentName, bounded);
        WindowsLocalSettingsStore store = CreateStore(root);

        SettingsWriteOutcome outcome = await store.WriteAsync(
            UserSettings.Create(ColorScheme.Ubuntu, HiddenItemVisibility.Shown, BookmarkCatalog.Empty, PaneSortPreferences.Default),
            CancellationToken.None);

        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(outcome);
        Assert.AreEqual(
            "{\"schemaVersion\":3,\"showHiddenItems\":true,\"colorScheme\":\"ubuntu\"," +
            "\"bookmarkCategories\":[],\"bookmarks\":[],\"paneSortOrders\":{\"left\":{\"key\":\"name\",\"direction\":\"ascending\"},\"right\":{\"key\":\"name\",\"direction\":\"ascending\"}}}",
            File.ReadAllText(root.Resolve(DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
    }

    /// <summary>Proves a direct write never repairs malformed settings.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenExistingDocumentIsMalformedRejectsWithoutMutationAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string Malformed = "{\"schemaVersion\":1";
        string documentPath = root.WriteFile(DocumentName, Malformed);
        WindowsLocalSettingsStore store = CreateStore(root);

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.ExistingDocumentRejected, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.None, outcome.TemporaryEffect);
        Assert.AreEqual(Malformed, File.ReadAllText(documentPath));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
    }

    /// <summary>Proves a non-file destination is rejected before temporary-file creation.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenDocumentPathIsDirectoryRejectsWithoutMutationAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory(DocumentName);
        WindowsLocalSettingsStore store = CreateStore(root);

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.ExistingDocumentRejected, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.None, outcome.TemporaryEffect);
        Assert.IsTrue(Directory.Exists(root.Resolve(DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
    }

    /// <summary>Proves a locked destination becomes a closed I/O rejection without mutation.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenExistingDocumentIsLockedRejectsWithoutMutationAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string Original =
            "{\"schemaVersion\":1,\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"}";
        string documentPath = root.WriteFile(DocumentName, Original);
        WindowsLocalSettingsStore store = CreateStore(root);
        using FileStream exclusive = new(documentPath, FileMode.Open, FileAccess.Read, FileShare.None);

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.IoFailure, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.None, outcome.TemporaryEffect);
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
    }

    /// <summary>Proves one store advances its baseline after publish so ordered choices can continue.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenTwoValuesAreOrderedPublishesBothWithoutSelfConflictAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        List<string> candidates = [];
        WindowsLocalSettingsStore store = CreateStore(
            root,
            new ScriptedSettingsWriteTestHook
            {
                OnBeforeTemporaryCreation = candidates.Add,
            });
        _ = Assert.IsInstanceOfType<SettingsAbsent>(await store.ReadAsync(CancellationToken.None));

        SettingsWriteOutcome first = await store.WriteAsync(
            UserSettings.Create(ColorScheme.Ubuntu, HiddenItemVisibility.Hidden, BookmarkCatalog.Empty, PaneSortPreferences.Default),
            CancellationToken.None);
        SettingsWriteOutcome second = await store.WriteAsync(
            UserSettings.Create(ColorScheme.Dracula, HiddenItemVisibility.Shown, BookmarkCatalog.Empty, PaneSortPreferences.Default),
            CancellationToken.None);
        SettingsWriteOutcome third = await store.WriteAsync(
            UserSettings.Create(ColorScheme.Monokai, HiddenItemVisibility.Hidden, BookmarkCatalog.Empty, PaneSortPreferences.Default),
            CancellationToken.None);

        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(first);
        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(second);
        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(third);
        string[] expectedCandidates =
        [
            root.Resolve(DocumentName + TemporarySuffix),
            root.Resolve(DocumentName + SecondTemporarySuffix),
            root.Resolve(DocumentName + ThirdTemporarySuffix),
        ];
        CollectionAssert.AreEqual(expectedCandidates, candidates);
        Assert.AreEqual(
            "{\"schemaVersion\":3,\"showHiddenItems\":false,\"colorScheme\":\"monokai\"," +
            "\"bookmarkCategories\":[],\"bookmarks\":[],\"paneSortOrders\":{\"left\":{\"key\":\"name\",\"direction\":\"ascending\"},\"right\":{\"key\":\"name\",\"direction\":\"ascending\"}}}",
            File.ReadAllText(root.Resolve(DocumentName)));
    }

    /// <summary>Proves directory creation advances the location baseline for the next owned write.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenMissingParentIsCreatedAllowsTheNextOwnedWriteAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string NestedDocument = "created-parent\\settings.json";
        WindowsLocalSettingsStore store = CreateStoreAt(root, NestedDocument);

        SettingsWriteOutcome first = await store.WriteAsync(
            UserSettings.Create(ColorScheme.Ubuntu, HiddenItemVisibility.Hidden, BookmarkCatalog.Empty, PaneSortPreferences.Default),
            CancellationToken.None);
        SettingsWriteOutcome second = await store.WriteAsync(
            UserSettings.Create(ColorScheme.Dracula, HiddenItemVisibility.Shown, BookmarkCatalog.Empty, PaneSortPreferences.Default),
            CancellationToken.None);

        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(first);
        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(second);
        Assert.AreEqual(
            "{\"schemaVersion\":3,\"showHiddenItems\":true,\"colorScheme\":\"dracula\"," +
            "\"bookmarkCategories\":[],\"bookmarks\":[],\"paneSortOrders\":{\"left\":{\"key\":\"name\",\"direction\":\"ascending\"},\"right\":{\"key\":\"name\",\"direction\":\"ascending\"}}}",
            File.ReadAllText(root.Resolve(NestedDocument)));
        Assert.IsFalse(File.Exists(root.Resolve(NestedDocument + TemporarySuffix)));
    }

    /// <summary>Proves a verified created parent remains the baseline after a later write failure.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenTemporaryStepFailsAfterParentCreationAllowsRetryAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string NestedDocument = "retry-parent\\settings.json";
        int attempts = 0;
        WindowsLocalSettingsStore store = CreateStoreAt(
            root,
            NestedDocument,
            new ScriptedSettingsWriteTestHook
            {
                OnTemporaryFlushed = _ =>
                {
                    attempts++;
                    if (attempts == 1)
                    {
                        throw new IOException("Injected failure after directory verification.");
                    }
                },
            });

        SettingsWriteRejected first = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));
        SettingsWriteOutcome second = await store.WriteAsync(
            UserSettings.Create(ColorScheme.Dracula, HiddenItemVisibility.Shown, BookmarkCatalog.Empty, PaneSortPreferences.Default),
            CancellationToken.None);

        Assert.AreSame(SettingsWriteFailureKind.IoFailure, first.Failure);
        Assert.AreSame(SettingsDirectoryEffect.CreationObserved, first.DirectoryEffect);
        Assert.AreSame(SettingsWriteEffect.None, first.TemporaryEffect);
        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(second);
        Assert.AreEqual(2, attempts);
        Assert.Contains("\"colorScheme\":\"dracula\"", File.ReadAllText(root.Resolve(NestedDocument)));
        Assert.IsFalse(File.Exists(root.Resolve(NestedDocument + TemporarySuffix)));
    }

    /// <summary>Proves an exception before created-parent verification reports uncertainty.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenDirectoryObservationFailsReportsUnconfirmedCreationAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string NestedDocument = "unconfirmed-parent\\settings.json";
        WindowsLocalSettingsStore store = CreateStoreAt(
            root,
            NestedDocument,
            new ScriptedSettingsWriteTestHook
            {
                OnDirectoryCreated = _ => throw new IOException("Injected directory observation failure."),
            });

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.IoFailure, outcome.Failure);
        Assert.AreSame(SettingsDirectoryEffect.CreationUnconfirmed, outcome.DirectoryEffect);
        Assert.AreSame(SettingsWriteEffect.None, outcome.TemporaryEffect);
        Assert.IsTrue(Directory.Exists(root.Resolve("unconfirmed-parent")));
        Assert.IsFalse(File.Exists(root.Resolve(NestedDocument)));
        Assert.IsFalse(File.Exists(root.Resolve(NestedDocument + TemporarySuffix)));
    }

    private static WindowsLocalSettingsStore CreateStore(TestOwnedTemporaryRoot root)
    {
        return new WindowsLocalSettingsStore(
            ParseChild(root, DocumentName),
            new WindowsLocalIoExecutionBoundary(),
            new DeterministicIdentifierSource());
    }

    private static WindowsLocalSettingsStore CreateStore(
        TestOwnedTemporaryRoot root,
        ISettingsWriteTestHook writeTestHook)
    {
        return new WindowsLocalSettingsStore(
            ParseChild(root, DocumentName),
            new WindowsLocalIoExecutionBoundary(),
            new DeterministicIdentifierSource(),
            writeTestHook);
    }

    private static WindowsLocalSettingsStore CreateStoreAt(
        TestOwnedTemporaryRoot root,
        string childName)
    {
        return new WindowsLocalSettingsStore(
            ParseChild(root, childName),
            new WindowsLocalIoExecutionBoundary(),
            new DeterministicIdentifierSource());
    }

    private static WindowsLocalSettingsStore CreateStoreAt(
        TestOwnedTemporaryRoot root,
        string childName,
        ISettingsWriteTestHook writeTestHook)
    {
        return new WindowsLocalSettingsStore(
            ParseChild(root, childName),
            new WindowsLocalIoExecutionBoundary(),
            new DeterministicIdentifierSource(),
            writeTestHook);
    }

    private static WindowsLocalPath ParseChild(TestOwnedTemporaryRoot root, string childName)
    {
        return Assert.IsInstanceOfType<WindowsLocalPath>(
            Assert.IsInstanceOfType<PathParseSuccess>(
                FileSystemPath.Parse(root.Resolve(childName))).Path);
    }
}
