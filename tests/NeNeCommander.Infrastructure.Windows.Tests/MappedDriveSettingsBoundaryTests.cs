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

/// <summary>Proves settings admission uses the current drive fact before identity and effects.</summary>
[TestClass]
public sealed class MappedDriveSettingsBoundaryTests
{
    private const string DocumentName = "settings.json";
    private const string TemporaryName = "settings.json.00000000000000000000000000000001.tmp";

    /// <summary>Network locations cannot seed a document baseline or enter a write.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-024")]
    public async Task ReadAsyncWhenDriveIsNetworkRejectsWithoutAdmittingDocumentIdentityAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string original = "{\"schemaVersion\":1,\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"}";
        _ = root.WriteFile(DocumentName, original);
        DeterministicIdentifierSource identifiers = new();
        WindowsLocalSettingsStore store = CreateStore(root, identifiers,
            new WindowsDriveBoundary(_ => DriveType.Network));

        SettingsRejected read = Assert.IsInstanceOfType<SettingsRejected>(
            await store.ReadAsync(CancellationToken.None));
        SettingsWriteRejected write = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsReadFailureKind.Unreadable, read.Kind);
        Assert.AreSame(SettingsWriteFailureKind.UnsafeLocation, write.Failure);
        Assert.AreSame(SettingsDirectoryEffect.NotAttempted, write.DirectoryEffect);
        Assert.AreSame(SettingsWriteEffect.None, write.TemporaryEffect);
        Assert.AreEqual(0, identifiers.IssuedCount);
        Assert.AreEqual(original, File.ReadAllText(root.Resolve(DocumentName)));
    }

    /// <summary>An unknown root cannot create a candidate or a settings directory.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-024")]
    public async Task WriteAsyncWhenDriveIsUnknownRejectsBeforeCandidateOrDirectoryEffectAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        DeterministicIdentifierSource identifiers = new();
        WindowsLocalSettingsStore store = CreateStore(root, identifiers,
            new WindowsDriveBoundary(_ => DriveType.Unknown));

        SettingsWriteRejected write = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.UnsafeLocation, write.Failure);
        Assert.AreSame(SettingsDirectoryEffect.NotAttempted, write.DirectoryEffect);
        Assert.AreSame(SettingsWriteEffect.None, write.TemporaryEffect);
        Assert.AreEqual(0, identifiers.IssuedCount);
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve(TemporaryName)));
    }

    /// <summary>A remap after reading must be observed before write preflight.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-024")]
    public async Task WriteAsyncWhenDriveRemapsAfterReadRejectsBeforeDocumentIdentityOrCandidateAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        DriveType reportedType = DriveType.Fixed;
        DeterministicIdentifierSource identifiers = new();
        WindowsLocalSettingsStore store = CreateStore(root, identifiers,
            new WindowsDriveBoundary(_ => reportedType));
        _ = Assert.IsInstanceOfType<SettingsAbsent>(await store.ReadAsync(CancellationToken.None));

        reportedType = DriveType.Network;
        SettingsWriteRejected write = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.UnsafeLocation, write.Failure);
        Assert.AreSame(SettingsDirectoryEffect.NotAttempted, write.DirectoryEffect);
        Assert.AreSame(SettingsWriteEffect.None, write.TemporaryEffect);
        Assert.AreEqual(0, identifiers.IssuedCount);
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName)));
    }

    /// <summary>A remap at the directory callback must prevent directory creation.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-024")]
    public async Task WriteAsyncWhenDriveRemapsBeforeDirectoryCreationRejectsWithNoDirectoryEffectAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        DriveType reportedType = DriveType.Fixed;
        DeterministicIdentifierSource identifiers = new();
        WindowsLocalPath path = Assert.IsInstanceOfType<WindowsLocalPath>(
            Assert.IsInstanceOfType<PathParseSuccess>(
                FileSystemPath.Parse(root.Resolve("nested\\" + DocumentName))).Path);
        WindowsLocalSettingsStoreDependencies dependencies = new(
            identifiers,
            new ScriptedSettingsWriteTestHook
            {
                OnBeforeDirectoryCreation = _ => reportedType = DriveType.Network,
            },
            new WindowsDriveBoundary(_ => reportedType));
        WindowsLocalSettingsStore store = new(path, new WindowsLocalIoExecutionBoundary(), dependencies);
        _ = Assert.IsInstanceOfType<SettingsAbsent>(await store.ReadAsync(CancellationToken.None));

        SettingsWriteRejected write = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.UnsafeLocation, write.Failure);
        Assert.AreSame(SettingsDirectoryEffect.NotAttempted, write.DirectoryEffect);
        Assert.AreSame(SettingsWriteEffect.None, write.TemporaryEffect);
        Assert.IsFalse(Directory.Exists(root.Resolve("nested")));
    }

    /// <summary>A remap at publication must leave the owned temporary file untouched.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-024")]
    public async Task WriteAsyncWhenDriveRemapsBeforePublishRejectsAndLeavesOwnedTemporaryAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        DriveType reportedType = DriveType.Fixed;
        WindowsLocalSettingsStore store = CreateStore(root, new DeterministicIdentifierSource(),
            new WindowsDriveBoundary(_ => reportedType),
            new ScriptedSettingsWriteTestHook
            {
                OnBeforePublish = _ => reportedType = DriveType.Network,
            });
        _ = Assert.IsInstanceOfType<SettingsAbsent>(await store.ReadAsync(CancellationToken.None));

        SettingsWriteRejected write = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.UnsafeLocation, write.Failure);
        Assert.AreSame(SettingsDirectoryEffect.NotAttempted, write.DirectoryEffect);
        Assert.AreSame(SettingsWriteEffect.TemporaryArtifactLeft, write.TemporaryEffect);
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName)));
        Assert.IsTrue(File.Exists(root.Resolve(TemporaryName)));
    }

    /// <summary>Failure cleanup cannot delete through a remapped root.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-024")]
    public async Task WriteAsyncWhenDriveRemapsBeforeFailureCleanupLeavesOwnedTemporaryAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        DriveType reportedType = DriveType.Fixed;
        WindowsLocalSettingsStore store = CreateStore(root, new DeterministicIdentifierSource(),
            new WindowsDriveBoundary(_ => reportedType),
            new ScriptedSettingsWriteTestHook
            {
                OnTemporaryFlushed = _ =>
                {
                    reportedType = DriveType.Network;
                    throw new IOException("Injected failure after the drive fact changed.");
                },
            });
        _ = Assert.IsInstanceOfType<SettingsAbsent>(await store.ReadAsync(CancellationToken.None));

        SettingsWriteRejected write = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.IoFailure, write.Failure);
        Assert.AreSame(SettingsWriteEffect.TemporaryArtifactLeft, write.TemporaryEffect);
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName)));
        Assert.IsTrue(File.Exists(root.Resolve(TemporaryName)));
    }

    private static WindowsLocalSettingsStore CreateStore(
        TestOwnedTemporaryRoot root,
        DeterministicIdentifierSource identifiers,
        WindowsDriveBoundary driveBoundary,
        ISettingsWriteTestHook? hook = null)
    {
        WindowsLocalPath path = Assert.IsInstanceOfType<WindowsLocalPath>(
            Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(root.Resolve(DocumentName))).Path);
        WindowsLocalSettingsStoreDependencies dependencies = new(
            identifiers, hook ?? NoOpSettingsWriteTestHook.Instance, driveBoundary);
        return new WindowsLocalSettingsStore(path, new WindowsLocalIoExecutionBoundary(), dependencies);
    }
}
