using System;
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

/// <summary>Proves a second store that publishes, replaces, or selects the same candidate first is never overwritten by a stale writer.</summary>
[TestClass]
public sealed class WindowsLocalSettingsStoreConcurrentStoreTests
{
    private const string DocumentName = "settings.json";
    private const string TemporarySuffix = ".00000000000000000000000000000001.tmp";
    private const string SecondTemporarySuffix = ".00000000000000000000000000000002.tmp";

    /// <summary>Proves a stale writer rejects a completed write and cleans only its own candidate.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenAnotherStorePublishesBeforeFinalCheckRejectsStaleWriterAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        ManualIoScheduler otherScheduler = new();
        bool distinctCandidatesCoexisted = false;
        WindowsLocalSettingsStore otherStore = new(
            ParseChild(root, DocumentName),
            new WindowsLocalIoExecutionBoundary(otherScheduler),
            new DeterministicIdentifierSource(first: 2),
            new ScriptedSettingsWriteTestHook
            {
                OnTemporaryClosed = temporaryPath =>
                    distinctCandidatesCoexisted =
                        File.Exists(root.Resolve(DocumentName + TemporarySuffix)) &&
                        temporaryPath.Equals(
                            root.Resolve(DocumentName + SecondTemporarySuffix),
                            StringComparison.Ordinal) &&
                        File.Exists(temporaryPath),
            });
        _ = await ReadThroughSchedulerAsync<SettingsAbsent>(otherStore, otherScheduler);
        Task<SettingsWriteOutcome>? otherWrite = null;
        UserSettings otherValue = UserSettings.Create(DisplayPreferences.Create(ColorScheme.Dracula, HiddenItemVisibility.Shown, KeyHintVisibility.Shown, InterfaceLanguage.System), BookmarkCatalog.Empty, PaneSortPreferences.Default);
        WindowsLocalSettingsStore staleStore = CreateStore(
            root,
            new ScriptedSettingsWriteTestHook
            {
                OnBeforePublish = _ =>
                {
                    otherWrite = otherStore.WriteAsync(otherValue, CancellationToken.None);
                    otherScheduler.ExecuteAll();
                },
            });
        _ = Assert.IsInstanceOfType<SettingsAbsent>(
            await staleStore.ReadAsync(CancellationToken.None));

        SettingsWriteRejected stale = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await staleStore.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.DestinationChanged, stale.Failure);
        Assert.AreSame(SettingsWriteEffect.None, stale.TemporaryEffect);
        Assert.IsTrue(distinctCandidatesCoexisted);
        await AssertOtherWritePublishedAsync(root, otherWrite);
    }

    /// <summary>Proves a stale replace rejects another store's completed replacement.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenAnotherStoreReplacesDocumentRejectsStaleReplaceAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string Original =
            "{\"schemaVersion\":1,\"showHiddenItems\":true,\"colorScheme\":\"ubuntu\"}";
        _ = root.WriteFile(DocumentName, Original);
        ManualIoScheduler otherScheduler = new();
        WindowsLocalSettingsStore otherStore = new(
            ParseChild(root, DocumentName),
            new WindowsLocalIoExecutionBoundary(otherScheduler),
            new DeterministicIdentifierSource(first: 2));
        _ = await ReadThroughSchedulerAsync<SettingsRead>(otherStore, otherScheduler);
        Task<SettingsWriteOutcome>? otherWrite = null;
        UserSettings otherValue = UserSettings.Create(DisplayPreferences.Create(ColorScheme.Dracula, HiddenItemVisibility.Hidden, KeyHintVisibility.Shown, InterfaceLanguage.System), BookmarkCatalog.Empty, PaneSortPreferences.Default);
        WindowsLocalSettingsStore staleStore = CreateStore(
            root,
            new ScriptedSettingsWriteTestHook
            {
                OnBeforePublish = _ =>
                {
                    otherWrite = otherStore.WriteAsync(otherValue, CancellationToken.None);
                    otherScheduler.ExecuteAll();
                },
            });
        _ = Assert.IsInstanceOfType<SettingsRead>(
            await staleStore.ReadAsync(CancellationToken.None));

        SettingsWriteRejected stale = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await staleStore.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.DestinationChanged, stale.Failure);
        Assert.AreSame(SettingsWriteEffect.None, stale.TemporaryEffect);
        await AssertOtherWritePublishedAsync(root, otherWrite);
    }

    /// <summary>Proves equal candidate tokens cannot mutate the first store's owned temporary.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenAnotherStoreSelectsSameCandidatePreservesOwnerAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        ManualIoScheduler otherScheduler = new();
        WindowsLocalSettingsStore otherStore = new(
            ParseChild(root, DocumentName),
            new WindowsLocalIoExecutionBoundary(otherScheduler),
            new DeterministicIdentifierSource());
        _ = await ReadThroughSchedulerAsync<SettingsAbsent>(otherStore, otherScheduler);
        Task<SettingsWriteOutcome>? competingWrite = null;
        byte[]? ownerBytesAfterCollision = null;
        UserSettings competingValue = UserSettings.Create(DisplayPreferences.Create(ColorScheme.Dracula, HiddenItemVisibility.Shown, KeyHintVisibility.Shown, InterfaceLanguage.System), BookmarkCatalog.Empty, PaneSortPreferences.Default);
        WindowsLocalSettingsStore ownerStore = CreateStore(
            root,
            new ScriptedSettingsWriteTestHook
            {
                OnTemporaryClosed = _ =>
                {
                    competingWrite = otherStore.WriteAsync(competingValue, CancellationToken.None);
                    otherScheduler.ExecuteAll();
                    ownerBytesAfterCollision = File.ReadAllBytes(root.Resolve(DocumentName + TemporarySuffix));
                },
            });
        _ = Assert.IsInstanceOfType<SettingsAbsent>(
            await ownerStore.ReadAsync(CancellationToken.None));

        SettingsWriteOutcome owner = await ownerStore.WriteAsync(UserSettings.Default, CancellationToken.None);

        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(owner);
        SettingsWriteRejected competitor = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await Assert.IsInstanceOfType<Task<SettingsWriteOutcome>>(competingWrite));
        Assert.AreSame(SettingsWriteFailureKind.TemporaryArtifactCollision, competitor.Failure);
        Assert.AreSame(SettingsWriteEffect.None, competitor.TemporaryEffect);
        CollectionAssert.AreEqual(
            Assert.IsInstanceOfType<byte[]>(ownerBytesAfterCollision),
            File.ReadAllBytes(root.Resolve(DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + SecondTemporarySuffix)));
    }

    private static async Task<TRead> ReadThroughSchedulerAsync<TRead>(
        WindowsLocalSettingsStore store, ManualIoScheduler scheduler)
        where TRead : SettingsReadOutcome
    {
        Task<SettingsReadOutcome> read = store.ReadAsync(CancellationToken.None);
        scheduler.ExecuteAll();
        return Assert.IsInstanceOfType<TRead>(await read);
    }

    private static async Task AssertOtherWritePublishedAsync(
        TestOwnedTemporaryRoot root, Task<SettingsWriteOutcome>? otherWrite)
    {
        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(
            await Assert.IsInstanceOfType<Task<SettingsWriteOutcome>>(otherWrite));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + SecondTemporarySuffix)));
        Assert.Contains("\"colorScheme\":\"dracula\"", File.ReadAllText(root.Resolve(DocumentName)));
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
