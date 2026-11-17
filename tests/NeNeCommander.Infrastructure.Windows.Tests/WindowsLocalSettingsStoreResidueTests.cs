using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Identifiers;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.Settings;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves flushed temporary residue left by an earlier store is preserved and never blocks a later save.</summary>
[TestClass]
public sealed class WindowsLocalSettingsStoreResidueTests
{
    private const string DocumentName = "settings.json";
    private const string TemporarySuffix = ".00000000000000000000000000000001.tmp";
    private const string SecondTemporarySuffix = ".00000000000000000000000000000002.tmp";

    /// <summary>Proves a new store can save after an earlier process leaves its flushed temporary file.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenPriorStoreLeavesFlushedTemporaryAllowsNewStoreToSaveAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalSettingsStore interruptedStore = CreateStore(
            root,
            new ScriptedSettingsWriteTestHook
            {
                OnTemporaryClosed = _ => throw new InvalidOperationException("Injected interruption after flush."),
            });
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => interruptedStore.WriteAsync(UserSettings.Default, CancellationToken.None));
        string leftoverPath = root.Resolve(DocumentName + TemporarySuffix);
        byte[] leftoverBytes = File.ReadAllBytes(leftoverPath);
        Assert.IsGreaterThan(0, leftoverBytes.Length);
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName)));
        WindowsLocalSettingsStore recoveredStore = CreateStore(
            root,
            new DeterministicIdentifierSource(first: 2));
        _ = Assert.IsInstanceOfType<SettingsAbsent>(
            await recoveredStore.ReadAsync(CancellationToken.None));
        UserSettings replacement = UserSettings.Create(
            ColorScheme.Dracula,
            HiddenItemVisibility.Shown,
            BookmarkCatalog.Empty,
            PaneSortPreferences.Default);

        SettingsWriteOutcome outcome = await recoveredStore.WriteAsync(replacement, CancellationToken.None);

        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(
            outcome,
            $"Unexpected settings write rejection: {(outcome as SettingsWriteRejected)?.Failure}");
        CollectionAssert.AreEqual(leftoverBytes, File.ReadAllBytes(leftoverPath));
        UserSettings saved = Assert.IsInstanceOfType<SettingsRead>(
            await CreateStore(root).ReadAsync(CancellationToken.None)).Settings;
        Assert.AreSame(ColorScheme.Dracula, saved.ColorScheme);
        Assert.AreSame(HiddenItemVisibility.Shown, saved.HiddenItemVisibility);
    }

    /// <summary>Proves a valid document survives an interrupted replace and a new store can save.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenPriorReplaceLeavesFlushedTemporaryPreservesOriginalAndRecoversAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string Original =
            "{\"schemaVersion\":1,\"showHiddenItems\":true,\"colorScheme\":\"ubuntu\"}";
        string documentPath = root.WriteFile(DocumentName, Original);
        byte[] leftoverBytes = await InterruptReplaceAfterFlushAsync(root);
        string leftoverPath = root.Resolve(DocumentName + TemporarySuffix);
        Assert.AreEqual(Original, File.ReadAllText(documentPath));
        WindowsLocalSettingsStore recoveredStore = CreateStore(
            root,
            new DeterministicIdentifierSource(first: 2));
        UserSettings oldSettings = Assert.IsInstanceOfType<SettingsRead>(
            await recoveredStore.ReadAsync(CancellationToken.None)).Settings;
        Assert.AreSame(ColorScheme.Ubuntu, oldSettings.ColorScheme);
        UserSettings replacement = UserSettings.Create(
            ColorScheme.Dracula,
            HiddenItemVisibility.Hidden,
            BookmarkCatalog.Empty,
            PaneSortPreferences.Default);

        SettingsWriteOutcome outcome = await recoveredStore.WriteAsync(replacement, CancellationToken.None);

        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(outcome);
        CollectionAssert.AreEqual(leftoverBytes, File.ReadAllBytes(leftoverPath));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + SecondTemporarySuffix)));
        Assert.Contains("\"colorScheme\":\"dracula\"", File.ReadAllText(documentPath));
        UserSettings saved = Assert.IsInstanceOfType<SettingsRead>(
            await CreateStore(root).ReadAsync(CancellationToken.None)).Settings;
        Assert.AreSame(ColorScheme.Dracula, saved.ColorScheme);
        Assert.AreSame(HiddenItemVisibility.Hidden, saved.HiddenItemVisibility);
    }

    /// <summary>Proves an old fixed-name temporary remains untouched while a new candidate saves.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenLegacyTemporaryRemainsPreservesItAndSavesAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string LegacyTemporary = "legacy temporary bytes";
        _ = root.WriteFile(DocumentName + ".tmp", LegacyTemporary);
        WindowsLocalSettingsStore store = CreateStore(root);
        _ = Assert.IsInstanceOfType<SettingsAbsent>(await store.ReadAsync(CancellationToken.None));

        SettingsWriteOutcome outcome = await store.WriteAsync(UserSettings.Default, CancellationToken.None);

        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(outcome);
        Assert.AreEqual(LegacyTemporary, File.ReadAllText(root.Resolve(DocumentName + ".tmp")));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
        _ = Assert.IsInstanceOfType<SettingsRead>(
            await CreateStore(root).ReadAsync(CancellationToken.None));
    }

    private static async Task<byte[]> InterruptReplaceAfterFlushAsync(TestOwnedTemporaryRoot root)
    {
        WindowsLocalSettingsStore interruptedStore = CreateStore(
            root,
            new ScriptedSettingsWriteTestHook
            {
                OnTemporaryClosed = _ => throw new InvalidOperationException("Injected interruption after flush."),
            });
        _ = Assert.IsInstanceOfType<SettingsRead>(
            await interruptedStore.ReadAsync(CancellationToken.None));
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => interruptedStore.WriteAsync(UserSettings.Default, CancellationToken.None));
        byte[] leftoverBytes = File.ReadAllBytes(root.Resolve(DocumentName + TemporarySuffix));
        Assert.IsGreaterThan(0, leftoverBytes.Length);
        return leftoverBytes;
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

    private static WindowsLocalSettingsStore CreateStore(
        TestOwnedTemporaryRoot root,
        IIdentifierSource identifierSource)
    {
        return new WindowsLocalSettingsStore(
            ParseChild(root, DocumentName),
            new WindowsLocalIoExecutionBoundary(),
            identifierSource);
    }

    private static WindowsLocalPath ParseChild(TestOwnedTemporaryRoot root, string childName)
    {
        return Assert.IsInstanceOfType<WindowsLocalPath>(
            Assert.IsInstanceOfType<PathParseSuccess>(
                FileSystemPath.Parse(root.Resolve(childName))).Path);
    }
}
