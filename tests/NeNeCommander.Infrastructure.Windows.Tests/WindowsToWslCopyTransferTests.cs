using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>
/// Proves each member of the Windows local to WSL cross transfer against a real Windows source and
/// a scripted WSL destination, so every destination failure is injected at its exact boundary.
/// </summary>
[TestClass]
public sealed class WindowsToWslCopyTransferTests
{
    private static readonly WslPath Destination = Wsl("\\\\wsl.localhost\\Ubuntu\\dest");

    /// <summary>Proves preflight plans one WSL child per source with the destination's own path rules.</summary>
    [TestMethod]
    public async Task PreflightTransferAsyncWhenDestinationIsUsablePlansWslChildTargets()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot file = await SnapshotAsync(root.WriteFile("Item.TXT", "item"));
        FileEntrySnapshot tree = await SnapshotAsync(root.CreateDirectory("tree"));
        ScriptedWslFileSystem destination = UsableDestination();

        TransferPreflightOutcome outcome = await Transfer(destination).PreflightTransferAsync(
            [file, tree],
            Destination,
            CancellationToken.None);

        TransferPreflightSucceeded succeeded = Assert.IsInstanceOfType<TransferPreflightSucceeded>(outcome);
        Assert.HasCount(2, succeeded.Plan);
        Assert.AreSame(file, succeeded.Plan[0].Source);
        Assert.IsTrue(FileSystemPathIdentityComparer.Instance.Equals(
            Wsl("\\\\wsl.localhost\\Ubuntu\\dest\\Item.TXT"),
            succeeded.Plan[0].Target));
        Assert.AreSame(TransferDisposition.Transfer, succeeded.Plan[0].Disposition);
        Assert.AreSame(tree, succeeded.Plan[1].Source);
        Assert.IsTrue(FileSystemPathIdentityComparer.Instance.Equals(
            Wsl("\\\\wsl.localhost\\Ubuntu\\dest\\tree"),
            succeeded.Plan[1].Target));
    }

    /// <summary>Proves the WSL destination is checked before any source and each closed reason is kept.</summary>
    [TestMethod]
    public async Task PreflightTransferAsyncWhenDestinationIsUnusableRejectsWithItsReason()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot file = await SnapshotAsync(root.WriteFile("item.txt", "item"));
        ScriptedWslFileSystem missing = new();
        ScriptedWslFileSystem notDirectory = new();
        notDirectory.Set(ScriptedWslFileSystem.Entry(Destination, "file", DirectoryEntryKind.File));
        ScriptedWslFileSystem link = new();
        link.Set(ScriptedWslFileSystem.Entry(
            Destination,
            "link",
            DirectoryEntryKind.Directory,
            FileAttributes.ReparsePoint));
        ScriptedWslFileSystem lost = new() { Failure = new IOException("Synthetic provider loss.") };
        ScriptedWslFileSystem denied = new() { Failure = new UnauthorizedAccessException() };

        Assert.AreSame(FileOperationFailureKind.NotFound, await PreflightFailureAsync(missing, file));
        Assert.AreSame(FileOperationFailureKind.NotFound, await PreflightFailureAsync(notDirectory, file));
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, await PreflightFailureAsync(link, file));
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, await PreflightFailureAsync(lost, file));
        Assert.AreSame(FileOperationFailureKind.AccessDenied, await PreflightFailureAsync(denied, file));
        Assert.AreSame(
            FileOperationFailureKind.ProviderUnavailable,
            (await Transfer(UsableDestination()).PreflightTransferAsync(
                [file],
                Local(root.Resolve("elsewhere")),
                CancellationToken.None)).Failure);
    }

    /// <summary>Proves a changed, missing, or linked Windows source is refused at preflight.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    [TestProperty("ThreatId", "ADV-022")]
    public async Task PreflightTransferAsyncWhenSourceIsChangedMissingOrLinkedRejects()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot changed = await SnapshotAsync(root.WriteFile("changed.txt", "before"));
        _ = root.WriteFile("changed.txt", "after-rewrite");
        FileEntrySnapshot missing = await SnapshotAsync(root.WriteFile("missing.txt", "gone"));
        File.Delete(root.Resolve("missing.txt"));
        _ = root.CreateDirectory("target");
        FileEntrySnapshot link = await SnapshotAsync(root.CreateJunction("link", "target"));

        Assert.AreSame(
            FileOperationFailureKind.IdentityChanged,
            await PreflightFailureAsync(UsableDestination(), changed));
        Assert.AreSame(FileOperationFailureKind.NotFound, await PreflightFailureAsync(UsableDestination(), missing));
        Assert.AreSame(
            FileOperationFailureKind.ProviderUnavailable,
            await PreflightFailureAsync(UsableDestination(), link));
    }

    /// <summary>Proves an existing target and a target whose canonical text would overflow are refused.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-022")]
    public async Task PreflightTransferAsyncWhenTargetExistsOrCannotBeNamedRejects()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot file = await SnapshotAsync(root.WriteFile("item.txt", "item"));
        ScriptedWslFileSystem taken = UsableDestination();
        taken.Set(ScriptedWslFileSystem.Entry(
            Wsl("\\\\wsl.localhost\\Ubuntu\\dest\\item.txt"),
            "taken",
            DirectoryEntryKind.File));
        WslPath deep = DeepDestination();
        ScriptedWslFileSystem deepDestination = new();
        deepDestination.Set(ScriptedWslFileSystem.Entry(deep, "deep", DirectoryEntryKind.Directory));

        TransferPreflightOutcome conflict = await Transfer(taken).PreflightTransferAsync(
            [file],
            Destination,
            CancellationToken.None);
        TransferPreflightOutcome unnamed = await Transfer(deepDestination).PreflightTransferAsync(
            [file],
            deep,
            CancellationToken.None);
        ProviderStepOutcome unnamedCopy = await Transfer(deepDestination).CopyAsync(
            file,
            deep,
            CancellationToken.None);
        ProviderStepOutcome unnamedVerify = await Transfer(deepDestination).VerifyCopyAsync(
            file,
            deep,
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Conflict, conflict.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, unnamed.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, unnamedCopy.Failure);
        Assert.AreSame(FileOperationFailureKind.Verification, unnamedVerify.Failure);
        Assert.HasCount(0, deepDestination.CopiedFromWindowsLocal);
    }

    /// <summary>Proves a copy step writes through the WSL file system once and verification then matches.</summary>
    [TestMethod]
    public async Task CopyAsyncWhenDestinationIsUsableCopiesOnceAndVerifies()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string sourcePath = root.WriteFile("item.txt", "item");
        FileEntrySnapshot file = await SnapshotAsync(sourcePath);
        ScriptedWslFileSystem destination = UsableDestination();
        WindowsToWslCopyTransfer transfer = Transfer(destination);

        ProviderStepOutcome copy = await transfer.CopyAsync(file, Destination, CancellationToken.None);
        ProviderStepOutcome verify = await transfer.VerifyCopyAsync(file, Destination, CancellationToken.None);

        Assert.IsNull(copy.Failure);
        Assert.IsNull(copy.Effect);
        Assert.IsNull(verify.Failure);
        Assert.HasCount(1, destination.CopiedFromWindowsLocal);
        Assert.AreEqual(sourcePath, destination.CopiedFromWindowsLocal[0].Source.FullName);
        Assert.AreEqual(
            Wsl("\\\\wsl.localhost\\Ubuntu\\dest\\item.txt"),
            destination.CopiedFromWindowsLocal[0].Target);
    }

    /// <summary>Proves every pre-write refusal of the copy step leaves no target and reports no effect.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    [TestProperty("ThreatId", "ADV-022")]
    public async Task CopyAsyncWhenStepPreconditionFailsRefusesWithoutWriting()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot file = await SnapshotAsync(root.WriteFile("item.txt", "item"));
        FileEntrySnapshot tree = await SnapshotAsync(root.CreateDirectory("tree"));
        _ = root.CreateDirectory("tree\\sub");
        tree = await SnapshotAsync(root.Resolve("tree"));
        _ = root.CreateDirectory("outside");
        FileEntrySnapshot changed = await SnapshotAsync(root.WriteFile("changed.txt", "before"));
        _ = root.WriteFile("changed.txt", "after-rewrite");
        ScriptedWslFileSystem taken = UsableDestination();
        taken.Set(ScriptedWslFileSystem.Entry(
            Wsl("\\\\wsl.localhost\\Ubuntu\\dest\\item.txt"),
            "taken",
            DirectoryEntryKind.File));
        ScriptedWslFileSystem missing = new();
        ScriptedWslFileSystem usable = UsableDestination();
        ScriptedWslFileSystem lost = new() { Failure = new IOException("Synthetic provider loss.") };

        ProviderStepOutcome conflict = await Transfer(taken).CopyAsync(file, Destination, CancellationToken.None);
        ProviderStepOutcome unusable = await Transfer(missing).CopyAsync(file, Destination, CancellationToken.None);
        ProviderStepOutcome identity = await Transfer(usable).CopyAsync(changed, Destination, CancellationToken.None);
        ProviderStepOutcome foreign = await Transfer(usable).CopyAsync(
            file,
            Local(root.Resolve("outside")),
            CancellationToken.None);
        ProviderStepOutcome guarded = await Transfer(lost).CopyAsync(file, Destination, CancellationToken.None);
        // A grandchild link changes no identity field of the tree itself, so only the step's own
        // tree-wide reparse check can refuse it.
        _ = root.CreateJunction("tree\\sub\\link", "outside");
        ProviderStepOutcome linked = await Transfer(usable).CopyAsync(tree, Destination, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Conflict, conflict.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, unusable.Failure);
        Assert.AreSame(FileOperationFailureKind.IdentityChanged, identity.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, foreign.Failure);
        Assert.AreSame(FileOperationFailureKind.Copy, guarded.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, linked.Failure);
        Assert.IsTrue(new[] { conflict, unusable, identity, foreign, guarded, linked }.All(step => step.Effect is null));
        Assert.HasCount(0, taken.CopiedFromWindowsLocal);
        Assert.HasCount(0, usable.CopiedFromWindowsLocal);
    }

    /// <summary>Proves a failed write reports the partial target only when one exists, with the normalized reason.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    [TestProperty("ThreatId", "ADV-022")]
    public async Task CopyAsyncWhenWriteFailsReportsPartialTargetOnlyWhenCreated()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot file = await SnapshotAsync(root.WriteFile("item.txt", "item"));
        ScriptedWslFileSystem before = UsableDestination();
        before.FailCopyBeforeTarget = true;
        ScriptedWslFileSystem after = UsableDestination();
        after.FailCopyAfterTarget = true;
        ScriptedWslFileSystem deniedBefore = UsableDestination();
        deniedBefore.FailCopyBeforeTarget = true;
        deniedBefore.CopyFailure = new UnauthorizedAccessException();
        ScriptedWslFileSystem deniedAfter = UsableDestination();
        deniedAfter.FailCopyAfterTarget = true;
        deniedAfter.CopyFailure = new UnauthorizedAccessException();

        ProviderStepOutcome noTarget = await Transfer(before).CopyAsync(file, Destination, CancellationToken.None);
        ProviderStepOutcome partial = await Transfer(after).CopyAsync(file, Destination, CancellationToken.None);
        ProviderStepOutcome deniedNoTarget = await Transfer(deniedBefore).CopyAsync(
            file,
            Destination,
            CancellationToken.None);
        ProviderStepOutcome deniedPartial = await Transfer(deniedAfter).CopyAsync(
            file,
            Destination,
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Copy, noTarget.Failure);
        Assert.IsNull(noTarget.Effect);
        Assert.AreSame(FileOperationFailureKind.Copy, partial.Failure);
        Assert.AreSame(ProviderStepEffectKind.CopyTargetCreated, partial.Effect);
        Assert.AreSame(FileOperationFailureKind.AccessDenied, deniedNoTarget.Failure);
        Assert.IsNull(deniedNoTarget.Effect);
        Assert.AreSame(FileOperationFailureKind.AccessDenied, deniedPartial.Failure);
        Assert.AreSame(ProviderStepEffectKind.CopyTargetCreated, deniedPartial.Effect);
    }

    /// <summary>Proves verification fails closed for every missing, linked, damaged, foreign, or changed condition.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    [TestProperty("ThreatId", "ADV-022")]
    public async Task VerifyCopyAsyncWhenCopyCannotBeConfirmedFailsClosed()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot file = await SnapshotAsync(root.WriteFile("item.txt", "item"));
        _ = root.CreateDirectory("tree");
        _ = root.CreateDirectory("tree\\sub");
        FileEntrySnapshot tree = await SnapshotAsync(root.Resolve("tree"));
        _ = root.CreateDirectory("outside");
        FileEntrySnapshot changed = await SnapshotAsync(root.WriteFile("changed.txt", "before"));
        _ = root.WriteFile("changed.txt", "after-rewrite");
        ScriptedWslFileSystem absentTarget = UsableDestination();
        ScriptedWslFileSystem damaged = CopiedDestination("item.txt");
        damaged.MatchResult = false;
        ScriptedWslFileSystem linkedTarget = CopiedDestination("item.txt");
        linkedTarget.TargetContainsReparsePoint = true;
        ScriptedWslFileSystem missingDestination = new();
        ScriptedWslFileSystem matching = CopiedDestination("item.txt");
        ScriptedWslFileSystem treeCopied = CopiedDestination("tree");
        ScriptedWslFileSystem lost = CopiedDestination("item.txt");
        lost.Failure = new IOException("Synthetic provider loss.");

        ProviderStepOutcome absent = await Transfer(absentTarget).VerifyCopyAsync(file, Destination, CancellationToken.None);
        ProviderStepOutcome mismatch = await Transfer(damaged).VerifyCopyAsync(file, Destination, CancellationToken.None);
        ProviderStepOutcome link = await Transfer(linkedTarget).VerifyCopyAsync(file, Destination, CancellationToken.None);
        ProviderStepOutcome unusable = await Transfer(missingDestination).VerifyCopyAsync(
            file,
            Destination,
            CancellationToken.None);
        ProviderStepOutcome identity = await Transfer(matching).VerifyCopyAsync(changed, Destination, CancellationToken.None);
        ProviderStepOutcome foreign = await Transfer(matching).VerifyCopyAsync(
            file,
            Local(root.Resolve("outside")),
            CancellationToken.None);
        ProviderStepOutcome guarded = await Transfer(lost).VerifyCopyAsync(file, Destination, CancellationToken.None);
        ProviderStepOutcome treeBeforeLink = await Transfer(treeCopied).VerifyCopyAsync(
            tree,
            Destination,
            CancellationToken.None);
        _ = root.CreateJunction("tree\\sub\\link", "outside");
        ProviderStepOutcome sourceLink = await Transfer(treeCopied).VerifyCopyAsync(tree, Destination, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Verification, absent.Failure);
        Assert.AreSame(FileOperationFailureKind.Verification, mismatch.Failure);
        Assert.AreSame(FileOperationFailureKind.Verification, link.Failure);
        Assert.AreSame(FileOperationFailureKind.Verification, unusable.Failure);
        Assert.AreSame(FileOperationFailureKind.IdentityChanged, identity.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, foreign.Failure);
        Assert.AreSame(FileOperationFailureKind.Verification, guarded.Failure);
        Assert.IsNull(treeBeforeLink.Failure);
        Assert.AreSame(FileOperationFailureKind.Verification, sourceLink.Failure);
    }

    /// <summary>
    /// Proves the pair copies only: a move, its atomic capability, and every member that is not a
    /// transfer fail closed without touching either side.
    /// </summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-022")]
    public async Task NonCopyMembersWhenCalledFailClosedWithoutEffect()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string sourcePath = root.WriteFile("item.txt", "item");
        FileEntrySnapshot file = await SnapshotAsync(sourcePath);
        ScriptedWslFileSystem destination = UsableDestination();
        WindowsToWslCopyTransfer transfer = Transfer(destination);

        FileInspectionOutcome inspection = await transfer.InspectAsync(file.Path, CancellationToken.None);
        AtomicMoveCapabilityOutcome capability = await transfer.GetAtomicMoveCapabilityAsync(
            file,
            Destination,
            CancellationToken.None);
        ProviderStepOutcome move = await transfer.MoveAsync(file, Destination, CancellationToken.None);
        ProviderStepOutcome delete = await transfer.DeleteAsync(
            file,
            DeletionExecutionMode.Permanent,
            CancellationToken.None);
        ProviderStepOutcome create = await transfer.CreateDirectoryAsync(file, Destination, CancellationToken.None);
        ProviderStepOutcome rename = await transfer.RenameAsync(file, Destination, CancellationToken.None);

        Assert.AreSame(
            FileOperationFailureKind.ProviderUnavailable,
            Assert.IsInstanceOfType<FileInspectionFailed>(inspection).Failure);
        Assert.AreSame(
            FileOperationFailureKind.ProviderUnavailable,
            Assert.IsInstanceOfType<AtomicMoveCapabilityFailed>(capability).Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, move.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, delete.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, create.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, rename.Failure);
        Assert.HasCount(0, destination.CopiedFromWindowsLocal);
        Assert.AreEqual(0, destination.FindCount(Destination));
        Assert.AreEqual("item", File.ReadAllText(sourcePath));
    }

    /// <summary>Proves every required cross-transfer argument rejects defects synchronously.</summary>
    [TestMethod]
    public void BoundariesWhenArgumentIsNullRejectDefect()
    {
        ScriptedWslFileSystem fileSystem = new();
        WindowsToWslCopyTransfer transfer = Transfer(fileSystem);
        FileEntrySnapshot source = FileEntrySnapshot.Create(
            Local("C:\\item"),
            Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("source")).Identity,
            DeletionCapability.PermanentOnly);

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WindowsToWslCopyTransfer(null!, fileSystem));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => new WindowsToWslCopyTransfer(new WindowsLocalIoExecutionBoundary(), null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => transfer.InspectAsync(null!, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => transfer.PreflightTransferAsync(null!, Destination, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => transfer.PreflightTransferAsync([source], null!, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => transfer.GetAtomicMoveCapabilityAsync(null!, Destination, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => transfer.GetAtomicMoveCapabilityAsync(source, null!, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => transfer.MoveAsync(null!, Destination, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => transfer.MoveAsync(source, null!, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => transfer.CopyAsync(null!, Destination, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => transfer.CopyAsync(source, null!, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => transfer.VerifyCopyAsync(null!, Destination, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => transfer.VerifyCopyAsync(source, null!, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => transfer.DeleteAsync(null!, DeletionExecutionMode.Permanent, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => transfer.DeleteAsync(source, null!, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => transfer.CreateDirectoryAsync(null!, Destination, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => transfer.CreateDirectoryAsync(source, null!, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => transfer.RenameAsync(null!, Destination, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => transfer.RenameAsync(source, null!, CancellationToken.None));
        Assert.AreEqual(0, fileSystem.FindCount(Destination));
    }

    private static async Task<FileOperationFailureKind?> PreflightFailureAsync(
        ScriptedWslFileSystem destination,
        FileEntrySnapshot source)
    {
        TransferPreflightOutcome outcome = await Transfer(destination).PreflightTransferAsync(
            [source],
            Destination,
            CancellationToken.None);
        return Assert.IsInstanceOfType<TransferPreflightRejected>(outcome).Failure;
    }

    private static ScriptedWslFileSystem UsableDestination()
    {
        ScriptedWslFileSystem fileSystem = new();
        fileSystem.Set(ScriptedWslFileSystem.Entry(Destination, "destination", DirectoryEntryKind.Directory));
        return fileSystem;
    }

    private static ScriptedWslFileSystem CopiedDestination(string name)
    {
        ScriptedWslFileSystem fileSystem = UsableDestination();
        fileSystem.Set(ScriptedWslFileSystem.Entry(
            Wsl("\\\\wsl.localhost\\Ubuntu\\dest\\" + name),
            "copied",
            DirectoryEntryKind.File));
        return fileSystem;
    }

    private static WslPath DeepDestination()
    {
        // The destination itself is four code units below the canonical limit, so any child name
        // longer than three units, such as "item.txt", cannot be named beneath it.
        const int destinationLength = 32763;
        string prefix = "\\\\wsl.localhost\\Ubuntu";
        string body = string.Concat(Enumerable.Repeat("\\" + new string('d', 200), 162));
        string last = new('e', destinationLength - prefix.Length - body.Length - 1);
        WslPath destination = Wsl(prefix + body + "\\" + last);
        Assert.AreEqual(destinationLength, destination.CanonicalText.Length);
        return destination;
    }

    private static WindowsToWslCopyTransfer Transfer(ScriptedWslFileSystem destination)
    {
        return new WindowsToWslCopyTransfer(new WindowsLocalIoExecutionBoundary(), destination);
    }

    private static async Task<FileEntrySnapshot> SnapshotAsync(string path)
    {
        FileInspectionOutcome outcome = await new WindowsLocalFileOperationAdapter().InspectAsync(
            Local(path),
            CancellationToken.None);
        return Assert.IsInstanceOfType<FileInspectionSucceeded>(outcome).Snapshot;
    }

    private static FileSystemPath Local(string text)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path;
    }

    private static WslPath Wsl(string text)
    {
        return Assert.IsInstanceOfType<WslPath>(
            Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path);
    }
}
