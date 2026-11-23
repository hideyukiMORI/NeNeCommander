using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.Settings;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>
/// Proves the order of one settings write attempt's admission, candidate and final checks: each
/// check rejects at its own step, before a later step can allocate, create or publish anything.
/// </summary>
[TestClass]
public sealed class SettingsWriteAttemptTests
{
    private const string DocumentName = "settings.json";
    private const string TemporaryName = "settings.json.00000000000000000000000000000001.tmp";
    private const string Original = "{\"schemaVersion\":1,\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"}";

    /// <summary>Each drive recheck of write admission rejects before a candidate token is requested.</summary>
    /// <param name="admissionQuery">The write's drive query, counted after the read, that reports a network drive.</param>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-024")]
    [DataRow(1)]
    [DataRow(2)]
    public async Task WriteAsyncWhenDriveRemapsDuringAdmissionRejectsBeforeCandidateAsync(int admissionQuery)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.WriteFile(DocumentName, Original);
        int queries = 0;
        int networkQuery = int.MaxValue;
        DeterministicIdentifierSource identifiers = new();
        WindowsLocalSettingsStore store = CreateStore(root, DocumentName, new WindowsLocalSettingsStoreDependencies(
            identifiers,
            new ScriptedSettingsWriteTestHook(),
            new WindowsDriveBoundary(_ => ++queries == networkQuery ? DriveType.Network : DriveType.Fixed)));
        _ = Assert.IsInstanceOfType<SettingsRead>(await store.ReadAsync(CancellationToken.None));
        networkQuery = queries + admissionQuery;

        SettingsWriteRejected write = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.UnsafeLocation, write.Failure);
        Assert.AreSame(SettingsDirectoryEffect.NotAttempted, write.DirectoryEffect);
        Assert.AreSame(SettingsWriteEffect.None, write.TemporaryEffect);
        Assert.AreEqual(0, identifiers.IssuedCount);
        Assert.AreEqual(Original, File.ReadAllText(root.Resolve(DocumentName)));
    }

    /// <summary>A replaced parent after the read is rejected by admission, before a candidate exists.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenParentIsReplacedAfterReadRejectsBeforeCandidateAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("settings-parent");
        bool temporaryStepReached = false;
        DeterministicIdentifierSource identifiers = new();
        WindowsLocalSettingsStore store = CreateStore(root, "settings-parent\\" + DocumentName, new WindowsLocalSettingsStoreDependencies(
            identifiers,
            new ScriptedSettingsWriteTestHook { OnBeforeTemporaryCreation = _ => temporaryStepReached = true },
            new WindowsDriveBoundary(_ => DriveType.Fixed)));
        _ = Assert.IsInstanceOfType<SettingsAbsent>(await store.ReadAsync(CancellationToken.None));
        Directory.Move(root.Resolve("settings-parent"), root.Resolve("parked-parent"));
        _ = root.CreateDirectory("settings-parent");

        SettingsWriteRejected write = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.UnsafeLocation, write.Failure);
        Assert.AreSame(SettingsDirectoryEffect.NotAttempted, write.DirectoryEffect);
        Assert.AreSame(SettingsWriteEffect.None, write.TemporaryEffect);
        Assert.AreEqual(0, identifiers.IssuedCount);
        Assert.IsFalse(temporaryStepReached);
        Assert.IsFalse(File.Exists(root.Resolve("settings-parent\\" + DocumentName)));
    }

    /// <summary>A remap at the candidate step is rejected before the temporary file is created.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-024")]
    public async Task WriteAsyncWhenDriveRemapsBeforeTemporaryCreationRejectsWithoutTemporaryAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        DriveType reportedType = DriveType.Fixed;
        DeterministicIdentifierSource identifiers = new();
        WindowsLocalSettingsStore store = CreateStore(root, DocumentName, new WindowsLocalSettingsStoreDependencies(
            identifiers,
            new ScriptedSettingsWriteTestHook { OnBeforeTemporaryCreation = _ => reportedType = DriveType.Network },
            new WindowsDriveBoundary(_ => reportedType)));
        _ = Assert.IsInstanceOfType<SettingsAbsent>(await store.ReadAsync(CancellationToken.None));

        SettingsWriteRejected write = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.UnsafeLocation, write.Failure);
        Assert.AreSame(SettingsDirectoryEffect.NotAttempted, write.DirectoryEffect);
        Assert.AreSame(SettingsWriteEffect.None, write.TemporaryEffect);
        Assert.AreEqual(1, identifiers.IssuedCount);
        Assert.IsFalse(File.Exists(root.Resolve(TemporaryName)));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName)));
    }

    /// <summary>An I/O failure while a foreign entry occupies the candidate reports a collision.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenCandidateIsOccupiedDuringIoFailureReportsCollisionAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string Foreign = "foreign candidate";
        WindowsLocalSettingsStore store = CreateStore(root, DocumentName, new WindowsLocalSettingsStoreDependencies(
            new DeterministicIdentifierSource(),
            new ScriptedSettingsWriteTestHook
            {
                OnBeforeTemporaryCreation = temporaryPath =>
                {
                    File.WriteAllText(temporaryPath, Foreign);
                    throw new IOException("Injected failure after a foreign candidate appeared.");
                },
            },
            new WindowsDriveBoundary(_ => DriveType.Fixed)));

        SettingsWriteRejected write = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.TemporaryArtifactCollision, write.Failure);
        Assert.AreSame(SettingsWriteEffect.None, write.TemporaryEffect);
        Assert.AreEqual(Foreign, File.ReadAllText(root.Resolve(TemporaryName)));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName)));
    }

    /// <summary>The location check after the temporary comparison is the last gate before publish.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-024")]
    public async Task WriteAsyncWhenDriveRemapsAtFinalLocationCheckKeepsPublishedDocumentAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.WriteFile(DocumentName, Original);
        int queries = 0;
        int networkQuery = int.MaxValue;
        int queriesAtBeforePublish = 0;
        int publishChecks = 0;
        WindowsLocalSettingsStore store = CreateStore(root, DocumentName, new WindowsLocalSettingsStoreDependencies(
            new DeterministicIdentifierSource(),
            new ScriptedSettingsWriteTestHook
            {
                OnBeforePublish = _ =>
                {
                    queriesAtBeforePublish = queries;
                    networkQuery = publishChecks > 0 ? queries + publishChecks : int.MaxValue;
                },
                OnPublished = _ => publishChecks = queries - queriesAtBeforePublish,
            },
            new WindowsDriveBoundary(_ => ++queries == networkQuery ? DriveType.Network : DriveType.Fixed)));
        _ = Assert.IsInstanceOfType<SettingsRead>(await store.ReadAsync(CancellationToken.None));
        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));
        string published = File.ReadAllText(root.Resolve(DocumentName));
        UserSettings changed = UserSettings.Create(
            DisplayPreferences.Create(ColorScheme.Monokai, HiddenItemVisibility.Shown, KeyHintVisibility.Shown, InterfaceLanguage.System),
            UserSettings.Default.Bookmarks, UserSettings.Default.PaneSortOrders, null);

        SettingsWriteRejected write = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(changed, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.UnsafeLocation, write.Failure);
        Assert.AreSame(SettingsWriteEffect.None, write.TemporaryEffect);
        Assert.AreEqual(published, File.ReadAllText(root.Resolve(DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve("settings.json.00000000000000000000000000000002.tmp")));
    }

    private static WindowsLocalSettingsStore CreateStore(
        TestOwnedTemporaryRoot root,
        string childName,
        WindowsLocalSettingsStoreDependencies dependencies)
    {
        WindowsLocalPath path = Assert.IsInstanceOfType<WindowsLocalPath>(
            Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(root.Resolve(childName))).Path);
        return new WindowsLocalSettingsStore(path, new WindowsLocalIoExecutionBoundary(), dependencies);
    }
}
