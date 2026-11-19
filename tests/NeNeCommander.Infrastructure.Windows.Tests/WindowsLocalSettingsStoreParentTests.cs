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

/// <summary>Proves a settings parent directory that changes around a write keeps the location blocked and preserves every foreign entry.</summary>
[TestClass]
public sealed class WindowsLocalSettingsStoreParentTests
{
    private const string DocumentName = "settings.json";
    private const string TemporarySuffix = ".00000000000000000000000000000001.tmp";

    /// <summary>Proves post-publish observation cannot adopt a replacement parent as its anchor.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenParentChangesAfterPublishKeepsLocationBlockedAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("settings-parent");
        bool replaceParent = true;
        WindowsLocalSettingsStore store = CreateStoreAt(
            root,
            "settings-parent\\" + DocumentName,
            new ScriptedSettingsWriteTestHook
            {
                OnPublished = _ =>
                {
                    if (!replaceParent)
                    {
                        return;
                    }
                    replaceParent = false;
                    Directory.Move(root.Resolve("settings-parent"), root.Resolve("parked-parent"));
                    _ = root.CreateDirectory("settings-parent");
                    File.Move(
                        root.Resolve("parked-parent\\" + DocumentName),
                        root.Resolve("settings-parent\\" + DocumentName));
                },
            });
        _ = Assert.IsInstanceOfType<SettingsAbsent>(await store.ReadAsync(CancellationToken.None));

        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));
        File.Move(
            root.Resolve("settings-parent\\" + DocumentName),
            root.Resolve("parked-parent\\" + DocumentName));
        Directory.Delete(root.Resolve("settings-parent"));
        Directory.Move(root.Resolve("parked-parent"), root.Resolve("settings-parent"));
        SettingsWriteRejected second = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(
                UserSettings.Create(DisplayPreferences.Create(ColorScheme.Dracula, HiddenItemVisibility.Shown, KeyHintVisibility.Shown), BookmarkCatalog.Empty, PaneSortPreferences.Default),
                CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.UnsafeLocation, second.Failure);
        Assert.Contains("\"colorScheme\":\"nene-dark\"", File.ReadAllText(
            root.Resolve("settings-parent\\" + DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve("settings-parent\\" + DocumentName + TemporarySuffix)));
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
        Assert.IsFalse(File.Exists(root.Resolve("settings-parent\\" + DocumentName + TemporarySuffix)));
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
        Assert.IsTrue(File.Exists(root.Resolve("parked-parent\\" + DocumentName + TemporarySuffix)));
        Assert.IsFalse(File.Exists(root.Resolve("target\\" + DocumentName + TemporarySuffix)));
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
                    _ = root.WriteFile("settings-parent\\" + DocumentName + TemporarySuffix, ForeignTemporary);
                },
            });
        _ = Assert.IsInstanceOfType<SettingsAbsent>(await store.ReadAsync(CancellationToken.None));

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.UnsafeLocation, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.TemporaryArtifactLeft, outcome.TemporaryEffect);
        Assert.AreEqual(
            ForeignTemporary,
            File.ReadAllText(root.Resolve("settings-parent\\" + DocumentName + TemporarySuffix)));
        Assert.IsTrue(File.Exists(root.Resolve("parked-parent\\" + DocumentName + TemporarySuffix)));
        Assert.IsFalse(File.Exists(root.Resolve("settings-parent\\" + DocumentName)));
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
