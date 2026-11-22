using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.FileOperations;
using NeNeCommander.Infrastructure.Windows.Settings;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves a settings document that appears, disappears, or changes after the read is preserved, and publication outcomes stay exact.</summary>
[TestClass]
public sealed class WindowsLocalSettingsStoreDocumentChangeTests
{
    private const string DocumentName = "settings.json";
    private const string TemporarySuffix = ".00000000000000000000000000000001.tmp";

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
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
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
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
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
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
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
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
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
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
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
            "{\"schemaVersion\":1,\"showHiddenItems\":true,\"colorScheme\":\"nene-dark\" }";
        Assert.AreEqual(Original.Length, ConcurrentDocument.Length);
        string documentPath = root.WriteFile(DocumentName, Original);
        FileInfo metadata = new(documentPath);
        DateTime creationTimeUtc = metadata.CreationTimeUtc;
        DateTime lastWriteTimeUtc = metadata.LastWriteTimeUtc;
        FileIdentity originalIdentity = WindowsLocalEntryIdentity.Describe(metadata);
        WindowsLocalSettingsStore store = CreateStore(
            root,
            new ScriptedSettingsWriteTestHook
            {
                OnBeforePublish = path =>
                {
                    File.WriteAllText(path, ConcurrentDocument);
                    File.SetCreationTimeUtc(path, creationTimeUtc);
                    File.SetLastWriteTimeUtc(path, lastWriteTimeUtc);
                    Assert.AreEqual(originalIdentity, WindowsLocalEntryIdentity.Describe(new FileInfo(path)));
                },
            });

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.DestinationChanged, outcome.Failure);
        Assert.AreEqual(ConcurrentDocument, File.ReadAllText(documentPath));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
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
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
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
            UserSettings.Create(DisplayPreferences.Create(ColorScheme.Dracula, HiddenItemVisibility.Shown, KeyHintVisibility.Shown, InterfaceLanguage.System), BookmarkCatalog.Empty, PaneSortPreferences.Default),
            cancellation.Token);

        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(outcome);
        Assert.IsTrue(cancellation.IsCancellationRequested);
        Assert.Contains("\"colorScheme\":\"dracula\"", File.ReadAllText(root.Resolve(DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
    }

    /// <summary>Proves cleanup reports owned residue when the operating system refuses deletion.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenOwnedTemporaryCleanupFailsReportsResidueAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string NestedDocument = "residue-parent\\settings.json";
        using ExclusiveFileBlocker blocker = new();
        WindowsLocalSettingsStore store = CreateStoreAt(
            root,
            NestedDocument,
            new ScriptedSettingsWriteTestHook
            {
                OnTemporaryFlushed = temporaryPath =>
                {
                    blocker.Block(temporaryPath);
                    throw new IOException("Injected failure after the owned temporary flush.");
                },
            });

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.IoFailure, outcome.Failure);
        Assert.AreSame(SettingsDirectoryEffect.CreationObserved, outcome.DirectoryEffect);
        Assert.AreSame(SettingsWriteEffect.TemporaryArtifactLeft, outcome.TemporaryEffect);
        Assert.IsTrue(File.Exists(root.Resolve(NestedDocument + TemporarySuffix)));
        Assert.IsFalse(File.Exists(root.Resolve(NestedDocument)));
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

    private sealed class ExclusiveFileBlocker : IDisposable
    {
        private FileStream? _stream;

        internal void Block(string path)
        {
            if (_stream is not null)
            {
                throw new InvalidOperationException("The test blocker already owns a file stream.");
            }

            _stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
        }

        public void Dispose()
        {
            _stream?.Dispose();
            _stream = null;
        }
    }
}
