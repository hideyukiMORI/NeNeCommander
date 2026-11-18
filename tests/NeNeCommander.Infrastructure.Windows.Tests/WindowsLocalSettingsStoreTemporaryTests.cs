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

/// <summary>Proves a temporary settings entry that is occupied, replaced, changed, or removed is never published or deleted as the store's own.</summary>
[TestClass]
public sealed class WindowsLocalSettingsStoreTemporaryTests
{
    private const string DocumentName = "settings.json";
    private const string TemporarySuffix = ".00000000000000000000000000000001.tmp";

    /// <summary>Proves every preexisting entry kind at the temporary path is a collision.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenTemporaryPathIsDirectoryReportsCollisionWithoutDeletingItAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory(DocumentName + TemporarySuffix);
        WindowsLocalSettingsStore store = CreateStore(root);

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.TemporaryArtifactCollision, outcome.Failure);
        Assert.AreSame(SettingsDirectoryEffect.NotAttempted, outcome.DirectoryEffect);
        Assert.AreSame(SettingsWriteEffect.None, outcome.TemporaryEffect);
        Assert.IsTrue(Directory.Exists(root.Resolve(DocumentName + TemporarySuffix)));
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
                    string temporaryPath = documentPath + TemporarySuffix;
                    File.Move(temporaryPath, temporaryPath + ".owned");
                    File.WriteAllText(temporaryPath, ForeignTemporary);
                    File.WriteAllText(documentPath, ConcurrentDocument);
                },
            });

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.DestinationChanged, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.TemporaryArtifactLeft, outcome.TemporaryEffect);
        Assert.AreEqual(ForeignTemporary, File.ReadAllText(root.Resolve(DocumentName + TemporarySuffix)));
        Assert.IsTrue(File.Exists(root.Resolve(DocumentName + TemporarySuffix + ".owned")));
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
                    string temporaryPath = documentPath + TemporarySuffix;
                    File.Move(temporaryPath, temporaryPath + ".owned");
                    File.WriteAllText(temporaryPath, ForeignTemporary);
                },
            });

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.TemporaryArtifactCollision, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.TemporaryArtifactLeft, outcome.TemporaryEffect);
        Assert.AreEqual(ForeignTemporary, File.ReadAllText(root.Resolve(DocumentName + TemporarySuffix)));
        Assert.IsTrue(File.Exists(root.Resolve(DocumentName + TemporarySuffix + ".owned")));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName)));
    }

    /// <summary>Proves an in-place temporary rewrite cannot preserve metadata and become settings.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenTemporaryBytesChangeWithRestoredMetadataRejectsBeforePublishAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        byte[]? foreignBytes = null;
        WindowsLocalSettingsStore store = CreateStore(
            root,
            new ScriptedSettingsWriteTestHook
            {
                OnTemporaryFlushed = temporaryPath =>
                {
                    FileInfo original = new(temporaryPath);
                    DateTime creationTimeUtc = original.CreationTimeUtc;
                    DateTime lastWriteTimeUtc = original.LastWriteTimeUtc;
                    foreignBytes = File.ReadAllBytes(temporaryPath);
                    foreignBytes[0] ^= byte.MaxValue;
                    File.WriteAllBytes(temporaryPath, foreignBytes);
                    File.SetCreationTimeUtc(temporaryPath, creationTimeUtc);
                    File.SetLastWriteTimeUtc(temporaryPath, lastWriteTimeUtc);
                },
            });

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.TemporaryArtifactCollision, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.TemporaryArtifactLeft, outcome.TemporaryEffect);
        CollectionAssert.AreEqual(foreignBytes, File.ReadAllBytes(root.Resolve(DocumentName + TemporarySuffix)));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName)));
    }

    /// <summary>Proves a parent captured after temporary close cannot replace the approved chain.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenParentChangesBeforeTemporaryCaptureRejectsNewChainAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("settings-parent");
        bool laterCheckReached = false;
        WindowsLocalSettingsStore store = CreateStoreAt(
            root,
            "settings-parent\\" + DocumentName,
            new ScriptedSettingsWriteTestHook
            {
                OnTemporaryClosed = _ =>
                {
                    Directory.Move(root.Resolve("settings-parent"), root.Resolve("parked-parent"));
                    _ = root.CreateDirectory("settings-parent");
                    File.Move(
                        root.Resolve("parked-parent\\" + DocumentName + TemporarySuffix),
                        root.Resolve("settings-parent\\" + DocumentName + TemporarySuffix));
                },
                OnTemporaryFlushed = temporaryPath =>
                {
                    laterCheckReached = true;
                    File.Move(
                        temporaryPath,
                        root.Resolve("parked-parent\\" + DocumentName + TemporarySuffix));
                    Directory.Delete(root.Resolve("settings-parent"));
                    Directory.Move(root.Resolve("parked-parent"), root.Resolve("settings-parent"));
                },
            });
        _ = Assert.IsInstanceOfType<SettingsAbsent>(await store.ReadAsync(CancellationToken.None));

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.UnsafeLocation, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.TemporaryArtifactLeft, outcome.TemporaryEffect);
        Assert.IsFalse(laterCheckReached);
        Assert.IsTrue(File.Exists(root.Resolve("settings-parent\\" + DocumentName + TemporarySuffix)));
        Assert.IsFalse(File.Exists(root.Resolve("settings-parent\\" + DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve("parked-parent\\" + DocumentName + TemporarySuffix)));
    }

    /// <summary>Proves post-close identity capture cannot adopt a same-byte foreign temporary file.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenTemporaryIsReplacedWithSameBytesAfterCloseRejectsForeignEntryAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalSettingsStore store = CreateStore(
            root,
            new ScriptedSettingsWriteTestHook
            {
                OnTemporaryClosed = temporaryPath =>
                {
                    byte[] bytes = File.ReadAllBytes(temporaryPath);
                    File.Move(temporaryPath, temporaryPath + ".owned");
                    File.WriteAllBytes(temporaryPath, bytes);
                },
            });

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.TemporaryArtifactCollision, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.TemporaryArtifactLeft, outcome.TemporaryEffect);
        Assert.IsTrue(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
        Assert.IsTrue(File.Exists(root.Resolve(DocumentName + TemporarySuffix + ".owned")));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName)));
    }

    /// <summary>Proves a vanished owned temporary is rejected and its relocated entry is untouched.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenTemporaryVanishesBeforePublishRejectsWithoutCleanupAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalSettingsStore store = CreateStore(
            root,
            new ScriptedSettingsWriteTestHook
            {
                OnTemporaryFlushed = temporaryPath => File.Move(temporaryPath, temporaryPath + ".moved"),
            });

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.TemporaryArtifactCollision, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.TemporaryArtifactLeft, outcome.TemporaryEffect);
        Assert.IsTrue(File.Exists(root.Resolve(DocumentName + TemporarySuffix + ".moved")));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
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
