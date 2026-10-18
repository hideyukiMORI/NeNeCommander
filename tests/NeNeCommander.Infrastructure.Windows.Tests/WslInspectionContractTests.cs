using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Preserves WslFileOperationAdapterTests evidence for WslInspectionContractTests.</summary>
[TestClass]
public sealed class WslInspectionContractTests
{
    /// <summary>Proves inspection publishes permanent-only capability and the provider identity.</summary>
    [TestMethod]
    public async Task InspectAsyncWhenEntryExistsReturnsPermanentOnlySnapshot()
    {
        ScriptedWslFileSystem fileSystem = new();
        WslPath path = WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home\\item.txt");
        WslFileSystemEntry entry = WslOperationTestContext.Entry(path, "identity-a", DirectoryEntryKind.File);
        fileSystem.Set(entry);

        FileInspectionOutcome outcome = await WslOperationTestContext.Adapter(fileSystem).InspectAsync(path, CancellationToken.None);

        FileEntrySnapshot snapshot = Assert.IsInstanceOfType<FileInspectionSucceeded>(outcome).Snapshot;
        Assert.AreEqual(path, snapshot.Path);
        Assert.AreEqual(entry.Identity, snapshot.Identity);
        Assert.AreSame(DeletionCapability.PermanentOnly, snapshot.DeletionCapability);
    }

    /// <summary>Proves missing entries and expected platform failures are normalized.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-017")]
    public async Task ProviderFailureWhenExpectedReturnsCanonicalFailure()
    {
        ScriptedWslFileSystem missing = new();
        WslPath path = WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home\\missing");
        FileInspectionFailed absent = Assert.IsInstanceOfType<FileInspectionFailed>(
            await WslOperationTestContext.Adapter(missing).InspectAsync(path, CancellationToken.None));
        ScriptedWslFileSystem denied = new() { Failure = new UnauthorizedAccessException() };
        FileInspectionFailed inaccessible = Assert.IsInstanceOfType<FileInspectionFailed>(
            await WslOperationTestContext.Adapter(denied).InspectAsync(path, CancellationToken.None));
        ScriptedWslFileSystem failedMutation = new();
        WslFileSystemEntry sourceEntry = WslOperationTestContext.Entry(path, "source", DirectoryEntryKind.File);
        failedMutation.Set(sourceEntry);
        failedMutation.Failure = new IOException("Synthetic provider loss.");
        ProviderStepOutcome failed = await WslOperationTestContext.Adapter(failedMutation).DeleteAsync(
            WslOperationTestContext.Snapshot(sourceEntry),
            DeletionExecutionMode.Permanent,
            CancellationToken.None);
        ScriptedWslFileSystem deniedMutation = new();
        deniedMutation.Set(sourceEntry);
        deniedMutation.Failure = new UnauthorizedAccessException();
        ProviderStepOutcome deniedDelete = await WslOperationTestContext.Adapter(deniedMutation).DeleteAsync(
            WslOperationTestContext.Snapshot(sourceEntry),
            DeletionExecutionMode.Permanent,
            CancellationToken.None);
        FileInspectionFailed unsupported = Assert.IsInstanceOfType<FileInspectionFailed>(
            await WslOperationTestContext.Adapter(new ScriptedWslFileSystem()).InspectAsync(
                WslOperationTestContext.Local("C:\\item"),
                CancellationToken.None));
        ScriptedWslFileSystem unavailable = new() { Failure = new IOException("Synthetic loss.") };
        FileInspectionFailed unavailableInspection = Assert.IsInstanceOfType<FileInspectionFailed>(
            await WslOperationTestContext.Adapter(unavailable).InspectAsync(path, CancellationToken.None));

        Assert.AreSame(FileOperationFailureKind.NotFound, absent.Failure);
        Assert.AreSame(FileOperationFailureKind.AccessDenied, inaccessible.Failure);
        Assert.AreSame(FileOperationFailureKind.Delete, failed.Failure);
        Assert.AreSame(FileOperationFailureKind.AccessDenied, deniedDelete.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, unsupported.Failure);
        Assert.AreSame(FileOperationFailureKind.Inspection, unavailableInspection.Failure);
        Assert.HasCount(0, failedMutation.Deleted);
    }

    /// <summary>Proves required adapter and entry arguments reject defects synchronously.</summary>
    [TestMethod]
    public void BoundariesWhenArgumentIsNullRejectDefect()
    {
        ScriptedWslFileSystem fileSystem = new();
        WindowsLocalIoExecutionBoundary boundary = new();
        WslFileOperationAdapter adapter = WslOperationTestContext.Adapter(fileSystem);
        WslPath path = WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home\\item");
        WslFileSystemEntry entry = WslOperationTestContext.Entry(path, "identity", DirectoryEntryKind.File);
        FileEntrySnapshot snapshot = WslOperationTestContext.Snapshot(entry);

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WslFileOperationAdapter(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WslFileOperationAdapter(boundary, null!));
        AssertEntryNullGuards(path, entry);
        AssertMutationNullGuards(adapter, snapshot, path);
        AssertTransferNullGuards(adapter, snapshot, path);
    }

    private static void AssertEntryNullGuards(WslPath path, WslFileSystemEntry entry)
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => new WslFileSystemEntry(null!, entry.Name, new WslEntryMetadata(entry.Identity, entry.Kind, entry.Attributes)));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => new WslFileSystemEntry(path, null!, new WslEntryMetadata(entry.Identity, entry.Kind, entry.Attributes)));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => new WslFileSystemEntry(path, entry.Name, new WslEntryMetadata(null!, entry.Kind, entry.Attributes)));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => new WslFileSystemEntry(path, entry.Name, new WslEntryMetadata(entry.Identity, null!, entry.Attributes)));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WslFileSystemEntry(path, entry.Name, null!));
    }

    private static void AssertMutationNullGuards(WslFileOperationAdapter adapter, FileEntrySnapshot snapshot, WslPath path)
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => adapter.InspectAsync(null!, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => adapter.DeleteAsync(null!, DeletionExecutionMode.Permanent, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => adapter.DeleteAsync(snapshot, null!, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => adapter.CreateDirectoryAsync(null!, path, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => adapter.CreateDirectoryAsync(snapshot, null!, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => adapter.RenameAsync(null!, path, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => adapter.RenameAsync(snapshot, null!, CancellationToken.None));
    }

    private static void AssertTransferNullGuards(WslFileOperationAdapter adapter, FileEntrySnapshot snapshot, WslPath path)
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => adapter.PreflightTransferAsync(null!, path, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => adapter.PreflightTransferAsync([snapshot], null!, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => adapter.GetAtomicMoveCapabilityAsync(null!, path, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => adapter.GetAtomicMoveCapabilityAsync(snapshot, null!, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => adapter.MoveAsync(null!, path, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => adapter.MoveAsync(snapshot, null!, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => adapter.CopyAsync(null!, path, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => adapter.CopyAsync(snapshot, null!, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => adapter.VerifyCopyAsync(null!, path, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => adapter.VerifyCopyAsync(snapshot, null!, CancellationToken.None));
    }
}
