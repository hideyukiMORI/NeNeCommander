using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves mutation routing uses validated provider identity exactly once.</summary>
[TestClass]
public sealed class ProviderFileOperationPortTests
{
    /// <summary>Proves local and WSL inspections reach only their matching adapter.</summary>
    [TestMethod]
    public async Task InspectAsyncWhenProviderIsSupportedDelegatesToItsOnlyAdapter()
    {
        RecordingPort windowsLocal = new();
        RecordingPort wsl = new();
        ProviderFileOperationPort router = new(windowsLocal, wsl);

        _ = await router.InspectAsync(Path("C:\\item.txt"), CancellationToken.None);
        Assert.AreEqual(1, windowsLocal.InspectionCount);
        Assert.AreEqual(0, wsl.InspectionCount);

        _ = await router.InspectAsync(
            Path("\\\\wsl.localhost\\Ubuntu\\home\\item.txt"),
            CancellationToken.None);

        Assert.AreEqual(1, windowsLocal.InspectionCount);
        Assert.AreEqual(1, wsl.InspectionCount);
    }

    /// <summary>Proves every source-owned mutation member follows the frozen source provider.</summary>
    [TestMethod]
    public async Task MutationMembersWhenProviderIsSupportedDelegateToSourceAdapter()
    {
        RecordingPort windowsLocal = new();
        RecordingPort wsl = new();
        ProviderFileOperationPort router = new(windowsLocal, wsl);
        FileEntrySnapshot local = Snapshot("C:\\item.txt", "local");
        FileEntrySnapshot linux = Snapshot(
            "\\\\wsl.localhost\\Ubuntu\\home\\item.txt",
            "wsl");
        FileSystemPath localDestination = Path("C:\\destination");
        FileSystemPath wslDestination = Path("\\\\wsl.localhost\\Ubuntu\\destination");

        _ = await router.PreflightTransferAsync([local], localDestination, CancellationToken.None);
        _ = await router.GetAtomicMoveCapabilityAsync(local, localDestination, CancellationToken.None);
        _ = await router.MoveAsync(local, localDestination, CancellationToken.None);
        _ = await router.CopyAsync(local, localDestination, CancellationToken.None);
        _ = await router.VerifyCopyAsync(local, localDestination, CancellationToken.None);
        _ = await router.DeleteAsync(local, DeletionExecutionMode.Permanent, CancellationToken.None);
        _ = await router.CreateDirectoryAsync(local, localDestination, CancellationToken.None);
        _ = await router.RenameAsync(local, localDestination, CancellationToken.None);
        _ = await router.PreflightTransferAsync([linux], wslDestination, CancellationToken.None);
        _ = await router.GetAtomicMoveCapabilityAsync(linux, wslDestination, CancellationToken.None);
        _ = await router.MoveAsync(linux, wslDestination, CancellationToken.None);
        _ = await router.CopyAsync(linux, wslDestination, CancellationToken.None);
        _ = await router.VerifyCopyAsync(linux, wslDestination, CancellationToken.None);
        _ = await router.DeleteAsync(linux, DeletionExecutionMode.Permanent, CancellationToken.None);
        _ = await router.CreateDirectoryAsync(linux, wslDestination, CancellationToken.None);
        _ = await router.RenameAsync(linux, wslDestination, CancellationToken.None);

        CollectionAssert.AreEqual(ExpectedMutationCalls(), windowsLocal.Calls);
        CollectionAssert.AreEqual(ExpectedMutationCalls(), wsl.Calls);
    }

    /// <summary>Proves unsupported and mixed providers fail before any adapter invocation.</summary>
    [TestMethod]
    public async Task OperationsWhenProviderIsUnsupportedOrMixedFailClosedWithoutDelegation()
    {
        RecordingPort windowsLocal = new();
        RecordingPort wsl = new();
        ProviderFileOperationPort router = new(windowsLocal, wsl);
        FileSystemPath unc = Path("\\\\server\\share\\item");
        FileEntrySnapshot local = Snapshot("C:\\item", "local");
        FileEntrySnapshot ubuntu = Snapshot("\\\\wsl.localhost\\Ubuntu\\item", "ubuntu");
        FileEntrySnapshot debian = Snapshot("\\\\wsl.localhost\\Debian\\item", "debian");
        FileEntrySnapshot uncSource = Snapshot("\\\\server\\share\\item", "unc");

        FileInspectionOutcome inspection = await router.InspectAsync(unc, CancellationToken.None);
        ProviderStepOutcome empty = await router.PreflightTransferAsync([], unc, CancellationToken.None);
        ProviderStepOutcome mixedProvider = await router.PreflightTransferAsync(
            [local, ubuntu], unc, CancellationToken.None);
        ProviderStepOutcome mixedDistribution = await router.PreflightTransferAsync(
            [ubuntu, debian], unc, CancellationToken.None);
        AtomicMoveCapabilityOutcome unsupportedCapability = await router.GetAtomicMoveCapabilityAsync(
            uncSource,
            unc,
            CancellationToken.None);
        ProviderStepOutcome unsupportedDelete = await router.DeleteAsync(
            uncSource,
            DeletionExecutionMode.Permanent,
            CancellationToken.None);
        ProviderStepOutcome unsupportedRename = await router.RenameAsync(
            uncSource,
            unc,
            CancellationToken.None);

        Assert.AreSame(
            FileOperationFailureKind.ProviderUnavailable,
            Assert.IsInstanceOfType<FileInspectionFailed>(inspection).Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, empty.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, mixedProvider.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, mixedDistribution.Failure);
        Assert.AreSame(
            FileOperationFailureKind.ProviderUnavailable,
            Assert.IsInstanceOfType<AtomicMoveCapabilityFailed>(unsupportedCapability).Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, unsupportedDelete.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, unsupportedRename.Failure);
        Assert.HasCount(0, windowsLocal.Calls);
        Assert.HasCount(0, wsl.Calls);
    }

