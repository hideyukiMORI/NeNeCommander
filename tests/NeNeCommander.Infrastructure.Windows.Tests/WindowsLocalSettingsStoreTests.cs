using System;
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

/// <summary>Proves the Windows settings store reads bounded documents and installs complete writes.</summary>
[TestClass]
public sealed class WindowsLocalSettingsStoreTests
{
    private const string DocumentName = "settings.json";

    /// <summary>Proves a complete stored document becomes the typed settings it describes.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenDocumentIsCompleteReturnsItsSettingsAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.WriteFile(
            DocumentName,
            "{\"schemaVersion\":1,\"showHiddenItems\":true,\"colorScheme\":\"monokai\"}");
        WindowsLocalSettingsStore store = CreateStore(root);

        SettingsReadOutcome outcome = await store.ReadAsync(CancellationToken.None);

        UserSettings settings = Assert.IsInstanceOfType<SettingsRead>(outcome).Settings;
        Assert.AreSame(ColorScheme.Monokai, settings.ColorScheme);
        Assert.AreSame(HiddenItemVisibility.Shown, settings.HiddenItemVisibility);
    }

    /// <summary>Proves a valid UTF-8 document with a byte-order mark is decoded before validation.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenDocumentHasUtf8ByteOrderMarkReturnsItsSettingsAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string Document =
            "{\"schemaVersion\":1,\"showHiddenItems\":true,\"colorScheme\":\"dracula\"}";
        byte[] preamble = Encoding.UTF8.GetPreamble();
        byte[] content = Encoding.UTF8.GetBytes(Document);
        byte[] stored = new byte[preamble.Length + content.Length];
        preamble.CopyTo(stored, 0);
        content.CopyTo(stored, preamble.Length);
        string documentPath = root.Resolve(DocumentName);
        File.WriteAllBytes(documentPath, stored);
        WindowsLocalSettingsStore store = CreateStore(root);

        SettingsReadOutcome outcome = await store.ReadAsync(CancellationToken.None);

        UserSettings settings = Assert.IsInstanceOfType<SettingsRead>(outcome).Settings;
        Assert.AreSame(ColorScheme.Dracula, settings.ColorScheme);
        Assert.AreSame(HiddenItemVisibility.Shown, settings.HiddenItemVisibility);
        CollectionAssert.AreEqual(stored, File.ReadAllBytes(documentPath));
    }

    /// <summary>Proves an absent document is reported and is never created by the read.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenDocumentIsAbsentReportsAbsenceWithoutWritingAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalSettingsStore store = CreateStore(root);

        SettingsReadOutcome outcome = await store.ReadAsync(CancellationToken.None);

        _ = Assert.IsInstanceOfType<SettingsAbsent>(outcome);
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName)));
        Assert.IsEmpty(Directory.GetFileSystemEntries(root.Path.CanonicalText));
    }

    /// <summary>Proves a missing settings directory is absence rather than an unreadable failure.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenSettingsDirectoryIsAbsentReportsAbsenceAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalSettingsStore store = new(
            ParseChild(root, "absent-directory\\" + DocumentName),
            new WindowsLocalIoExecutionBoundary());

        SettingsReadOutcome outcome = await store.ReadAsync(CancellationToken.None);

        _ = Assert.IsInstanceOfType<SettingsAbsent>(outcome);
        Assert.IsFalse(Directory.Exists(root.Resolve("absent-directory")));
    }

    /// <summary>Proves an absent parent observed by read cannot appear before the first write.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenMissingParentAppearsAfterReadRejectsBeforeMutationAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string NestedDocument = "appeared-parent\\settings.json";
        WindowsLocalSettingsStore store = CreateStoreAt(root, NestedDocument);
        _ = Assert.IsInstanceOfType<SettingsAbsent>(await store.ReadAsync(CancellationToken.None));
        _ = root.CreateDirectory("appeared-parent");

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.UnsafeLocation, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.None, outcome.TemporaryEffect);
        Assert.IsFalse(File.Exists(root.Resolve(NestedDocument)));
        Assert.IsFalse(File.Exists(root.Resolve(NestedDocument + ".tmp")));
    }

    /// <summary>Proves a document appearing with its missing parent is a destination change.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenDocumentAndMissingParentAppearAfterReadRejectsTheDestinationAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string NestedDocument = "appeared-document-parent\\settings.json";
        WindowsLocalSettingsStore store = CreateStoreAt(root, NestedDocument);
        _ = Assert.IsInstanceOfType<SettingsAbsent>(await store.ReadAsync(CancellationToken.None));
        _ = root.CreateDirectory("appeared-document-parent");
        const string Foreign =
            "{\"schemaVersion\":1,\"showHiddenItems\":true,\"colorScheme\":\"ubuntu\"}";
        _ = root.WriteFile(NestedDocument, Foreign);

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.DestinationChanged, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.None, outcome.TemporaryEffect);
        Assert.AreEqual(Foreign, File.ReadAllText(root.Resolve(NestedDocument)));
        Assert.IsFalse(File.Exists(root.Resolve(NestedDocument + ".tmp")));
    }

    /// <summary>Proves an oversized document is rejected on length and left byte-for-byte unchanged.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenDocumentExceedsBoundaryRejectsItWithoutRepairAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string oversized = new('x', SettingsDocumentValidator.MaximumDocumentLength + 1);
        string documentPath = root.WriteFile(DocumentName, oversized);
        WindowsLocalSettingsStore store = CreateStore(root);

        SettingsReadOutcome outcome = await store.ReadAsync(CancellationToken.None);

        Assert.AreSame(
            SettingsReadFailureKind.TooLarge,
            Assert.IsInstanceOfType<SettingsRejected>(outcome).Kind);
        Assert.AreEqual(oversized, File.ReadAllText(documentPath));
    }

    /// <summary>Proves the exact document-size boundary remains readable.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenDocumentIsExactlyAtBoundaryAcceptsItAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string Document =
            "{\"schemaVersion\":1,\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"}";
        string bounded = Document + new string(
            ' ',
            SettingsDocumentValidator.MaximumDocumentLength - Document.Length);
        _ = root.WriteFile(DocumentName, bounded);
        WindowsLocalSettingsStore store = CreateStore(root);

        SettingsReadOutcome outcome = await store.ReadAsync(CancellationToken.None);

        UserSettings settings = Assert.IsInstanceOfType<SettingsRead>(outcome).Settings;
        Assert.AreSame(ColorScheme.NeNeDark, settings.ColorScheme);
        Assert.AreSame(HiddenItemVisibility.Hidden, settings.HiddenItemVisibility);
    }

    /// <summary>Proves a malformed document is rejected without repairing the stored text.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenDocumentIsMalformedRejectsItWithoutRepairAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string documentPath = root.WriteFile(DocumentName, "{\"schemaVersion\":1,\"colorScheme\":\"nene-");
        WindowsLocalSettingsStore store = CreateStore(root);

        SettingsReadOutcome outcome = await store.ReadAsync(CancellationToken.None);

        Assert.AreSame(
            SettingsReadFailureKind.Malformed,
            Assert.IsInstanceOfType<SettingsRejected>(outcome).Kind);
        Assert.AreEqual("{\"schemaVersion\":1,\"colorScheme\":\"nene-", File.ReadAllText(documentPath));
    }

    /// <summary>Proves an expected input failure normalizes to the unreadable rejection.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenDocumentCannotBeOpenedReturnsUnreadableAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory(DocumentName);
        WindowsLocalSettingsStore store = CreateStore(root);

        SettingsReadOutcome outcome = await store.ReadAsync(CancellationToken.None);

        Assert.AreSame(
            SettingsReadFailureKind.Unreadable,
            Assert.IsInstanceOfType<SettingsRejected>(outcome).Kind);
    }

    /// <summary>Proves a document held for exclusive writing is unreadable rather than empty settings.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenDocumentIsLockedReturnsUnreadableAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string documentPath = root.WriteFile(DocumentName, "{}");
        WindowsLocalSettingsStore store = CreateStore(root);
        using FileStream exclusive = new(documentPath, FileMode.Open, FileAccess.Write, FileShare.None);

        SettingsReadOutcome outcome = await store.ReadAsync(CancellationToken.None);

        Assert.AreSame(
            SettingsReadFailureKind.Unreadable,
            Assert.IsInstanceOfType<SettingsRejected>(outcome).Kind);
    }

    /// <summary>Proves an unreadable entry cannot be replaced by an ordinary settings write.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncAfterUnreadableDirectoryReadRetainsTheAccessRejectionAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory(DocumentName);
        WindowsLocalSettingsStore store = CreateStore(root);
        _ = Assert.IsInstanceOfType<SettingsRejected>(
            await store.ReadAsync(CancellationToken.None));
        Directory.Delete(root.Resolve(DocumentName));
        const string Foreign =
            "{\"schemaVersion\":1,\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"}";
        _ = root.WriteFile(DocumentName, Foreign);

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.Unauthorized, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.None, outcome.TemporaryEffect);
        Assert.AreEqual(Foreign, File.ReadAllText(root.Resolve(DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + ".tmp")));
    }

    /// <summary>Proves an unreadable startup baseline cannot become writable after its lock clears.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncAfterLockedStartupReadRetainsTheIoRejectionAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string Original =
            "{\"schemaVersion\":1,\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"}";
        string documentPath = root.WriteFile(DocumentName, Original);
        WindowsLocalSettingsStore store = CreateStore(root);
        using (FileStream exclusive = new(documentPath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            SettingsRejected read = Assert.IsInstanceOfType<SettingsRejected>(
                await store.ReadAsync(CancellationToken.None));
            Assert.AreSame(SettingsReadFailureKind.Unreadable, read.Kind);
        }

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.IoFailure, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.None, outcome.TemporaryEffect);
        Assert.AreEqual(Original, File.ReadAllText(documentPath));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + ".tmp")));
    }

    /// <summary>Proves the composed production location names the product settings document.</summary>
    [TestMethod]
    public void ResolveWhenCalledNamesTheProductSettingsDocument()
    {
        WindowsLocalPath location = WindowsLocalSettingsLocation.Resolve();

        Assert.EndsWith("\\NeNeCommander\\settings.json", location.CanonicalText);
    }

    /// <summary>Proves the asynchronous write entry rejects an absent settings value.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenSettingsAreNullThrowsArgumentNullExceptionAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalSettingsStore store = CreateStore(root);

        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => store.WriteAsync(null!, CancellationToken.None));

        Assert.IsEmpty(Directory.GetFileSystemEntries(root.Path.CanonicalText));
    }

    /// <summary>Proves an absent document is installed with the exact schema and no temporary residue.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenDocumentIsAbsentInstallsExactCompleteDocumentAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalSettingsStore store = CreateStore(root);
        UserSettings settings = UserSettings.Create(ColorScheme.Dracula, HiddenItemVisibility.Shown);

        SettingsWriteOutcome outcome = await store.WriteAsync(settings, CancellationToken.None);

        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(outcome);
        byte[] bytes = File.ReadAllBytes(root.Resolve(DocumentName));
        CollectionAssert.AreEqual(
            System.Text.Encoding.UTF8.GetBytes(
                "{\"schemaVersion\":1,\"showHiddenItems\":true,\"colorScheme\":\"dracula\"}"),
            bytes);
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + ".tmp")));
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
            UserSettings.Create(ColorScheme.SolarizedLight, HiddenItemVisibility.Hidden),
            CancellationToken.None);

        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(outcome);
        Assert.AreEqual(
            "{\"schemaVersion\":1,\"showHiddenItems\":false,\"colorScheme\":\"solarized-light\"}",
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
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + ".tmp")));
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
            UserSettings.Create(ColorScheme.Ubuntu, HiddenItemVisibility.Shown),
            CancellationToken.None);

        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(outcome);
        Assert.AreEqual(
            "{\"schemaVersion\":1,\"showHiddenItems\":true,\"colorScheme\":\"ubuntu\"}",
            File.ReadAllText(root.Resolve(DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + ".tmp")));
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
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + ".tmp")));
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
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + ".tmp")));
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
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + ".tmp")));
    }

    /// <summary>Proves one store advances its baseline after publish so ordered choices can continue.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenTwoValuesAreOrderedPublishesBothWithoutSelfConflictAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalSettingsStore store = CreateStore(root);
        _ = Assert.IsInstanceOfType<SettingsAbsent>(await store.ReadAsync(CancellationToken.None));

        SettingsWriteOutcome first = await store.WriteAsync(
            UserSettings.Create(ColorScheme.Ubuntu, HiddenItemVisibility.Hidden),
            CancellationToken.None);
        SettingsWriteOutcome second = await store.WriteAsync(
            UserSettings.Create(ColorScheme.Dracula, HiddenItemVisibility.Shown),
            CancellationToken.None);
        SettingsWriteOutcome third = await store.WriteAsync(
            UserSettings.Create(ColorScheme.Monokai, HiddenItemVisibility.Hidden),
            CancellationToken.None);

        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(first);
        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(second);
        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(third);
        Assert.AreEqual(
            "{\"schemaVersion\":1,\"showHiddenItems\":false,\"colorScheme\":\"monokai\"}",
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
            UserSettings.Create(ColorScheme.Ubuntu, HiddenItemVisibility.Hidden),
            CancellationToken.None);
        SettingsWriteOutcome second = await store.WriteAsync(
            UserSettings.Create(ColorScheme.Dracula, HiddenItemVisibility.Shown),
            CancellationToken.None);

        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(first);
        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(second);
        Assert.AreEqual(
            "{\"schemaVersion\":1,\"showHiddenItems\":true,\"colorScheme\":\"dracula\"}",
            File.ReadAllText(root.Resolve(NestedDocument)));
        Assert.IsFalse(File.Exists(root.Resolve(NestedDocument + ".tmp")));
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
            UserSettings.Create(ColorScheme.Dracula, HiddenItemVisibility.Shown),
            CancellationToken.None);

        Assert.AreSame(SettingsWriteFailureKind.IoFailure, first.Failure);
        Assert.AreSame(SettingsDirectoryEffect.CreationObserved, first.DirectoryEffect);
        Assert.AreSame(SettingsWriteEffect.None, first.TemporaryEffect);
        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(second);
        Assert.AreEqual(2, attempts);
        Assert.Contains("\"colorScheme\":\"dracula\"", File.ReadAllText(root.Resolve(NestedDocument)));
        Assert.IsFalse(File.Exists(root.Resolve(NestedDocument + ".tmp")));
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
        Assert.IsFalse(File.Exists(root.Resolve(NestedDocument + ".tmp")));
    }

    /// <summary>Proves a detected existing-ancestor replacement cannot become the created baseline.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenAncestorIsReplacedAfterDirectoryCreationRejectsNewChainAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string StableParent = "stable-parent";
        const string NestedDocument = "stable-parent\\created-parent\\settings.json";
        _ = root.CreateDirectory(StableParent);
        WindowsLocalSettingsStore store = CreateStoreAt(
            root,
            NestedDocument,
            new ScriptedSettingsWriteTestHook
            {
                OnDirectoryCreated = _ =>
                {
                    Directory.Move(root.Resolve(StableParent), root.Resolve("original-parent"));
                    _ = root.CreateDirectory("stable-parent\\created-parent");
                },
            });

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.UnsafeLocation, outcome.Failure);
        Assert.AreSame(SettingsDirectoryEffect.CreationUnconfirmed, outcome.DirectoryEffect);
        Assert.AreSame(SettingsWriteEffect.None, outcome.TemporaryEffect);
        Assert.IsFalse(File.Exists(root.Resolve(NestedDocument)));
        Assert.IsFalse(File.Exists(root.Resolve(NestedDocument + ".tmp")));
        Assert.IsTrue(Directory.Exists(root.Resolve("original-parent\\created-parent")));
    }

    /// <summary>Proves the post-publish baseline detects a foreign edit before the next write.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncAfterOwnedPublishRejectsForeignDocumentEditAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalSettingsStore store = CreateStore(root);
        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));
        const string Foreign =
            "{\"schemaVersion\":1,\"showHiddenItems\":true,\"colorScheme\":\"ubuntu\"}";
        File.WriteAllText(root.Resolve(DocumentName), Foreign);

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(
                UserSettings.Create(ColorScheme.Dracula, HiddenItemVisibility.Hidden),
                CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.DestinationChanged, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.None, outcome.TemporaryEffect);
        Assert.AreEqual(Foreign, File.ReadAllText(root.Resolve(DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + ".tmp")));
    }

    /// <summary>Proves identical foreign bytes cannot become the owned post-publish baseline.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenPublishedDocumentIsReplacedWithIdenticalBytesKeepsBaselineUncertainAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        bool replaceAfterPublish = true;
        WindowsLocalSettingsStore store = CreateStore(
            root,
            new ScriptedSettingsWriteTestHook
            {
                OnPublished = documentPath =>
                {
                    if (!replaceAfterPublish)
                    {
                        return;
                    }
                    replaceAfterPublish = false;
                    byte[] publishedBytes = File.ReadAllBytes(documentPath);
                    File.Move(documentPath, documentPath + ".owned");
                    File.WriteAllBytes(documentPath, publishedBytes);
                },
            });

        SettingsWriteOutcome first = await store.WriteAsync(UserSettings.Default, CancellationToken.None);
        SettingsWriteRejected second = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(
                UserSettings.Create(ColorScheme.Dracula, HiddenItemVisibility.Shown),
                CancellationToken.None));

        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(first);
        Assert.AreSame(SettingsWriteFailureKind.DestinationChanged, second.Failure);
        Assert.AreSame(SettingsWriteEffect.None, second.TemporaryEffect);
        CollectionAssert.AreEqual(
            File.ReadAllBytes(root.Resolve(DocumentName + ".owned")),
            File.ReadAllBytes(root.Resolve(DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + ".tmp")));
    }

    /// <summary>Proves the post-publish location baseline detects a replaced created parent.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncAfterOwnedDirectoryCreationRejectsReplacedParentAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string NestedDocument = "created-parent\\settings.json";
        WindowsLocalSettingsStore store = CreateStoreAt(root, NestedDocument);
        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));
        Directory.Move(root.Resolve("created-parent"), root.Resolve("owned-parent"));
        _ = root.CreateDirectory("created-parent");

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(
                UserSettings.Create(ColorScheme.Dracula, HiddenItemVisibility.Shown),
                CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.DestinationChanged, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.None, outcome.TemporaryEffect);
        Assert.IsFalse(File.Exists(root.Resolve(NestedDocument)));
        Assert.IsFalse(File.Exists(root.Resolve(NestedDocument + ".tmp")));
        Assert.IsTrue(File.Exists(root.Resolve("owned-parent\\settings.json")));
    }

    /// <summary>Proves a malformed existing document blocks mutation and remains byte-for-byte unchanged.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenExistingDocumentIsRejectedLeavesItUntouchedAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string Malformed = "{\"schemaVersion\":1";
        _ = root.WriteFile(DocumentName, Malformed);
        WindowsLocalSettingsStore store = CreateStore(root);
        SettingsReadOutcome read = await store.ReadAsync(CancellationToken.None);

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(
            SettingsReadFailureKind.Malformed,
            Assert.IsInstanceOfType<SettingsRejected>(read).Kind);
        Assert.AreSame(SettingsWriteFailureKind.ExistingDocumentRejected, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.None, outcome.TemporaryEffect);
        Assert.AreEqual(Malformed, File.ReadAllText(root.Resolve(DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + ".tmp")));
    }

    /// <summary>Proves a rejected startup document cannot become an implicit repair write.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenRejectedDocumentChangesAfterReadStillRefusesRepairAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string documentPath = root.WriteFile(DocumentName, "{\"schemaVersion\":1");
        WindowsLocalSettingsStore store = CreateStore(root);
        _ = Assert.IsInstanceOfType<SettingsRejected>(
            await store.ReadAsync(CancellationToken.None));
        const string ExternalRepair =
            "{\"schemaVersion\":1,\"showHiddenItems\":false,\"colorScheme\":\"nene-light\"}";
        File.WriteAllText(documentPath, ExternalRepair);

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.ExistingDocumentRejected, outcome.Failure);
        Assert.AreEqual(ExternalRepair, File.ReadAllText(documentPath));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + ".tmp")));
    }

    /// <summary>Proves a preexisting temporary artifact is neither overwritten nor deleted.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenTemporaryPathIsOccupiedRejectsWithoutMutationAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string Occupant = "owned elsewhere";
        _ = root.WriteFile(DocumentName + ".tmp", Occupant);
        WindowsLocalSettingsStore store = CreateStore(root);

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.TemporaryArtifactCollision, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.None, outcome.TemporaryEffect);
        Assert.AreEqual(Occupant, File.ReadAllText(root.Resolve(DocumentName + ".tmp")));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName)));
    }

    /// <summary>Proves a destination created after absent preflight is preserved and blocks publish.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenAbsentDestinationAppearsBeforePublishPreservesNewDocumentAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string ConcurrentDocument =
            "{\"schemaVersion\":1,\"showHiddenItems\":true,\"colorScheme\":\"ubuntu\"}";
        WindowsLocalSettingsStore store = CreateStore(
            root,
            new ScriptedSettingsWriteTestHook
            {
                OnBeforePublish = path => File.WriteAllText(path, ConcurrentDocument),
            });

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.DestinationChanged, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.None, outcome.TemporaryEffect);
        Assert.AreEqual(ConcurrentDocument, File.ReadAllText(root.Resolve(DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + ".tmp")));
    }

    /// <summary>Proves a destination created after the startup read is never silently overwritten.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenAbsentDocumentAppearsAfterReadRejectsBeforeTemporaryCreationAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string ConcurrentDocument =
            "{\"schemaVersion\":1,\"showHiddenItems\":false,\"colorScheme\":\"monokai\"}";
        bool temporaryFlushed = false;
        WindowsLocalSettingsStore store = CreateStore(
            root,
            new ScriptedSettingsWriteTestHook
            {
                OnTemporaryFlushed = _ => temporaryFlushed = true,
            });
        _ = Assert.IsInstanceOfType<SettingsAbsent>(
            await store.ReadAsync(CancellationToken.None));
        _ = root.WriteFile(DocumentName, ConcurrentDocument);

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.DestinationChanged, outcome.Failure);
        Assert.AreEqual(ConcurrentDocument, File.ReadAllText(root.Resolve(DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + ".tmp")));
        Assert.IsFalse(temporaryFlushed);
    }

    /// <summary>Proves disappearance of a valid startup document is rejected before mutation.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenValidDocumentDisappearsAfterReadRejectsBeforeMutationAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.WriteFile(
            DocumentName,
            "{\"schemaVersion\":1,\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"}");
        bool temporaryFlushed = false;
        WindowsLocalSettingsStore store = CreateStore(
            root,
            new ScriptedSettingsWriteTestHook
            {
                OnTemporaryFlushed = _ => temporaryFlushed = true,
            });
        _ = Assert.IsInstanceOfType<SettingsRead>(await store.ReadAsync(CancellationToken.None));
        File.Delete(root.Resolve(DocumentName));

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.DestinationChanged, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.None, outcome.TemporaryEffect);
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + ".tmp")));
        Assert.IsFalse(temporaryFlushed);
    }

    /// <summary>Proves provider identity changes are detected even when bytes are unchanged.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenValidDocumentIdentityChangesAfterReadRejectsBeforeMutationAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string Original =
            "{\"schemaVersion\":1,\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"}";
        string documentPath = root.WriteFile(DocumentName, Original);
        WindowsLocalSettingsStore store = CreateStore(root);
        _ = Assert.IsInstanceOfType<SettingsRead>(await store.ReadAsync(CancellationToken.None));
        File.Move(documentPath, documentPath + ".original");
        File.WriteAllText(documentPath, Original);

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.DestinationChanged, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.None, outcome.TemporaryEffect);
        Assert.AreEqual(Original, File.ReadAllText(documentPath));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + ".tmp")));
    }

    /// <summary>Proves a valid document edited after startup read is preserved before mutation starts.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenValidDocumentChangesAfterReadRejectsBeforeTemporaryCreationAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string Original =
            "{\"schemaVersion\":1,\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"}";
        const string ConcurrentDocument =
            "{\"schemaVersion\":1,\"showHiddenItems\":true,\"colorScheme\":\"nene-light\"}";
        string documentPath = root.WriteFile(DocumentName, Original);
        WindowsLocalSettingsStore store = CreateStore(root);
        _ = Assert.IsInstanceOfType<SettingsRead>(await store.ReadAsync(CancellationToken.None));
        File.WriteAllText(documentPath, ConcurrentDocument);

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.DestinationChanged, outcome.Failure);
        Assert.AreEqual(ConcurrentDocument, File.ReadAllText(documentPath));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + ".tmp")));
    }

    /// <summary>Proves in-place content change after valid preflight blocks publish even with restored metadata.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenExistingContentChangesBeforePublishPreservesConcurrentEditAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string Original =
            "{\"schemaVersion\":1,\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"}";
        const string ConcurrentDocument =
            "{\"schemaVersion\":1,\"showHiddenItems\":false,\"colorScheme\":\"nene-light\"}";
        string documentPath = root.WriteFile(DocumentName, Original);
        FileInfo metadata = new(documentPath);
        DateTime creationTimeUtc = metadata.CreationTimeUtc;
        DateTime lastWriteTimeUtc = metadata.LastWriteTimeUtc;
        WindowsLocalSettingsStore store = CreateStore(
            root,
            new ScriptedSettingsWriteTestHook
            {
                OnBeforePublish = path =>
                {
                    File.WriteAllText(path, ConcurrentDocument);
                    File.SetCreationTimeUtc(path, creationTimeUtc);
                    File.SetLastWriteTimeUtc(path, lastWriteTimeUtc);
                },
            });

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.DestinationChanged, outcome.Failure);
        Assert.AreEqual(ConcurrentDocument, File.ReadAllText(documentPath));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + ".tmp")));
    }

    /// <summary>Proves cancellation after a move publish cannot hide its successful effect.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenCancelledAfterMovePublishesStillReportsPublishedSuccessAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        using CancellationTokenSource cancellation = new();
        WindowsLocalSettingsStore store = CreateStore(
            root,
            new ScriptedSettingsWriteTestHook
            {
                OnPublished = _ => cancellation.Cancel(),
            });

        SettingsWriteOutcome outcome = await store.WriteAsync(UserSettings.Default, cancellation.Token);

        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(outcome);
        Assert.IsTrue(cancellation.IsCancellationRequested);
        Assert.IsTrue(File.Exists(root.Resolve(DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + ".tmp")));
    }

    /// <summary>Proves cancellation after replace cannot turn its successful effect into cancellation.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenCancelledAfterReplacePublishesStillReportsPublishedSuccessAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.WriteFile(
            DocumentName,
            "{\"schemaVersion\":1,\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"}");
        using CancellationTokenSource cancellation = new();
        WindowsLocalSettingsStore store = CreateStore(
            root,
            new ScriptedSettingsWriteTestHook
            {
                OnPublished = _ => cancellation.Cancel(),
            });

        SettingsWriteOutcome outcome = await store.WriteAsync(
            UserSettings.Create(ColorScheme.Dracula, HiddenItemVisibility.Shown),
            cancellation.Token);

        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(outcome);
        Assert.IsTrue(cancellation.IsCancellationRequested);
        Assert.Contains("\"colorScheme\":\"dracula\"", File.ReadAllText(root.Resolve(DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + ".tmp")));
    }

    /// <summary>Proves cleanup reports owned residue when the operating system refuses deletion.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenOwnedTemporaryCleanupFailsReportsResidueAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string NestedDocument = "residue-parent\\settings.json";
        FileStream? blocker = null;
        WindowsLocalSettingsStore store = CreateStoreAt(
            root,
            NestedDocument,
            new ScriptedSettingsWriteTestHook
            {
                OnTemporaryFlushed = temporaryPath =>
                {
                    blocker = new FileStream(
                        temporaryPath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.None);
                    throw new IOException("Injected failure after the owned temporary flush.");
                },
            });

        SettingsWriteRejected outcome;
        try
        {
            outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
                await store.WriteAsync(UserSettings.Default, CancellationToken.None));
        }
        finally
        {
            blocker?.Dispose();
        }

        Assert.AreSame(SettingsWriteFailureKind.IoFailure, outcome.Failure);
        Assert.AreSame(SettingsDirectoryEffect.CreationObserved, outcome.DirectoryEffect);
        Assert.AreSame(SettingsWriteEffect.TemporaryArtifactLeft, outcome.TemporaryEffect);
        Assert.IsTrue(File.Exists(root.Resolve(NestedDocument + ".tmp")));
        Assert.IsFalse(File.Exists(root.Resolve(NestedDocument)));
    }

    /// <summary>Proves every preexisting entry kind at the temporary path is a collision.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenTemporaryPathIsDirectoryReportsCollisionWithoutDeletingItAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory(DocumentName + ".tmp");
        WindowsLocalSettingsStore store = CreateStore(root);

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.TemporaryArtifactCollision, outcome.Failure);
        Assert.AreSame(SettingsDirectoryEffect.NotAttempted, outcome.DirectoryEffect);
        Assert.AreSame(SettingsWriteEffect.None, outcome.TemporaryEffect);
        Assert.IsTrue(Directory.Exists(root.Resolve(DocumentName + ".tmp")));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName)));
    }

    /// <summary>Proves a detected foreign replacement at the temporary path is preserved.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenTemporaryIsReplacedBeforeRejectionPreservesForeignEntryAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string ConcurrentDocument =
            "{\"schemaVersion\":1,\"showHiddenItems\":true,\"colorScheme\":\"ubuntu\"}";
        const string ForeignTemporary = "foreign temporary";
        WindowsLocalSettingsStore store = CreateStore(
            root,
            new ScriptedSettingsWriteTestHook
            {
                OnBeforePublish = documentPath =>
                {
                    string temporaryPath = documentPath + ".tmp";
                    File.Move(temporaryPath, temporaryPath + ".owned");
                    File.WriteAllText(temporaryPath, ForeignTemporary);
                    File.WriteAllText(documentPath, ConcurrentDocument);
                },
            });

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.DestinationChanged, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.TemporaryArtifactLeft, outcome.TemporaryEffect);
        Assert.AreEqual(ForeignTemporary, File.ReadAllText(root.Resolve(DocumentName + ".tmp")));
        Assert.IsTrue(File.Exists(root.Resolve(DocumentName + ".tmp.owned")));
        Assert.AreEqual(ConcurrentDocument, File.ReadAllText(root.Resolve(DocumentName)));
    }

    /// <summary>Proves a detected temporary replacement cannot become the published document.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenTemporaryIsReplacedBeforePublishRejectsForeignEntryAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string ForeignTemporary = "foreign temporary";
        WindowsLocalSettingsStore store = CreateStore(
            root,
            new ScriptedSettingsWriteTestHook
            {
                OnBeforePublish = documentPath =>
                {
                    string temporaryPath = documentPath + ".tmp";
                    File.Move(temporaryPath, temporaryPath + ".owned");
                    File.WriteAllText(temporaryPath, ForeignTemporary);
                },
            });

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.TemporaryArtifactCollision, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.TemporaryArtifactLeft, outcome.TemporaryEffect);
        Assert.AreEqual(ForeignTemporary, File.ReadAllText(root.Resolve(DocumentName + ".tmp")));
        Assert.IsTrue(File.Exists(root.Resolve(DocumentName + ".tmp.owned")));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName)));
    }

    /// <summary>Proves a reparse ancestor is rejected before reads or writes reach its target.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task StoreWhenDocumentAncestorIsJunctionRejectsWithoutFollowingItAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("target");
        _ = root.CreateJunction("settings-link", "target");
        WindowsLocalSettingsStore store = CreateStoreAt(root, "settings-link\\" + DocumentName);

        SettingsReadOutcome read = await store.ReadAsync(CancellationToken.None);
        SettingsWriteRejected write = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(
            SettingsReadFailureKind.Unreadable,
            Assert.IsInstanceOfType<SettingsRejected>(read).Kind);
        Assert.AreSame(SettingsWriteFailureKind.UnsafeLocation, write.Failure);
        Assert.IsFalse(File.Exists(root.Resolve("target\\" + DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve("target\\" + DocumentName + ".tmp")));
    }

    /// <summary>Proves replacing a captured parent with a junction blocks mutation before temp creation.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenParentBecomesJunctionAfterReadRejectsBeforeMutationAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("settings-parent");
        _ = root.CreateDirectory("target");
        WindowsLocalSettingsStore store = CreateStoreAt(root, "settings-parent\\" + DocumentName);
        _ = Assert.IsInstanceOfType<SettingsAbsent>(await store.ReadAsync(CancellationToken.None));
        Directory.Move(root.Resolve("settings-parent"), root.Resolve("parked-parent"));
        _ = root.CreateJunction("settings-parent", "target");

        SettingsWriteRejected write = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.UnsafeLocation, write.Failure);
        Assert.AreSame(SettingsWriteEffect.None, write.TemporaryEffect);
        Assert.IsFalse(File.Exists(root.Resolve("target\\" + DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve("target\\" + DocumentName + ".tmp")));
    }

    /// <summary>Proves a blocked startup location cannot be replaced and retried implicitly.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenJunctionFromStartupIsReplacedByDirectoryRetainsUnsafeLocationAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("target");
        _ = root.CreateJunction("settings-link", "target");
        WindowsLocalSettingsStore store = CreateStoreAt(root, "settings-link\\" + DocumentName);
        _ = Assert.IsInstanceOfType<SettingsRejected>(
            await store.ReadAsync(CancellationToken.None));
        Directory.Delete(root.Resolve("settings-link"));
        _ = root.CreateDirectory("settings-link");

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.UnsafeLocation, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.None, outcome.TemporaryEffect);
        Assert.IsFalse(File.Exists(root.Resolve("settings-link\\" + DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve("settings-link\\" + DocumentName + ".tmp")));
        Assert.IsFalse(File.Exists(root.Resolve("target\\" + DocumentName)));
    }

    /// <summary>Proves replacement by another ordinary directory is rejected on identity.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenParentDirectoryIdentityChangesAfterReadRejectsBeforeMutationAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("settings-parent");
        WindowsLocalSettingsStore store = CreateStoreAt(root, "settings-parent\\" + DocumentName);
        _ = Assert.IsInstanceOfType<SettingsAbsent>(await store.ReadAsync(CancellationToken.None));
        Directory.Move(root.Resolve("settings-parent"), root.Resolve("original-parent"));
        _ = root.CreateDirectory("settings-parent");

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.UnsafeLocation, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.None, outcome.TemporaryEffect);
        Assert.IsFalse(File.Exists(root.Resolve("settings-parent\\" + DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve("settings-parent\\" + DocumentName + ".tmp")));
    }

    /// <summary>Proves detected parent replacement after temp creation reports the old-parent residue.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenParentChangesAfterTemporaryFlushLeavesOwnedResidueAtOldParentAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("settings-parent");
        _ = root.CreateDirectory("target");
        WindowsLocalSettingsStore store = CreateStoreAt(
            root,
            "settings-parent\\" + DocumentName,
            new ScriptedSettingsWriteTestHook
            {
                OnTemporaryFlushed = _ =>
                {
                    Directory.Move(root.Resolve("settings-parent"), root.Resolve("parked-parent"));
                    _ = root.CreateJunction("settings-parent", "target");
                },
            });
        _ = Assert.IsInstanceOfType<SettingsAbsent>(await store.ReadAsync(CancellationToken.None));

        SettingsWriteRejected write = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.UnsafeLocation, write.Failure);
        Assert.AreSame(SettingsWriteEffect.TemporaryArtifactLeft, write.TemporaryEffect);
        Assert.IsTrue(File.Exists(root.Resolve("parked-parent\\" + DocumentName + ".tmp")));
        Assert.IsFalse(File.Exists(root.Resolve("target\\" + DocumentName + ".tmp")));
        Assert.IsFalse(File.Exists(root.Resolve("target\\" + DocumentName)));
    }

    /// <summary>Proves detected parent replacement preserves the foreign temp at the new parent.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenParentIsReplacedAfterFlushPreservesForeignTemporaryAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("settings-parent");
        const string ForeignTemporary = "foreign temporary";
        WindowsLocalSettingsStore store = CreateStoreAt(
            root,
            "settings-parent\\" + DocumentName,
            new ScriptedSettingsWriteTestHook
            {
                OnTemporaryFlushed = _ =>
                {
                    Directory.Move(root.Resolve("settings-parent"), root.Resolve("parked-parent"));
                    _ = root.CreateDirectory("settings-parent");
                    _ = root.WriteFile("settings-parent\\" + DocumentName + ".tmp", ForeignTemporary);
                },
            });
        _ = Assert.IsInstanceOfType<SettingsAbsent>(await store.ReadAsync(CancellationToken.None));

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.UnsafeLocation, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.TemporaryArtifactLeft, outcome.TemporaryEffect);
        Assert.AreEqual(
            ForeignTemporary,
            File.ReadAllText(root.Resolve("settings-parent\\" + DocumentName + ".tmp")));
        Assert.IsTrue(File.Exists(root.Resolve("parked-parent\\" + DocumentName + ".tmp")));
        Assert.IsFalse(File.Exists(root.Resolve("settings-parent\\" + DocumentName)));
    }

    /// <summary>Proves cancellation precedes directory creation and temporary-file mutation.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenCancelledBeforeMutationThrowsWithoutWritingAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string nestedDocument = "cancelled-parent\\settings.json";
        WindowsLocalSettingsStore store = new(
            ParseChild(root, nestedDocument),
            new WindowsLocalIoExecutionBoundary());
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => store.WriteAsync(UserSettings.Default, cancellation.Token));

        Assert.IsFalse(Directory.Exists(root.Resolve("cancelled-parent")));
        Assert.IsFalse(File.Exists(root.Resolve(nestedDocument)));
        Assert.IsFalse(File.Exists(root.Resolve(nestedDocument + ".tmp")));
    }

    /// <summary>Proves cancellation wins before a blocked startup baseline is evaluated.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenCancelledWithRejectedBaselineThrowsWithoutRepairAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string Malformed = "{\"schemaVersion\":1";
        string documentPath = root.WriteFile(DocumentName, Malformed);
        WindowsLocalSettingsStore store = CreateStore(root);
        _ = Assert.IsInstanceOfType<SettingsRejected>(
            await store.ReadAsync(CancellationToken.None));
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => store.WriteAsync(UserSettings.Default, cancellation.Token));

        Assert.AreEqual(Malformed, File.ReadAllText(documentPath));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + ".tmp")));
    }

    /// <summary>Proves cancellation observed immediately before directory creation leaves no directory effect.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenCancelledAtDirectoryBoundaryLeavesParentAbsentAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string NestedDocument = "cancelled-directory\\settings.json";
        using CancellationTokenSource cancellation = new();
        WindowsLocalSettingsStore store = CreateStoreAt(
            root,
            NestedDocument,
            new ScriptedSettingsWriteTestHook
            {
                OnBeforeDirectoryCreation = _ => cancellation.Cancel(),
            });

        _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => store.WriteAsync(UserSettings.Default, cancellation.Token));

        Assert.IsFalse(Directory.Exists(root.Resolve("cancelled-directory")));
        Assert.IsFalse(File.Exists(root.Resolve(NestedDocument)));
        Assert.IsFalse(File.Exists(root.Resolve(NestedDocument + ".tmp")));
    }

    /// <summary>Proves cancellation after directory creation cannot hide the observed directory effect.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenCancelledAfterDirectoryCreationCompletesWithTypedEffectAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string NestedDocument = "started-directory\\settings.json";
        using CancellationTokenSource cancellation = new();
        WindowsLocalSettingsStore store = CreateStoreAt(
            root,
            NestedDocument,
            new ScriptedSettingsWriteTestHook
            {
                OnDirectoryCreated = _ => cancellation.Cancel(),
                OnTemporaryFlushed = _ => throw new IOException("Injected post-mutation failure."),
            });

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, cancellation.Token));

        Assert.IsTrue(cancellation.IsCancellationRequested);
        Assert.AreSame(SettingsWriteFailureKind.IoFailure, outcome.Failure);
        Assert.AreSame(SettingsDirectoryEffect.CreationObserved, outcome.DirectoryEffect);
        Assert.AreSame(SettingsWriteEffect.None, outcome.TemporaryEffect);
        Assert.IsTrue(Directory.Exists(root.Resolve("started-directory")));
        Assert.IsFalse(File.Exists(root.Resolve(NestedDocument)));
        Assert.IsFalse(File.Exists(root.Resolve(NestedDocument + ".tmp")));
    }

    /// <summary>Proves cancellation observed at the temporary boundary leaves the existing parent untouched.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenCancelledAtTemporaryBoundaryCreatesNoTemporaryAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        using CancellationTokenSource cancellation = new();
        WindowsLocalSettingsStore store = CreateStore(
            root,
            new ScriptedSettingsWriteTestHook
            {
                OnBeforeTemporaryCreation = _ => cancellation.Cancel(),
            });

        _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => store.WriteAsync(UserSettings.Default, cancellation.Token));

        Assert.IsFalse(File.Exists(root.Resolve(DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + ".tmp")));
    }

    /// <summary>Proves a file that wins the final CreateNew race is reported and preserved as a collision.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenTemporaryAppearsAfterPreflightReportsCollisionAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string ForeignTemporary = "foreign race winner";
        WindowsLocalSettingsStore store = CreateStore(
            root,
            new ScriptedSettingsWriteTestHook
            {
                OnBeforeTemporaryCreation = temporaryPath =>
                    File.WriteAllText(temporaryPath, ForeignTemporary),
            });

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.TemporaryArtifactCollision, outcome.Failure);
        Assert.AreSame(SettingsDirectoryEffect.NotAttempted, outcome.DirectoryEffect);
        Assert.AreSame(SettingsWriteEffect.None, outcome.TemporaryEffect);
        Assert.AreEqual(ForeignTemporary, File.ReadAllText(root.Resolve(DocumentName + ".tmp")));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName)));
    }

    private static WindowsLocalSettingsStore CreateStore(TestOwnedTemporaryRoot root)
    {
        return new WindowsLocalSettingsStore(
            ParseChild(root, DocumentName),
            new WindowsLocalIoExecutionBoundary());
    }

    private static WindowsLocalSettingsStore CreateStore(
        TestOwnedTemporaryRoot root,
        ISettingsWriteTestHook writeTestHook)
    {
        return new WindowsLocalSettingsStore(
            ParseChild(root, DocumentName),
            new WindowsLocalIoExecutionBoundary(),
            writeTestHook);
    }

    private static WindowsLocalSettingsStore CreateStoreAt(
        TestOwnedTemporaryRoot root,
        string childName)
    {
        return new WindowsLocalSettingsStore(
            ParseChild(root, childName),
            new WindowsLocalIoExecutionBoundary());
    }

    private static WindowsLocalSettingsStore CreateStoreAt(
        TestOwnedTemporaryRoot root,
        string childName,
        ISettingsWriteTestHook writeTestHook)
    {
        return new WindowsLocalSettingsStore(
            ParseChild(root, childName),
            new WindowsLocalIoExecutionBoundary(),
            writeTestHook);
    }

    private static WindowsLocalPath ParseChild(TestOwnedTemporaryRoot root, string childName)
    {
        return Assert.IsInstanceOfType<WindowsLocalPath>(
            Assert.IsInstanceOfType<PathParseSuccess>(
                FileSystemPath.Parse(root.Resolve(childName))).Path);
    }
}
