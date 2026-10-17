using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves current drive facts gate direct local adapter identity and effects.</summary>
[TestClass]
public sealed class MappedDriveLocalMutationBoundaryTests
{
    /// <summary>Remapping after snapshots prevents each local identity and effect step.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-024")]
    public async Task DirectAdapterWhenDriveRemapsAfterSnapshotRejectsEveryLocalStepWithoutEffect()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string sourceText = root.WriteFile("a.txt", "kept");
        WindowsLocalPath sourcePath = Local(sourceText);
        WindowsLocalPath destination = Local(root.CreateDirectory("dest"));
        WindowsLocalPath renamed = Local(root.Resolve("b.txt"));
        WindowsLocalPath created = Local(root.Resolve("created"));
        DriveType observed = DriveType.Fixed;
        WindowsLocalFileOperationAdapter adapter = Adapter(new WindowsDriveBoundary(_ => observed));
        FileEntrySnapshot source = Assert.IsInstanceOfType<FileInspectionSucceeded>(
            await adapter.InspectAsync(sourcePath, CancellationToken.None)).Snapshot;
        FileEntrySnapshot location = Assert.IsInstanceOfType<FileInspectionSucceeded>(
            await adapter.InspectAsync(root.Path, CancellationToken.None)).Snapshot;
        Assert.IsNull((await adapter.PreflightTransferAsync([source], destination, CancellationToken.None)).Failure);

