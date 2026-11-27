using System.Collections.Generic;
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
public sealed class WindowsToWslTransferTests
{
    private static readonly WslPath Destination = Wsl("\\\\wsl.localhost\\Ubuntu\\dest");

    /// <summary>Proves a copy step writes through the WSL file system once and verification then matches.</summary>
    [TestMethod]
    public async Task CopyAsyncWhenDestinationIsUsableCopiesOnceAndVerifies()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string sourcePath = root.WriteFile("item.txt", "item");
        FileEntrySnapshot file = await SnapshotAsync(sourcePath);
        ScriptedWslFileSystem destination = UsableDestination();
        WindowsToWslTransfer transfer = Transfer(destination);

        ProviderStepOutcome copy = await transfer.CopyAsync(file, Destination, CancellationToken.None);
        CopyVerificationOutcome verify = await transfer.VerifyCopyAsync(file, Destination, CancellationToken.None);

        Assert.IsNull(copy.Failure);
        Assert.IsEmpty(copy.Receipt.Effects);
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
        _ = root.CreateDirectory("tree\\sub");
        FileEntrySnapshot tree = await SnapshotAsync(root.Resolve("tree"));
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
        Assert.IsTrue(new[] { conflict, unusable, identity, foreign, guarded, linked }.All(step => step.Receipt.Effects.Count == 0));
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
        Assert.IsEmpty(noTarget.Receipt.Effects);
        Assert.AreSame(FileOperationFailureKind.Copy, partial.Failure);
        CollectionAssert.AreEqual(new[] { ProviderStepEffectKind.CopyTargetCreated }, new List<ProviderStepEffectKind>(partial.Receipt.Effects));
        Assert.AreSame(FileOperationFailureKind.AccessDenied, deniedNoTarget.Failure);
        Assert.IsEmpty(deniedNoTarget.Receipt.Effects);
        Assert.AreSame(FileOperationFailureKind.AccessDenied, deniedPartial.Failure);
        CollectionAssert.AreEqual(new[] { ProviderStepEffectKind.CopyTargetCreated }, new List<ProviderStepEffectKind>(deniedPartial.Receipt.Effects));
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

        await AssertDestinationConditionsFailVerificationAsync(file);
        await AssertSourceConditionsFailVerificationAsync(root, file, changed, tree);
    }

    private static async Task AssertDestinationConditionsFailVerificationAsync(FileEntrySnapshot file)
    {
        ScriptedWslFileSystem absentTarget = UsableDestination();
        ScriptedWslFileSystem damaged = CopiedDestination("item.txt");
        damaged.MatchResult = false;
        ScriptedWslFileSystem linkedTarget = CopiedDestination("item.txt");
        linkedTarget.TargetContainsReparsePoint = true;
        ScriptedWslFileSystem missingDestination = new();

        CopyVerificationOutcome absent = await Transfer(absentTarget).VerifyCopyAsync(file, Destination, CancellationToken.None);
        CopyVerificationOutcome mismatch = await Transfer(damaged).VerifyCopyAsync(file, Destination, CancellationToken.None);
        CopyVerificationOutcome link = await Transfer(linkedTarget).VerifyCopyAsync(file, Destination, CancellationToken.None);
        CopyVerificationOutcome unusable = await Transfer(missingDestination).VerifyCopyAsync(
            file,
            Destination,
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Verification, absent.Failure);
        Assert.AreSame(FileOperationFailureKind.Verification, mismatch.Failure);
        Assert.AreSame(FileOperationFailureKind.Verification, link.Failure);
        Assert.AreSame(FileOperationFailureKind.Verification, unusable.Failure);
    }

    private static async Task AssertSourceConditionsFailVerificationAsync(
        TestOwnedTemporaryRoot root, FileEntrySnapshot file, FileEntrySnapshot changed, FileEntrySnapshot tree)
    {
        ScriptedWslFileSystem matching = CopiedDestination("item.txt");
        ScriptedWslFileSystem treeCopied = CopiedDestination("tree");
        ScriptedWslFileSystem lost = CopiedDestination("item.txt");
        lost.Failure = new IOException("Synthetic provider loss.");

        CopyVerificationOutcome identity = await Transfer(matching).VerifyCopyAsync(changed, Destination, CancellationToken.None);
        CopyVerificationOutcome foreign = await Transfer(matching).VerifyCopyAsync(
            file,
            Local(root.Resolve("outside")),
            CancellationToken.None);
        CopyVerificationOutcome guarded = await Transfer(lost).VerifyCopyAsync(file, Destination, CancellationToken.None);
        CopyVerificationOutcome treeBeforeLink = await Transfer(treeCopied).VerifyCopyAsync(
            tree,
            Destination,
            CancellationToken.None);
        _ = root.CreateJunction("tree\\sub\\link", "outside");
        CopyVerificationOutcome sourceLink = await Transfer(treeCopied).VerifyCopyAsync(tree, Destination, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.IdentityChanged, identity.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, foreign.Failure);
        Assert.AreSame(FileOperationFailureKind.Verification, guarded.Failure);
        Assert.IsNull(treeBeforeLink.Failure);
        Assert.AreSame(FileOperationFailureKind.Verification, sourceLink.Failure);
    }

    /// <summary>
    /// Proves the pair has no atomic move, so the gateway composes a move from copy, verification,
    /// and the source provider's deletion, while a direct move and every member that is not a copy
    /// or verification step fail closed without touching either side.
    /// </summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-022")]
    public async Task NonCopyMembersWhenCalledReportNoAtomicMoveAndFailClosedWithoutEffect()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string sourcePath = root.WriteFile("item.txt", "item");
        FileEntrySnapshot file = await SnapshotAsync(sourcePath);
        ScriptedWslFileSystem destination = UsableDestination();
        WindowsToWslTransfer transfer = Transfer(destination);

        FileInspectionOutcome inspection = await transfer.InspectAsync(file.Path, FileInspectionPurpose.Deletion, CancellationToken.None);
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
        Assert.AreSame(AtomicMoveCapabilityOutcome.Unsupported, capability);
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
        WindowsToWslTransfer transfer = Transfer(fileSystem);
        FileEntrySnapshot source = FileEntrySnapshot.Create(
            Local("C:\\item"),
            Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("source")).Identity,
            DeletionCapability.PermanentOnly);

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WindowsToWslTransfer(null!, fileSystem));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => new WindowsToWslTransfer(new WindowsLocalIoExecutionBoundary(), null!));
        AssertTransferMembersRejectNull(transfer, source);
        AssertSourceOwnedMembersRejectNull(transfer, source);
        Assert.AreEqual(0, fileSystem.FindCount(Destination));
    }

    private static void AssertTransferMembersRejectNull(WindowsToWslTransfer transfer, FileEntrySnapshot source)
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => transfer.InspectAsync(null!, FileInspectionPurpose.Deletion, CancellationToken.None));
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
    }

    private static void AssertSourceOwnedMembersRejectNull(WindowsToWslTransfer transfer, FileEntrySnapshot source)
    {
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

    private static WindowsToWslTransfer Transfer(ScriptedWslFileSystem destination)
    {
        return new WindowsToWslTransfer(new WindowsLocalIoExecutionBoundary(), destination);
    }

    private static async Task<FileEntrySnapshot> SnapshotAsync(string path)
    {
        FileInspectionOutcome outcome = await new WindowsLocalFileOperationAdapter(new DeterministicIdentifierSource()).InspectAsync(
            Local(path),
            FileInspectionPurpose.Deletion,
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
