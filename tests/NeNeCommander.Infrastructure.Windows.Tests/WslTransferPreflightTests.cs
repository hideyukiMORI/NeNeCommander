using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Preserves WslFileOperationAdapterTests evidence for WslTransferPreflightTests.</summary>
[TestClass]
public sealed class WslTransferPreflightTests
{
    /// <summary>Proves preflight builds its immutable plan from the one revalidated source entry.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task PreflightTransferAsyncUsesOneRevalidatedSourceEntryForItsPlan()
    {
        ScriptedWslFileSystem fileSystem = new();
        WslFileSystemEntry entry = WslOperationTestContext.Entry(
            WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home\\source.txt"),
            "source",
            DirectoryEntryKind.File);
        FileEntrySnapshot source = WslOperationTestContext.Snapshot(entry);
        WslPath destination = WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\target");
        fileSystem.Set(entry);
        fileSystem.Set(WslOperationTestContext.Entry(destination, "destination", DirectoryEntryKind.Directory));

        TransferPreflightSucceeded succeeded = Assert.IsInstanceOfType<TransferPreflightSucceeded>(
            await WslOperationTestContext.Adapter(fileSystem).PreflightTransferAsync(
                [source],
                destination,
                CancellationToken.None));

        Assert.HasCount(1, succeeded.Plan);
        Assert.AreSame(source, succeeded.Plan[0].Source);
        Assert.AreEqual(
            WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\target\\source.txt"),
            succeeded.Plan[0].Target);
        Assert.AreEqual(1, fileSystem.FindCount(entry.Path));

        fileSystem.Set(WslOperationTestContext.Entry(entry.Path, "replacement", DirectoryEntryKind.File));
        TransferPreflightOutcome replaced = await WslOperationTestContext.Adapter(fileSystem).PreflightTransferAsync(
            [source],
            destination,
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.IdentityChanged, replaced.Failure);
        Assert.AreEqual(2, fileSystem.FindCount(entry.Path));
        Assert.IsEmpty(fileSystem.Copied);
    }

    /// <summary>Proves WSL transfer preflight rejects every unsafe destination before copying.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-003")]
    [TestProperty("ThreatId", "ADV-006")]
    [TestProperty("ThreatId", "ADV-018")]
    public async Task PreflightTransferAsyncWhenBoundaryIsUnsafeFailsWithoutCopy()
    {
        ScriptedWslFileSystem fileSystem = new();
        WslFileSystemEntry source = WslOperationTestContext.Entry(
            WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home\\source"),
            "source",
            DirectoryEntryKind.Directory);
        WslPath destination = WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\target");
        fileSystem.Set(source);
        fileSystem.Set(WslOperationTestContext.Entry(destination, "destination", DirectoryEntryKind.Directory));
        WslFileOperationAdapter adapter = WslOperationTestContext.Adapter(fileSystem);

        await AssertPreflightRoutesAsync(fileSystem, source, adapter, destination);
        await AssertPreflightSourceRefusalsAsync(fileSystem, source, adapter, destination);
        await AssertPreflightDestinationRefusalsAsync(fileSystem, source, adapter);

        Assert.HasCount(0, fileSystem.Copied);
    }

    private static async Task AssertPreflightRoutesAsync(ScriptedWslFileSystem fileSystem, WslFileSystemEntry source, WslFileOperationAdapter adapter, WslPath destination)
    {
        TransferPreflightOutcome foreign = await adapter.PreflightTransferAsync(
            [WslOperationTestContext.Snapshot(source)],
            WslOperationTestContext.Wsl("\\\\wsl.localhost\\Debian\\target"),
            CancellationToken.None);
        WslFileSystemEntry foreignSource = WslOperationTestContext.Entry(
            WslOperationTestContext.Wsl("\\\\wsl.localhost\\Debian\\home\\foreign"),
            "foreign",
            DirectoryEntryKind.Directory);
        fileSystem.Set(foreignSource);
        TransferPreflightOutcome mixedSources = await adapter.PreflightTransferAsync(
            [WslOperationTestContext.Snapshot(source), WslOperationTestContext.Snapshot(foreignSource)],
            destination,
            CancellationToken.None);
        WslPath recursiveDestination = WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home\\source\\child");
        fileSystem.Set(WslOperationTestContext.Entry(recursiveDestination, "child", DirectoryEntryKind.Directory));
        TransferPreflightOutcome recursive = await adapter.PreflightTransferAsync(
            [WslOperationTestContext.Snapshot(source)],
            recursiveDestination,
            CancellationToken.None);
        Assert.AreSame(FileOperationFailureKind.NotFound, foreign.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, mixedSources.Failure);
        Assert.AreSame(FileOperationFailureKind.Conflict, recursive.Failure);
    }

    private static async Task AssertPreflightSourceRefusalsAsync(ScriptedWslFileSystem fileSystem, WslFileSystemEntry source, WslFileOperationAdapter adapter, WslPath destination)
    {
        WslPath collision = WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\target\\source");
        fileSystem.Set(WslOperationTestContext.Entry(collision, "collision", DirectoryEntryKind.Directory));
        TransferPreflightOutcome existing = await adapter.PreflightTransferAsync(
            [WslOperationTestContext.Snapshot(source)], destination, CancellationToken.None);
        fileSystem.Set(WslOperationTestContext.Entry(
            source.Path,
            "source",
            DirectoryEntryKind.Directory,
            FileAttributes.ReparsePoint));
        TransferPreflightOutcome linkedSource = await adapter.PreflightTransferAsync(
            [WslOperationTestContext.Snapshot(WslOperationTestContext.Entry(
                source.Path,
                "source",
                DirectoryEntryKind.Directory,
                FileAttributes.ReparsePoint))],
            destination,
            CancellationToken.None);
        fileSystem.Set(source);
        Assert.AreSame(FileOperationFailureKind.Conflict, existing.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, linkedSource.Failure);
    }

    private static async Task AssertPreflightDestinationRefusalsAsync(ScriptedWslFileSystem fileSystem, WslFileSystemEntry source, WslFileOperationAdapter adapter)
    {
        TransferPreflightOutcome missingDestination = await adapter.PreflightTransferAsync(
            [WslOperationTestContext.Snapshot(source)],
            WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\missing"),
            CancellationToken.None);
        WslPath fileDestination = WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\file-destination");
        fileSystem.Set(WslOperationTestContext.Entry(fileDestination, "file-destination", DirectoryEntryKind.File));
        TransferPreflightOutcome fileTarget = await adapter.PreflightTransferAsync(
            [WslOperationTestContext.Snapshot(source)], fileDestination, CancellationToken.None);
        WslPath linkedDestination = WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\linked-destination");
        fileSystem.Set(WslOperationTestContext.Entry(
            linkedDestination,
            "linked-destination",
            DirectoryEntryKind.Directory,
            FileAttributes.ReparsePoint));
        TransferPreflightOutcome linkedTarget = await adapter.PreflightTransferAsync(
            [WslOperationTestContext.Snapshot(source)], linkedDestination, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.NotFound, missingDestination.Failure);
        Assert.AreSame(FileOperationFailureKind.NotFound, fileTarget.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, linkedTarget.Failure);
    }
}