    /// <summary>Proves required router arguments reject defects synchronously.</summary>
    [TestMethod]
    public void BoundariesWhenArgumentIsNullRejectDefect()
    {
        RecordingPort port = new();
        ProviderFileOperationPort router = new(port, port);
        FileEntrySnapshot source = Snapshot("C:\\item", "source");
        FileSystemPath target = Path("C:\\target");

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ProviderFileOperationPort(null!, port));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ProviderFileOperationPort(port, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ProviderFileOperationPort(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => router.InspectAsync(null!, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => router.PreflightTransferAsync(null!, target, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => router.PreflightTransferAsync([source], null!, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => router.GetAtomicMoveCapabilityAsync(null!, target, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => router.GetAtomicMoveCapabilityAsync(source, null!, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => router.DeleteAsync(null!, DeletionExecutionMode.Permanent, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => router.DeleteAsync(source, null!, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => router.RenameAsync(null!, target, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => router.RenameAsync(source, null!, CancellationToken.None));
    }

    private static FileSystemPath Path(string text)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path;
    }

    private static FileEntrySnapshot Snapshot(string text, string identity)
    {
        FileIdentity parsed = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse(identity)).Identity;
        return FileEntrySnapshot.Create(Path(text), parsed, DeletionCapability.PermanentOnly);
    }

    private static string[] ExpectedMutationCalls()
    {
        return ["preflight", "capability", "move", "copy", "verify", "delete", "create", "rename"];
    }

    private sealed class RecordingPort : IFileOperationPort
    {
        internal int InspectionCount { get; private set; }

        internal List<string> Calls { get; } = [];

        public Task<FileInspectionOutcome> InspectAsync(FileSystemPath path, CancellationToken cancellationToken)
        {
            InspectionCount++;
            return Task.FromResult(FileInspectionOutcome.Failed(FileOperationFailureKind.NotFound));
        }

        public Task<ProviderStepOutcome> PreflightTransferAsync(
            IReadOnlyList<FileEntrySnapshot> sources,
            FileSystemPath destination,
            CancellationToken cancellationToken)
        {
            return Step("preflight");
        }

        public Task<AtomicMoveCapabilityOutcome> GetAtomicMoveCapabilityAsync(
            FileEntrySnapshot source,
            FileSystemPath destination,
            CancellationToken cancellationToken)
        {
            Calls.Add("capability");
            return Task.FromResult(AtomicMoveCapabilityOutcome.Unsupported);
        }

        public Task<ProviderStepOutcome> MoveAsync(
            FileEntrySnapshot source,
            FileSystemPath destination,
            CancellationToken cancellationToken)
        {
            return Step("move");
        }

        public Task<ProviderStepOutcome> CopyAsync(
            FileEntrySnapshot source,
            FileSystemPath destination,
            CancellationToken cancellationToken)
        {
            return Step("copy");
        }

        public Task<ProviderStepOutcome> VerifyCopyAsync(
            FileEntrySnapshot source,
            FileSystemPath destination,
            CancellationToken cancellationToken)
        {
            return Step("verify");
        }

        public Task<ProviderStepOutcome> DeleteAsync(
            FileEntrySnapshot source,
            DeletionExecutionMode mode,
            CancellationToken cancellationToken)
        {
            return Step("delete");
        }

        public Task<ProviderStepOutcome> CreateDirectoryAsync(
            FileEntrySnapshot location,
            FileSystemPath target,
            CancellationToken cancellationToken)
        {
            return Step("create");
        }

        public Task<ProviderStepOutcome> RenameAsync(
            FileEntrySnapshot source,
            FileSystemPath target,
            CancellationToken cancellationToken)
        {
            return Step("rename");
        }

        private Task<ProviderStepOutcome> Step(string name)
        {
            Calls.Add(name);
            return Task.FromResult(ProviderStepOutcome.Succeeded());
        }
    }
}
