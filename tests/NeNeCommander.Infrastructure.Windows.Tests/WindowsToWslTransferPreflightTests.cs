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
/// Proves the preflight of the Windows local to WSL cross transfer against a real Windows source and
/// a scripted WSL destination, so every destination refusal is injected at its exact boundary.
/// </summary>
[TestClass]
public sealed class WindowsToWslTransferPreflightTests
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
        CopyVerificationOutcome unnamedVerify = await Transfer(deepDestination).VerifyCopyAsync(
            file,
            deep,
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Conflict, conflict.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, unnamed.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, unnamedCopy.Failure);
        Assert.AreSame(FileOperationFailureKind.Verification, unnamedVerify.Failure);
        Assert.HasCount(0, deepDestination.CopiedFromWindowsLocal);
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

    private static WindowsToWslTransfer Transfer(ScriptedWslFileSystem destination)
    {
        return new WindowsToWslTransfer(new WindowsLocalIoExecutionBoundary(), destination);
    }

    private static async Task<FileEntrySnapshot> SnapshotAsync(string path)
    {
        FileInspectionOutcome outcome = await new WindowsLocalFileOperationAdapter().InspectAsync(
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
