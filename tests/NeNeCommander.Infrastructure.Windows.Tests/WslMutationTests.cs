using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Preserves WslFileOperationAdapterTests evidence for WslMutationTests.</summary>
[TestClass]
public sealed class WslMutationTests
{
    /// <summary>Proves create, rename, and confirmed delete retain the canonical gateway effects.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-008")]
    public async Task ExecuteAsyncWhenSameDistributionMutationSucceedsReportsExactEffects()
    {
        ScriptedWslFileSystem fileSystem = new();
        WslPath location = WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home\\xi");
        WslPath source = WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home\\xi\\old.txt");
        fileSystem.Set(WslOperationTestContext.Entry(location, "location", DirectoryEntryKind.Directory));
        fileSystem.Set(WslOperationTestContext.Entry(source, "source", DirectoryEntryKind.File));
        using FileOperationGateway gateway = new(WslOperationTestContext.Adapter(fileSystem));

        await AssertCreatedAsync(gateway, location);
        await AssertRenamedAsync(gateway, source);
        WslPath renamedPath = WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home\\xi\\New.txt");
        await AssertConfirmedDeletionAsync(gateway, renamedPath);

        Assert.AreEqual(1, fileSystem.Created.Count);
        Assert.AreEqual("/.created", fileSystem.Created[0].LinuxPath[location.LinuxPath.Length..]);
        Assert.AreEqual(1, fileSystem.Renamed.Count);
        Assert.AreEqual(1, fileSystem.Deleted.Count);
    }

    /// <summary>Proves changed identity stops the mutation before any provider effect.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task RenameAsyncWhenIdentityChangesReturnsIdentityChangedWithoutEffect()
    {
        ScriptedWslFileSystem fileSystem = new();
        WslPath source = WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home\\old.txt");
        WslFileOperationAdapter adapter = WslOperationTestContext.Adapter(fileSystem);
        fileSystem.Set(WslOperationTestContext.Entry(source, "before", DirectoryEntryKind.File));
        FileEntrySnapshot snapshot = Assert.IsInstanceOfType<FileInspectionSucceeded>(
            await adapter.InspectAsync(source, CancellationToken.None)).Snapshot;
        fileSystem.Set(WslOperationTestContext.Entry(source, "after", DirectoryEntryKind.File));

        ProviderStepOutcome outcome = await adapter.RenameAsync(
            snapshot,
            WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home\\new.txt"),
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.IdentityChanged, outcome.Failure);
        Assert.HasCount(0, fileSystem.Renamed);
    }

    /// <summary>Proves provider, distribution, parent, link, and collision boundaries fail closed.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-003")]
    [TestProperty("ThreatId", "ADV-006")]
    public async Task MutationsWhenBoundaryIsInvalidReturnFailureWithoutEffect()
    {
        ScriptedWslFileSystem fileSystem = new();
        WslPath sourcePath = WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home\\old.txt");
        WslFileSystemEntry sourceEntry = WslOperationTestContext.Entry(sourcePath, "source", DirectoryEntryKind.File);
        fileSystem.Set(sourceEntry);
        WslFileOperationAdapter adapter = WslOperationTestContext.Adapter(fileSystem);
        FileEntrySnapshot source = WslOperationTestContext.Snapshot(sourceEntry);

        WslPath collision = await AssertRenameRefusalsAsync(adapter, source, fileSystem);
        await AssertCreateCollisionAsync(adapter, collision, fileSystem);
        await AssertDeleteRefusalsAsync(adapter, sourcePath, source, fileSystem);

        Assert.HasCount(0, fileSystem.Renamed);
        Assert.HasCount(0, fileSystem.Deleted);
    }

    /// <summary>Proves create and revalidation variants cannot widen their provider root.</summary>
    [TestMethod]
    public async Task CreateDirectoryAsyncWhenLocationOrTargetIsInvalidFailsClosed()
    {
        ScriptedWslFileSystem fileSystem = new();
        WslPath filePath = WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home\\file");
        WslFileSystemEntry file = WslOperationTestContext.Entry(filePath, "file", DirectoryEntryKind.File);
        fileSystem.Set(file);
        WslPath linkedPath = WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home\\linked");
        WslFileSystemEntry linked = WslOperationTestContext.Entry(
            linkedPath,
            "linked",
            DirectoryEntryKind.Directory,
            FileAttributes.ReparsePoint);
        fileSystem.Set(linked);
        WslFileOperationAdapter adapter = WslOperationTestContext.Adapter(fileSystem);

        ProviderStepOutcome notDirectory = await adapter.CreateDirectoryAsync(
            WslOperationTestContext.Snapshot(file),
            WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home\\file\\child"),
            CancellationToken.None);
        ProviderStepOutcome reparse = await adapter.CreateDirectoryAsync(
            WslOperationTestContext.Snapshot(linked),
            WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home\\linked\\child"),
            CancellationToken.None);
        ProviderStepOutcome otherProvider = await adapter.CreateDirectoryAsync(
            WslOperationTestContext.Snapshot(linked),
            WslOperationTestContext.Local("C:\\child"),
            CancellationToken.None);
        ProviderStepOutcome missing = await adapter.RenameAsync(
            WslOperationTestContext.Snapshot(WslOperationTestContext.Entry(
                WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home\\missing"),
                "missing",
                DirectoryEntryKind.File)),
            WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home\\new"),
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.NotFound, notDirectory.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, reparse.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, otherProvider.Failure);
        Assert.AreSame(FileOperationFailureKind.NotFound, missing.Failure);
        Assert.HasCount(0, fileSystem.Created);
        Assert.HasCount(0, fileSystem.Renamed);
    }

