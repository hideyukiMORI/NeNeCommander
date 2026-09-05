using System;
using System.Collections.Generic;
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

/// <summary>Proves WSL same-distribution mutations fail closed and report exact gateway effects.</summary>
[TestClass]
public sealed class WslFileOperationAdapterTests
{
    /// <summary>Proves inspection publishes permanent-only capability and the provider identity.</summary>
    [TestMethod]
    public async Task InspectAsyncWhenEntryExistsReturnsPermanentOnlySnapshot()
    {
        ScriptedWslFileSystem fileSystem = new();
        WslPath path = Wsl("\\\\wsl.localhost\\Ubuntu\\home\\item.txt");
        WslFileSystemEntry entry = Entry(path, "identity-a", DirectoryEntryKind.File);
        fileSystem.Set(entry);

        FileInspectionOutcome outcome = await Adapter(fileSystem).InspectAsync(path, CancellationToken.None);

        FileEntrySnapshot snapshot = Assert.IsInstanceOfType<FileInspectionSucceeded>(outcome).Snapshot;
        Assert.AreEqual(path, snapshot.Path);
        Assert.AreEqual(entry.Identity, snapshot.Identity);
        Assert.AreSame(DeletionCapability.PermanentOnly, snapshot.DeletionCapability);
    }

    /// <summary>Proves create, rename, and confirmed delete retain the canonical gateway effects.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-008")]
    public async Task ExecuteAsyncWhenSameDistributionMutationSucceedsReportsExactEffects()
    {
        ScriptedWslFileSystem fileSystem = new();
        WslPath location = Wsl("\\\\wsl.localhost\\Ubuntu\\home\\xi");
        WslPath source = Wsl("\\\\wsl.localhost\\Ubuntu\\home\\xi\\old.txt");
        fileSystem.Set(Entry(location, "location", DirectoryEntryKind.Directory));
        fileSystem.Set(Entry(source, "source", DirectoryEntryKind.File));
        using FileOperationGateway gateway = new(Adapter(fileSystem));

        CreateDirectoryRequest create = Assert.IsInstanceOfType<CreateDirectoryRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(
                CreateDirectoryRequest.Create(location, ".created")).Request);
        FileOperationOutcome created = await gateway.ExecuteAsync(
            create,
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        RenameRequest rename = Assert.IsInstanceOfType<RenameRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(
                RenameRequest.Create(source, "New.txt")).Request);
        FileOperationOutcome renamed = await gateway.ExecuteAsync(
            rename,
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        WslPath renamedPath = Wsl("\\\\wsl.localhost\\Ubuntu\\home\\xi\\New.txt");
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

        Assert.IsNull(created.Failure, created.Failure?.GetType().Name);
        AssertEffect(created, FileOperationEffectKind.DirectoryCreated, create.Target);
        Assert.IsNull(renamed.Failure, renamed.Failure?.GetType().Name);
        AssertEffect(renamed, FileOperationEffectKind.Renamed, source);
        Assert.AreSame(FileOperationFailureKind.ConfirmationRequired, refused.Failure);
        Assert.HasCount(0, refused.Effects);
        Assert.IsNull(deleted.Failure, deleted.Failure?.GetType().Name);
        AssertEffect(deleted, FileOperationEffectKind.PermanentlyDeleted, renamedPath);
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
        WslPath source = Wsl("\\\\wsl.localhost\\Ubuntu\\home\\old.txt");
        WslFileOperationAdapter adapter = Adapter(fileSystem);
        fileSystem.Set(Entry(source, "before", DirectoryEntryKind.File));
        FileEntrySnapshot snapshot = Assert.IsInstanceOfType<FileInspectionSucceeded>(
            await adapter.InspectAsync(source, CancellationToken.None)).Snapshot;
        fileSystem.Set(Entry(source, "after", DirectoryEntryKind.File));

        ProviderStepOutcome outcome = await adapter.RenameAsync(
            snapshot,
            Wsl("\\\\wsl.localhost\\Ubuntu\\home\\new.txt"),
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
        WslPath sourcePath = Wsl("\\\\wsl.localhost\\Ubuntu\\home\\old.txt");
        WslFileSystemEntry sourceEntry = Entry(sourcePath, "source", DirectoryEntryKind.File);
        fileSystem.Set(sourceEntry);
        WslFileOperationAdapter adapter = Adapter(fileSystem);
        FileEntrySnapshot source = Snapshot(sourceEntry);

        ProviderStepOutcome otherProvider = await adapter.RenameAsync(
            source,
            Local("C:\\new.txt"),
            CancellationToken.None);
        ProviderStepOutcome otherDistribution = await adapter.RenameAsync(
            source,
            Wsl("\\\\wsl.localhost\\Debian\\home\\new.txt"),
            CancellationToken.None);
        ProviderStepOutcome otherParent = await adapter.RenameAsync(
            source,
            Wsl("\\\\wsl.localhost\\Ubuntu\\other\\new.txt"),
            CancellationToken.None);
        WslPath collision = Wsl("\\\\wsl.localhost\\Ubuntu\\home\\existing.txt");
        fileSystem.Set(Entry(collision, "collision", DirectoryEntryKind.File));
        ProviderStepOutcome existing = await adapter.RenameAsync(source, collision, CancellationToken.None);
        WslPath locationPath = Wsl("\\\\wsl.localhost\\Ubuntu\\home");
        WslFileSystemEntry location = Entry(locationPath, "location", DirectoryEntryKind.Directory);
        fileSystem.Set(location);
        ProviderStepOutcome existingDirectory = await adapter.CreateDirectoryAsync(
            Snapshot(location),
            collision,
            CancellationToken.None);
        ProviderStepOutcome recycled = await adapter.DeleteAsync(
            source,
            DeletionExecutionMode.Recycle,
            CancellationToken.None);
        WslFileSystemEntry linkEntry = Entry(
            sourcePath,
            "source",
            DirectoryEntryKind.File,
            FileAttributes.ReparsePoint);
        fileSystem.Set(linkEntry);
        ProviderStepOutcome link = await adapter.DeleteAsync(
            Snapshot(linkEntry),
            DeletionExecutionMode.Permanent,
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, otherProvider.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, otherDistribution.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, otherParent.Failure);
        Assert.AreSame(FileOperationFailureKind.Conflict, existing.Failure);
        Assert.AreSame(FileOperationFailureKind.Conflict, existingDirectory.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, recycled.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, link.Failure);
        Assert.HasCount(0, fileSystem.Renamed);
        Assert.HasCount(0, fileSystem.Deleted);
    }

    /// <summary>Proves missing entries and expected platform failures are normalized.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-017")]
    public async Task ProviderFailureWhenExpectedReturnsCanonicalFailure()
    {
        ScriptedWslFileSystem missing = new();
        WslPath path = Wsl("\\\\wsl.localhost\\Ubuntu\\home\\missing");
        FileInspectionFailed absent = Assert.IsInstanceOfType<FileInspectionFailed>(
            await Adapter(missing).InspectAsync(path, CancellationToken.None));
        ScriptedWslFileSystem denied = new() { Failure = new UnauthorizedAccessException() };
        FileInspectionFailed inaccessible = Assert.IsInstanceOfType<FileInspectionFailed>(
            await Adapter(denied).InspectAsync(path, CancellationToken.None));
        ScriptedWslFileSystem failedMutation = new();
        WslFileSystemEntry sourceEntry = Entry(path, "source", DirectoryEntryKind.File);
        failedMutation.Set(sourceEntry);
        failedMutation.Failure = new IOException("Synthetic provider loss.");
        ProviderStepOutcome failed = await Adapter(failedMutation).DeleteAsync(
            Snapshot(sourceEntry),
            DeletionExecutionMode.Permanent,
            CancellationToken.None);
        ScriptedWslFileSystem deniedMutation = new();
        deniedMutation.Set(sourceEntry);
        deniedMutation.Failure = new UnauthorizedAccessException();
        ProviderStepOutcome deniedDelete = await Adapter(deniedMutation).DeleteAsync(
            Snapshot(sourceEntry),
            DeletionExecutionMode.Permanent,
            CancellationToken.None);
        FileInspectionFailed unsupported = Assert.IsInstanceOfType<FileInspectionFailed>(
            await Adapter(new ScriptedWslFileSystem()).InspectAsync(
                Local("C:\\item"),
                CancellationToken.None));
        ScriptedWslFileSystem unavailable = new() { Failure = new IOException("Synthetic loss.") };
        FileInspectionFailed unavailableInspection = Assert.IsInstanceOfType<FileInspectionFailed>(
            await Adapter(unavailable).InspectAsync(path, CancellationToken.None));

        Assert.AreSame(FileOperationFailureKind.NotFound, absent.Failure);
        Assert.AreSame(FileOperationFailureKind.AccessDenied, inaccessible.Failure);
        Assert.AreSame(FileOperationFailureKind.Delete, failed.Failure);
        Assert.AreSame(FileOperationFailureKind.AccessDenied, deniedDelete.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, unsupported.Failure);
        Assert.AreSame(FileOperationFailureKind.Inspection, unavailableInspection.Failure);
        Assert.HasCount(0, failedMutation.Deleted);
    }

    /// <summary>Proves transfer members remain unavailable until their separate provider slice.</summary>
    [TestMethod]
    public async Task TransferMembersBeforeTransferSliceFailClosed()
    {
        ScriptedWslFileSystem fileSystem = new();
        WslFileSystemEntry entry = Entry(
            Wsl("\\\\wsl.localhost\\Ubuntu\\home\\item"),
            "source",
            DirectoryEntryKind.File);
        FileEntrySnapshot source = Snapshot(entry);
        WslPath destination = Wsl("\\\\wsl.localhost\\Ubuntu\\dest");
        WslFileOperationAdapter adapter = Adapter(fileSystem);

        ProviderStepOutcome preflight = await adapter.PreflightTransferAsync(
            [source], destination, CancellationToken.None);
        AtomicMoveCapabilityOutcome capability = await adapter.GetAtomicMoveCapabilityAsync(
            source, destination, CancellationToken.None);
        ProviderStepOutcome move = await adapter.MoveAsync(source, destination, CancellationToken.None);
        ProviderStepOutcome copy = await adapter.CopyAsync(source, destination, CancellationToken.None);
        ProviderStepOutcome verify = await adapter.VerifyCopyAsync(source, destination, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, preflight.Failure);
        Assert.AreSame(
            FileOperationFailureKind.ProviderUnavailable,
            Assert.IsInstanceOfType<AtomicMoveCapabilityFailed>(capability).Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, move.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, copy.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, verify.Failure);
    }

    /// <summary>Proves create and revalidation variants cannot widen their provider root.</summary>
    [TestMethod]
    public async Task CreateDirectoryAsyncWhenLocationOrTargetIsInvalidFailsClosed()
    {
        ScriptedWslFileSystem fileSystem = new();
        WslPath filePath = Wsl("\\\\wsl.localhost\\Ubuntu\\home\\file");
        WslFileSystemEntry file = Entry(filePath, "file", DirectoryEntryKind.File);
        fileSystem.Set(file);
        WslPath linkedPath = Wsl("\\\\wsl.localhost\\Ubuntu\\home\\linked");
        WslFileSystemEntry linked = Entry(
            linkedPath,
            "linked",
            DirectoryEntryKind.Directory,
            FileAttributes.ReparsePoint);
        fileSystem.Set(linked);
        WslFileOperationAdapter adapter = Adapter(fileSystem);

        ProviderStepOutcome notDirectory = await adapter.CreateDirectoryAsync(
            Snapshot(file),
            Wsl("\\\\wsl.localhost\\Ubuntu\\home\\file\\child"),
            CancellationToken.None);
        ProviderStepOutcome reparse = await adapter.CreateDirectoryAsync(
            Snapshot(linked),
            Wsl("\\\\wsl.localhost\\Ubuntu\\home\\linked\\child"),
            CancellationToken.None);
        ProviderStepOutcome otherProvider = await adapter.CreateDirectoryAsync(
            Snapshot(linked),
            Local("C:\\child"),
            CancellationToken.None);
        ProviderStepOutcome missing = await adapter.RenameAsync(
            Snapshot(Entry(
                Wsl("\\\\wsl.localhost\\Ubuntu\\home\\missing"),
                "missing",
                DirectoryEntryKind.File)),
            Wsl("\\\\wsl.localhost\\Ubuntu\\home\\new"),
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.NotFound, notDirectory.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, reparse.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, otherProvider.Failure);
        Assert.AreSame(FileOperationFailureKind.NotFound, missing.Failure);
        Assert.HasCount(0, fileSystem.Created);
        Assert.HasCount(0, fileSystem.Renamed);
    }

    /// <summary>Proves required adapter and entry arguments reject defects synchronously.</summary>
    [TestMethod]
    public void BoundariesWhenArgumentIsNullRejectDefect()
    {
        ScriptedWslFileSystem fileSystem = new();
        WindowsLocalIoExecutionBoundary boundary = new();
        WslFileOperationAdapter adapter = Adapter(fileSystem);
        WslPath path = Wsl("\\\\wsl.localhost\\Ubuntu\\home\\item");
        WslFileSystemEntry entry = Entry(path, "identity", DirectoryEntryKind.File);
        FileEntrySnapshot snapshot = Snapshot(entry);

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WslFileOperationAdapter(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WslFileOperationAdapter(boundary, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => new WslFileSystemEntry(null!, entry.Identity, entry.Kind, entry.Attributes));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => new WslFileSystemEntry(path, null!, entry.Kind, entry.Attributes));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => new WslFileSystemEntry(path, entry.Identity, null!, entry.Attributes));
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
            () => adapter.CopyAsync(snapshot, null!, CancellationToken.None));
    }

    private static void AssertEffect(
        FileOperationOutcome outcome,
        FileOperationEffectKind expectedKind,
        FileSystemPath expectedSource)
    {
        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.HasCount(1, outcome.Effects);
        Assert.AreSame(expectedKind, outcome.Effects[0].Kind);
        Assert.AreEqual(expectedSource, outcome.Effects[0].Source);
    }

    private static WslFileOperationAdapter Adapter(ScriptedWslFileSystem fileSystem)
    {
        return new WslFileOperationAdapter(new WindowsLocalIoExecutionBoundary(), fileSystem);
    }

    private static WslFileSystemEntry Entry(
        WslPath path,
        string identity,
        DirectoryEntryKind kind,
        FileAttributes attributes = FileAttributes.None)
    {
        FileIdentity parsed = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse(identity)).Identity;
        return new WslFileSystemEntry(path, parsed, kind, attributes);
    }

    private static FileEntrySnapshot Snapshot(WslFileSystemEntry entry)
    {
        return FileEntrySnapshot.Create(entry.Path, entry.Identity, DeletionCapability.PermanentOnly);
    }

    private static WslPath Wsl(string text)
    {
        return Assert.IsInstanceOfType<WslPath>(Path(text));
    }

    private static FileSystemPath Local(string text)
    {
        return Path(text);
    }

    private static FileSystemPath Path(string text)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path;
    }

    private sealed class ScriptedWslFileSystem : IWslFileSystem
    {
        private readonly Dictionary<string, WslFileSystemEntry> _entries = new(StringComparer.Ordinal);

        internal Exception? Failure { get; set; }

        internal List<WslPath> Created { get; } = [];

        internal List<(WslFileSystemEntry Source, WslPath Target)> Renamed { get; } = [];

        internal List<WslFileSystemEntry> Deleted { get; } = [];

        public WslFileSystemEntry? Find(WslPath path)
        {
            ThrowWhenConfigured();
            return _entries.GetValueOrDefault(path.CanonicalText);
        }

        public bool TargetExists(WslPath path)
        {
            ThrowWhenConfigured();
            return _entries.ContainsKey(path.CanonicalText);
        }

        public void CreateDirectory(WslPath target)
        {
            ThrowWhenConfigured();
            Created.Add(target);
            Set(Entry(target, "created", DirectoryEntryKind.Directory));
        }

        public void Rename(WslFileSystemEntry source, WslPath target)
        {
            ThrowWhenConfigured();
            Renamed.Add((source, target));
            _ = _entries.Remove(source.Path.CanonicalText);
            Set(new WslFileSystemEntry(target, source.Identity, source.Kind, source.Attributes));
        }

        public void Delete(WslFileSystemEntry source)
        {
            ThrowWhenConfigured();
            Deleted.Add(source);
            _ = _entries.Remove(source.Path.CanonicalText);
        }

        internal void Set(WslFileSystemEntry entry)
        {
            _entries[entry.Path.CanonicalText] = entry;
        }

        private void ThrowWhenConfigured()
        {
            if (Failure is not null)
            {
                throw Failure;
            }
        }
    }
}
