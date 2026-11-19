using System;
using System.Collections.Generic;
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
    private const string TemporarySuffix = ".00000000000000000000000000000001.tmp";
    /// <summary>Proves startup preflight and document I/O begin only inside the shared scheduler.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenIoIsQueuedReturnsBeforeFilesystemWorkRunsAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.WriteFile(
            DocumentName,
            "{\"schemaVersion\":1,\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"}");
        ManualIoScheduler scheduler = new();
        WindowsLocalSettingsStore store = new(
            ParseChild(root, DocumentName),
            new WindowsLocalIoExecutionBoundary(scheduler),
            new DeterministicIdentifierSource());

        Task<SettingsReadOutcome> read = store.ReadAsync(CancellationToken.None);

        bool returnedBeforeCompletion = !read.IsCompleted;
        int pendingCount = scheduler.PendingCount;
        scheduler.ExecuteAll();
        _ = Assert.IsInstanceOfType<SettingsRead>(await read);
        Assert.IsTrue(returnedBeforeCompletion);
        Assert.AreEqual(1, pendingCount);
    }

    /// <summary>Proves cancellation is observed inside the scheduled read before filesystem work.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenCancelledBeforeScheduledWorkStartsIsCancelledAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        ManualIoScheduler scheduler = new();
        using CancellationTokenSource cancellation = new();
        WindowsLocalSettingsStore store = new(
            ParseChild(root, DocumentName),
            new WindowsLocalIoExecutionBoundary(scheduler),
            new DeterministicIdentifierSource());

        Task<SettingsReadOutcome> read = store.ReadAsync(cancellation.Token);
        cancellation.Cancel();
        scheduler.ExecuteAll();

        _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => read);
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName)));
    }

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
        Assert.AreSame(ColorScheme.Monokai, settings.Display.ColorScheme);
        Assert.AreSame(HiddenItemVisibility.Shown, settings.Display.HiddenItemVisibility);
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
        Assert.AreSame(ColorScheme.Dracula, settings.Display.ColorScheme);
        Assert.AreSame(HiddenItemVisibility.Shown, settings.Display.HiddenItemVisibility);
        CollectionAssert.AreEqual(stored, File.ReadAllBytes(documentPath));
    }

    /// <summary>Proves the byte bound is enforced before a shorter decoded string is validated.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenMultibyteDocumentExceedsByteLimitReturnsTooLargeAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string content = new('\u00e9', (SettingsDocumentValidator.MaximumDocumentLength / 2) + 1);
        _ = root.WriteFile(DocumentName, content);
        WindowsLocalSettingsStore store = CreateStore(root);

        SettingsRejected outcome = Assert.IsInstanceOfType<SettingsRejected>(
            await store.ReadAsync(CancellationToken.None));

        Assert.AreSame(SettingsReadFailureKind.TooLarge, outcome.Kind);
        Assert.IsLessThanOrEqualTo(SettingsDocumentValidator.MaximumDocumentLength, content.Length);
        Assert.IsGreaterThan(
            SettingsDocumentValidator.MaximumDocumentLength,
            Encoding.UTF8.GetByteCount(content));
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
            new WindowsLocalIoExecutionBoundary(),
            new DeterministicIdentifierSource());

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
        Assert.IsFalse(File.Exists(root.Resolve(NestedDocument + TemporarySuffix)));
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
        Assert.IsFalse(File.Exists(root.Resolve(NestedDocument + TemporarySuffix)));
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
        Assert.AreSame(ColorScheme.NeNeDark, settings.Display.ColorScheme);
        Assert.AreSame(HiddenItemVisibility.Hidden, settings.Display.HiddenItemVisibility);
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
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
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
            Assert.IsTrue(exclusive.CanRead);
            SettingsRejected read = Assert.IsInstanceOfType<SettingsRejected>(
                await store.ReadAsync(CancellationToken.None));
            Assert.AreSame(SettingsReadFailureKind.Unreadable, read.Kind);
        }

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.IoFailure, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.None, outcome.TemporaryEffect);
        Assert.AreEqual(Original, File.ReadAllText(documentPath));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
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

    private static WindowsLocalSettingsStore CreateStore(TestOwnedTemporaryRoot root)
    {
        return new WindowsLocalSettingsStore(
            ParseChild(root, DocumentName),
            new WindowsLocalIoExecutionBoundary(),
            new DeterministicIdentifierSource());
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

    private static WindowsLocalPath ParseChild(TestOwnedTemporaryRoot root, string childName)
    {
        return Assert.IsInstanceOfType<WindowsLocalPath>(
            Assert.IsInstanceOfType<PathParseSuccess>(
                FileSystemPath.Parse(root.Resolve(childName))).Path);
    }

    private sealed class ManualIoScheduler : IWindowsLocalIoScheduler
    {
        private readonly Queue<Action> _pending = [];

        internal int PendingCount => _pending.Count;

        internal void ExecuteAll()
        {
            while (_pending.Count > 0)
            {
                _pending.Dequeue()();
            }
        }

        public Task<TResult> ScheduleAsync<TResult>(Func<TResult> operation)
        {
            TaskCompletionSource<TResult> completion = new();
            _pending.Enqueue(() => completion.SetResult(operation()));
            return completion.Task;
        }
    }
}