    private static async Task AssertCreatedAsync(FileOperationGateway gateway, WslPath location)
    {
        CreateDirectoryRequest create = Assert.IsInstanceOfType<CreateDirectoryRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(
                CreateDirectoryRequest.Create(location, ".created")).Request);
        FileOperationOutcome created = await gateway.ExecuteAsync(
            create,
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.IsNull(created.Failure, created.Failure?.GetType().Name);
        WslOperationTestContext.AssertEffect(created, FileOperationEffectKind.DirectoryCreated, create.Target);
    }

    private static async Task AssertRenamedAsync(FileOperationGateway gateway, WslPath source)
    {
        RenameRequest rename = Assert.IsInstanceOfType<RenameRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(
                RenameRequest.Create(source, "New.txt")).Request);
        FileOperationOutcome renamed = await gateway.ExecuteAsync(
            rename,
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.IsNull(renamed.Failure, renamed.Failure?.GetType().Name);
        WslOperationTestContext.AssertEffect(renamed, FileOperationEffectKind.Renamed, source);
    }

    private static async Task AssertConfirmedDeletionAsync(FileOperationGateway gateway, WslPath renamedPath)
    {
        DeleteRequest unconfirmed = Assert.IsInstanceOfType<DeleteRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(
                DeleteRequest.Create([renamedPath], null)).Request);
        DeleteRequest confirmed = Assert.IsInstanceOfType<DeleteRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(
                DeleteRequest.Create(
                    [renamedPath],
                    PermanentDeletionConfirmation.CreateFor(unconfirmed))).Request);
        FileOperationOutcome refused = await gateway.ExecuteAsync(
            unconfirmed,
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);
        FileOperationOutcome deleted = await gateway.ExecuteAsync(
            confirmed,
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ConfirmationRequired, refused.Failure);
        Assert.HasCount(0, refused.Effects);
        Assert.IsNull(deleted.Failure, deleted.Failure?.GetType().Name);
        WslOperationTestContext.AssertEffect(deleted, FileOperationEffectKind.PermanentlyDeleted, renamedPath);
    }

    private static async Task<WslPath> AssertRenameRefusalsAsync(WslFileOperationAdapter adapter, FileEntrySnapshot source, ScriptedWslFileSystem fileSystem)
    {
        ProviderStepOutcome otherProvider = await adapter.RenameAsync(
            source,
            WslOperationTestContext.Local("C:\\new.txt"),
            CancellationToken.None);
        ProviderStepOutcome otherDistribution = await adapter.RenameAsync(
            source,
            WslOperationTestContext.Wsl("\\\\wsl.localhost\\Debian\\home\\new.txt"),
            CancellationToken.None);
        ProviderStepOutcome otherParent = await adapter.RenameAsync(
            source,
            WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\other\\new.txt"),
            CancellationToken.None);
        WslPath collision = WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home\\existing.txt");
        fileSystem.Set(WslOperationTestContext.Entry(collision, "collision", DirectoryEntryKind.File));
        ProviderStepOutcome existing = await adapter.RenameAsync(source, collision, CancellationToken.None);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, otherProvider.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, otherDistribution.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, otherParent.Failure);
        Assert.AreSame(FileOperationFailureKind.Conflict, existing.Failure);
        return collision;
    }

    private static async Task AssertCreateCollisionAsync(WslFileOperationAdapter adapter, WslPath collision, ScriptedWslFileSystem fileSystem)
    {
        WslPath locationPath = WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home");
        WslFileSystemEntry location = WslOperationTestContext.Entry(locationPath, "location", DirectoryEntryKind.Directory);
        fileSystem.Set(location);
        ProviderStepOutcome existingDirectory = await adapter.CreateDirectoryAsync(
            WslOperationTestContext.Snapshot(location),
            collision,
            CancellationToken.None);
        Assert.AreSame(FileOperationFailureKind.Conflict, existingDirectory.Failure);
    }

    private static async Task AssertDeleteRefusalsAsync(WslFileOperationAdapter adapter, WslPath sourcePath, FileEntrySnapshot source, ScriptedWslFileSystem fileSystem)
    {
        ProviderStepOutcome recycled = await adapter.DeleteAsync(
            source,
            DeletionExecutionMode.Recycle,
            CancellationToken.None);
        WslFileSystemEntry linkEntry = WslOperationTestContext.Entry(
            sourcePath,
            "source",
            DirectoryEntryKind.File,
            FileAttributes.ReparsePoint);
        fileSystem.Set(linkEntry);
        ProviderStepOutcome link = await adapter.DeleteAsync(
            WslOperationTestContext.Snapshot(linkEntry),
            DeletionExecutionMode.Permanent,
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, recycled.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, link.Failure);
    }
}
