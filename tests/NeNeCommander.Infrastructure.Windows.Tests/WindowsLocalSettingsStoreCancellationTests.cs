using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.Settings;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves cancellation at each settings write boundary stops before the next effect and reports only completed effects.</summary>
[TestClass]
public sealed class WindowsLocalSettingsStoreCancellationTests
{
    private const string DocumentName = "settings.json";
    private const string TemporarySuffix = ".00000000000000000000000000000001.tmp";

    /// <summary>Proves cancellation precedes directory creation and temporary-file mutation.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenCancelledBeforeMutationThrowsWithoutWritingAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string nestedDocument = "cancelled-parent\\settings.json";
        WindowsLocalSettingsStore store = new(
            ParseChild(root, nestedDocument),
            new WindowsLocalIoExecutionBoundary(),
            new DeterministicIdentifierSource());
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => store.WriteAsync(UserSettings.Default, cancellation.Token));

        Assert.IsFalse(Directory.Exists(root.Resolve("cancelled-parent")));
        Assert.IsFalse(File.Exists(root.Resolve(nestedDocument)));
        Assert.IsFalse(File.Exists(root.Resolve(nestedDocument + TemporarySuffix)));
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
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
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
        Assert.IsFalse(File.Exists(root.Resolve(NestedDocument + TemporarySuffix)));
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
        Assert.IsFalse(File.Exists(root.Resolve(NestedDocument + TemporarySuffix)));
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
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
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
        Assert.AreEqual(ForeignTemporary, File.ReadAllText(root.Resolve(DocumentName + TemporarySuffix)));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName)));
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
}