        observed = DriveType.Network;
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable,
            Assert.IsInstanceOfType<FileInspectionFailed>(
                await adapter.InspectAsync(sourcePath, CancellationToken.None)).Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable,
            (await adapter.PreflightTransferAsync([source], destination, CancellationToken.None)).Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable,
            Assert.IsInstanceOfType<AtomicMoveCapabilityFailed>(
                await adapter.GetAtomicMoveCapabilityAsync(source, destination, CancellationToken.None)).Failure);
        AssertDenied(await adapter.CopyAsync(source, destination, CancellationToken.None));
        AssertDenied(await adapter.MoveAsync(source, destination, CancellationToken.None));
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable,
            (await adapter.VerifyCopyAsync(source, destination, CancellationToken.None)).Failure);
        AssertDenied(await adapter.DeleteAsync(source, DeletionExecutionMode.Permanent, CancellationToken.None));
        AssertDenied(await adapter.DeleteAsync(source, DeletionExecutionMode.Recycle, CancellationToken.None));
        AssertDenied(await adapter.CreateDirectoryAsync(location, created, CancellationToken.None));
        AssertDenied(await adapter.RenameAsync(source, renamed, CancellationToken.None));
        Assert.AreEqual("kept", File.ReadAllText(sourceText));
        Assert.IsFalse(File.Exists(root.Resolve("dest\\a.txt")));
        Assert.IsFalse(File.Exists(renamed.CanonicalText));
        Assert.IsFalse(Directory.Exists(created.CanonicalText));
    }

    /// <summary>Unsupported drive facts deny fresh identity and deletion.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-024")]
    [DataRow(DriveType.Unknown)]
    [DataRow(DriveType.NoRootDirectory)]
    [DataRow(DriveType.Network)]
    [DataRow((DriveType)1234)]
    public async Task DirectAdapterWhenDriveTypeIsNotKnownLocalRejectsIdentityAndDeletion(DriveType observed)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string sourceText = root.WriteFile("a.txt", "kept");
        WindowsLocalPath path = Local(sourceText);
        WindowsLocalFileOperationAdapter local = Adapter(new WindowsDriveBoundary(_ => DriveType.Fixed));
        FileEntrySnapshot snapshot = Assert.IsInstanceOfType<FileInspectionSucceeded>(
            await local.InspectAsync(path, CancellationToken.None)).Snapshot;
        WindowsLocalFileOperationAdapter denied = Adapter(new WindowsDriveBoundary(_ => observed));

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable,
            Assert.IsInstanceOfType<FileInspectionFailed>(
                await denied.InspectAsync(path, CancellationToken.None)).Failure);
        AssertDenied(await denied.DeleteAsync(snapshot, DeletionExecutionMode.Permanent, CancellationToken.None));
        Assert.AreEqual("kept", File.ReadAllText(sourceText));
    }

    /// <summary>Drive query failures deny local identity.</summary>
    [TestMethod]
    public async Task DirectAdapterWhenDriveQueryFailsRejectsWithoutIdentity()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = Adapter(new WindowsDriveBoundary(_ => throw new IOException("Drive query unavailable.")));

        FileInspectionOutcome outcome = await adapter.InspectAsync(Local(root.WriteFile("a.txt", "kept")), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable,
            Assert.IsInstanceOfType<FileInspectionFailed>(outcome).Failure);
    }

    /// <summary>A remapped local source prevents transfer before any target is created.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-024")]
    public async Task TransferWhenLocalSourceReportsNetworkRejectsWithoutCopy()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string sourceText = root.WriteFile("a.txt", "kept");
        WindowsLocalPath sourcePath = Local(sourceText);
        WindowsLocalPath destination = Local(root.CreateDirectory("dest"));
        FileEntrySnapshot source = Assert.IsInstanceOfType<FileInspectionSucceeded>(
            await Adapter(new WindowsDriveBoundary(_ => DriveType.Fixed))
                .InspectAsync(sourcePath, CancellationToken.None)).Snapshot;
        WindowsDriveBoundary drives = new(path =>
            path.CanonicalText.StartsWith(destination.CanonicalText, StringComparison.OrdinalIgnoreCase)
                ? DriveType.Fixed : DriveType.Network);
        WindowsLocalFileOperationAdapter adapter = Adapter(drives);

        TransferPreflightOutcome preflight = await adapter.PreflightTransferAsync([source], destination, CancellationToken.None);
        ProviderStepOutcome copy = await adapter.CopyAsync(source, destination, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, preflight.Failure);
        AssertDenied(copy);
        Assert.AreEqual("kept", File.ReadAllText(sourceText));
        Assert.IsFalse(File.Exists(root.Resolve("dest\\a.txt")));
    }

    /// <summary>A remapped local destination prevents transfer before any target is created.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-024")]
    public async Task TransferWhenLocalDestinationReportsNetworkRejectsWithoutCopy()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string sourceText = root.WriteFile("a.txt", "kept");
        WindowsLocalPath sourcePath = Local(sourceText);
        WindowsLocalPath destination = Local(root.CreateDirectory("dest"));
        FileEntrySnapshot source = Assert.IsInstanceOfType<FileInspectionSucceeded>(
            await Adapter(new WindowsDriveBoundary(_ => DriveType.Fixed))
                .InspectAsync(sourcePath, CancellationToken.None)).Snapshot;
        WindowsDriveBoundary drives = new(path =>
            path.CanonicalText.StartsWith(destination.CanonicalText, StringComparison.OrdinalIgnoreCase)
                ? DriveType.Network : DriveType.Fixed);
        WindowsLocalFileOperationAdapter adapter = Adapter(drives);

        TransferPreflightOutcome preflight = await adapter.PreflightTransferAsync([source], destination, CancellationToken.None);
        ProviderStepOutcome copy = await adapter.CopyAsync(source, destination, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, preflight.Failure);
        AssertDenied(copy);
        Assert.AreEqual("kept", File.ReadAllText(sourceText));
        Assert.IsFalse(File.Exists(root.Resolve("dest\\a.txt")));
    }

    /// <summary>Recognized local media preserve direct inspection.</summary>
    [TestMethod]
    [DataRow(DriveType.Fixed)]
    [DataRow(DriveType.Removable)]
    [DataRow(DriveType.CDRom)]
    [DataRow(DriveType.Ram)]
    public async Task DirectAdapterWhenDriveIsKnownLocalPreservesInspection(DriveType observed)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath path = Local(root.WriteFile("a.txt", "kept"));
        WindowsLocalFileOperationAdapter adapter = Adapter(new WindowsDriveBoundary(_ => observed));

        FileInspectionOutcome outcome = await adapter.InspectAsync(path, CancellationToken.None);

        Assert.AreSame(path, Assert.IsInstanceOfType<FileInspectionSucceeded>(outcome).Snapshot.Path);
    }

    /// <summary>A Windows source remap denies preflight and rejects copy after an admitted preflight.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-024")]
    public async Task WindowsToWslWhenLocalSourceRemapsAfterPreflightRejectsCopy()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath sourcePath = Local(root.WriteFile("source.txt", "kept"));
        DriveType observed = DriveType.Fixed;
        WindowsDriveBoundary drives = new(_ => observed);
        FileEntrySnapshot source = Assert.IsInstanceOfType<FileInspectionSucceeded>(
            await Adapter(drives)
                .InspectAsync(sourcePath, CancellationToken.None)).Snapshot;
        WslPath destination = WslToWindowsTransferFixture.Source("destination");
        ScriptedWslFileSystem target = new();
        target.Set(ScriptedWslFileSystem.Entry(destination, "destination", DirectoryEntryKind.Directory));
        WindowsToWslTransfer transfer = new(new WindowsLocalIoExecutionBoundary(), target, drives);

        observed = DriveType.Network;
        TransferPreflightOutcome rejected = await transfer.PreflightTransferAsync([source], destination, CancellationToken.None);
        observed = DriveType.Fixed;
        TransferPreflightOutcome admitted = await transfer.PreflightTransferAsync([source], destination, CancellationToken.None);
        observed = DriveType.Network;
        ProviderStepOutcome copy = await transfer.CopyAsync(source, destination, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, rejected.Failure);
        Assert.IsNull(admitted.Failure);
        AssertDenied(copy);
        Assert.AreEqual("kept", File.ReadAllText(sourcePath.CanonicalText));
        Assert.IsFalse(target.TargetExists(WslToWindowsTransferFixture.Source("destination\\source.txt")));
    }

    /// <summary>A Windows target remap denies preflight and rejects copy after an admitted preflight.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-024")]
    public async Task WslToWindowsWhenLocalDestinationRemapsAfterPreflightRejectsCopy()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("wsl");
        _ = root.WriteFile("wsl\\source.txt", "kept");
        WindowsLocalPath destination = Local(root.CreateDirectory("windows"));
        DriveType observed = DriveType.Fixed;
        WindowsDriveBoundary drives = new(_ => observed);
        WindowsWslFileSystem sourceFileSystem = new(
            path => OwnedWslNamespace.Resolve(root.ResolveVerbatim("wsl"), path),
            OwnedWslNamespace.ReadFacts, drives);
        WslPath sourcePath = WslToWindowsTransferFixture.Source("source.txt");
        FileEntrySnapshot source = Assert.IsInstanceOfType<FileInspectionSucceeded>(
            await new WslFileOperationAdapter(new WindowsLocalIoExecutionBoundary(), sourceFileSystem)
                .InspectAsync(sourcePath, CancellationToken.None)).Snapshot;
        WslToWindowsTransfer transfer = new(new WindowsLocalIoExecutionBoundary(), sourceFileSystem, drives);

        observed = DriveType.Network;
        TransferPreflightOutcome rejected = await transfer.PreflightTransferAsync([source], destination, CancellationToken.None);
        observed = DriveType.Fixed;
        TransferPreflightOutcome admitted = await transfer.PreflightTransferAsync([source], destination, CancellationToken.None);
        observed = DriveType.Network;
        ProviderStepOutcome copy = await transfer.CopyAsync(source, destination, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, rejected.Failure);
        Assert.IsNull(admitted.Failure);
        AssertDenied(copy);
        Assert.AreEqual("kept", File.ReadAllText(root.Resolve("wsl\\source.txt")));
        Assert.IsFalse(File.Exists(root.Resolve("windows\\source.txt")));
    }

    private static WindowsLocalFileOperationAdapter Adapter(WindowsDriveBoundary drives)
    {
        return new WindowsLocalFileOperationAdapter(new WindowsLocalIoExecutionBoundary(),
            new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }, drives),
            new WindowsWslFileSystem(drives), drives);
    }

    private static WindowsLocalPath Local(string text)
    {
        return Assert.IsInstanceOfType<WindowsLocalPath>(
            Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path);
    }

    private static void AssertDenied(ProviderStepOutcome outcome)
    {
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
        Assert.IsNull(outcome.Effect);
    }
}
