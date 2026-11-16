using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.Settings;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves symbolic links and junctions on the settings document chain are refused and never followed.</summary>
[TestClass]
public sealed class WindowsLocalSettingsStoreLinkTests
{
    private const string DocumentName = "settings.json";
    private const string TemporarySuffix = ".00000000000000000000000000000001.tmp";

    /// <summary>Proves a settings-document file symlink is rejected without changing its target.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task StoreWhenDocumentIsFileSymbolicLinkRejectsWithoutChangingTargetAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string TargetName = "target-settings.json";
        const string TargetDocument =
            "{\"schemaVersion\":1,\"showHiddenItems\":true,\"colorScheme\":\"ubuntu\"}";
        _ = root.WriteFile(TargetName, TargetDocument);
        _ = root.CreateFileSymbolicLink(DocumentName, TargetName);
        WindowsLocalSettingsStore store = CreateStore(root);

        SettingsReadOutcome read = await store.ReadAsync(CancellationToken.None);
        SettingsWriteRejected write = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(
            SettingsReadFailureKind.Unreadable,
            Assert.IsInstanceOfType<SettingsRejected>(read).Kind);
        Assert.AreSame(SettingsWriteFailureKind.ExistingDocumentRejected, write.Failure);
        Assert.AreEqual(TargetDocument, File.ReadAllText(root.Resolve(TargetName)));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
    }

    /// <summary>Proves a dangling ancestor junction is unsafe rather than an absent directory.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task StoreWhenAncestorIsDanglingJunctionRejectsWithoutCreatingItsTargetAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateJunction("settings-link", "missing-target");
        WindowsLocalSettingsStore store = CreateStoreAt(root, "settings-link\\" + DocumentName);

        SettingsReadOutcome read = await store.ReadAsync(CancellationToken.None);
        SettingsWriteRejected write = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        _ = Assert.IsInstanceOfType<SettingsRejected>(read);
        Assert.AreSame(SettingsWriteFailureKind.UnsafeLocation, write.Failure);
        Assert.IsFalse(Directory.Exists(root.Resolve("missing-target")));
        Assert.IsFalse(File.Exists(root.Resolve("missing-target\\" + DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve("missing-target\\" + DocumentName + TemporarySuffix)));
    }

    /// <summary>Proves a dangling document symlink is rejected during write preflight.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenDocumentIsDanglingSymbolicLinkRejectsBeforeTemporaryAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateFileSymbolicLink(DocumentName, "missing-target.json");
        WindowsLocalSettingsStore store = CreateStore(root);

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.ExistingDocumentRejected, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.None, outcome.TemporaryEffect);
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
        Assert.IsFalse(File.Exists(root.Resolve("missing-target.json")));
    }

    /// <summary>Proves a dangling temporary symlink is an existing collision and remains untouched.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenTemporaryIsDanglingSymbolicLinkReportsCollisionAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateFileSymbolicLink(DocumentName + TemporarySuffix, "missing-temporary-target.json");
        WindowsLocalSettingsStore store = CreateStore(root);

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.TemporaryArtifactCollision, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.None, outcome.TemporaryEffect);
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve("missing-temporary-target.json")));
    }

    /// <summary>Proves a document replaced by a file symlink is rejected at publish revalidation.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenDocumentBecomesFileSymbolicLinkRejectsBeforePublishAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string OriginalDocument =
            "{\"schemaVersion\":1,\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"}";
        const string TargetName = "target-settings.json";
        const string TargetDocument =
            "{\"schemaVersion\":1,\"showHiddenItems\":true,\"colorScheme\":\"ubuntu\"}";
        _ = root.WriteFile(DocumentName, OriginalDocument);
        _ = root.WriteFile(TargetName, TargetDocument);
        WindowsLocalSettingsStore store = CreateStore(
            root,
            new ScriptedSettingsWriteTestHook
            {
                OnTemporaryFlushed = documentTemporaryPath =>
                {
                    _ = documentTemporaryPath;
                    File.Move(root.Resolve(DocumentName), root.Resolve(DocumentName + ".original"));
                    _ = root.CreateFileSymbolicLink(DocumentName, TargetName);
                },
            });
        _ = Assert.IsInstanceOfType<SettingsRead>(await store.ReadAsync(CancellationToken.None));

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.DestinationChanged, outcome.Failure);
        Assert.AreEqual(TargetDocument, File.ReadAllText(root.Resolve(TargetName)));
        Assert.AreEqual(OriginalDocument, File.ReadAllText(root.Resolve(DocumentName + ".original")));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
    }

    /// <summary>Proves a direct write cannot bypass an unsafe ancestor through a read baseline.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenAncestorIsJunctionRejectsWithoutPriorReadAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("target");
        _ = root.CreateJunction("settings-link", "target");
        WindowsLocalSettingsStore store = CreateStoreAt(root, "settings-link\\" + DocumentName);

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.UnsafeLocation, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.None, outcome.TemporaryEffect);
        Assert.IsFalse(File.Exists(root.Resolve("target\\" + DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve("target\\" + DocumentName + TemporarySuffix)));
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
        Assert.IsFalse(File.Exists(root.Resolve("target\\" + DocumentName + TemporarySuffix)));
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
        Assert.IsFalse(File.Exists(root.Resolve("target\\" + DocumentName + TemporarySuffix)));
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
        Assert.IsFalse(File.Exists(root.Resolve("settings-link\\" + DocumentName + TemporarySuffix)));
        Assert.IsFalse(File.Exists(root.Resolve("target\\" + DocumentName)));
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

    private static WindowsLocalPath ParseChild(TestOwnedTemporaryRoot root, string childName)
    {
        return Assert.IsInstanceOfType<WindowsLocalPath>(
            Assert.IsInstanceOfType<PathParseSuccess>(
                FileSystemPath.Parse(root.Resolve(childName))).Path);
    }
}
