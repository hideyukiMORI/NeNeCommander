using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves mutation routing uses validated provider identity and the closed transfer route exactly once.</summary>
[TestClass]
public sealed class ProviderFileOperationPortTests
{
    /// <summary>Proves local and WSL inspections reach only their matching adapter.</summary>
    [TestMethod]
    public async Task InspectAsyncWhenProviderIsSupportedDelegatesToItsOnlyAdapter()
    {
        RecordingFileOperationPort windowsLocal = new();
        RecordingFileOperationPort wsl = new();
        RecordingFileOperationPort windowsLocalToWsl = new();
        ProviderFileOperationPort router = new(windowsLocal, wsl, windowsLocalToWsl, new RecordingFileOperationPort());

        _ = await router.InspectAsync(Path("C:\\item.txt"), CancellationToken.None);
        Assert.AreEqual(1, windowsLocal.InspectionCount);
        Assert.AreEqual(0, wsl.InspectionCount);

        _ = await router.InspectAsync(
            Path("\\\\wsl.localhost\\Ubuntu\\home\\item.txt"),
            CancellationToken.None);

        Assert.AreEqual(1, windowsLocal.InspectionCount);
        Assert.AreEqual(1, wsl.InspectionCount);
        Assert.AreEqual(0, windowsLocalToWsl.InspectionCount);
    }

    /// <summary>Proves every mutation member of a same-provider pair reaches that provider's adapter only.</summary>
    [TestMethod]
    [DataRow("ubuntu")]
    [DataRow("Debian")]
    public async Task MutationMembersWhenPairIsSameProviderDelegateToThatAdapter(string destinationDistribution)
    {
        RecordingFileOperationPort windowsLocal = new();
        RecordingFileOperationPort wsl = new();
        RecordingFileOperationPort windowsLocalToWsl = new();
        ProviderFileOperationPort router = new(windowsLocal, wsl, windowsLocalToWsl, new RecordingFileOperationPort());
        FileEntrySnapshot local = Snapshot("C:\\item.txt", "local");
        FileEntrySnapshot linux = Snapshot(
            "\\\\wsl.localhost\\Ubuntu\\home\\item.txt",
            "wsl");
        FileSystemPath localDestination = Path("C:\\destination");
        FileSystemPath wslDestination = Path(@"\\wsl.localhost\" + destinationDistribution + @"\destination");

        await InvokeEveryMemberAsync(router, local, localDestination);
        await InvokeEveryMemberAsync(router, linux, wslDestination);

        CollectionAssert.AreEqual(ExpectedMutationCalls(), windowsLocal.Calls);
        CollectionAssert.AreEqual(ExpectedMutationCalls(), wsl.Calls);
        Assert.HasCount(0, windowsLocalToWsl.Calls);
    }

    /// <summary>
    /// Proves the Windows local to WSL pair sends every transfer member to the cross transfer while
    /// deletion, directory creation, and rename still follow the source provider, so a composite
    /// move's permanent source deletion reaches the Windows local adapter and never the WSL side.
    /// </summary>
    [TestMethod]
    public async Task MutationMembersWhenPairIsWindowsLocalToWslRouteTransfersToCrossTransfer()
    {
        RecordingFileOperationPort windowsLocal = new();
        RecordingFileOperationPort wsl = new();
        RecordingFileOperationPort windowsLocalToWsl = new();
        RecordingFileOperationPort wslToWindows = new();
        ProviderFileOperationPort router = new(windowsLocal, wsl, windowsLocalToWsl, wslToWindows);
        FileEntrySnapshot source = Snapshot("C:\\item.txt", "first");
        FileEntrySnapshot second = Snapshot("D:\\other.txt", "other");
        FileSystemPath destination = Path("\\\\wsl.localhost\\Ubuntu\\destination");

        _ = await router.PreflightTransferAsync([source, second], destination, CancellationToken.None);
        await InvokeEveryMemberAsync(router, source, destination);

        CollectionAssert.AreEqual(ExpectedCrossTransferCalls(), windowsLocalToWsl.Calls);
        CollectionAssert.AreEqual(ExpectedSourceOwnedCalls(), windowsLocal.Calls);
        Assert.HasCount(0, wslToWindows.Calls);
        Assert.HasCount(0, wsl.Calls);
    }

    /// <summary>Proves the reverse route keeps source-owned mutations on the WSL adapter.</summary>
    [TestMethod]
    public async Task MutationMembersWhenPairIsWslToWindowsRouteTransfersToCrossTransfer()
    {
        RecordingFileOperationPort windowsLocal = new();
        RecordingFileOperationPort wsl = new();
        RecordingFileOperationPort windowsLocalToWsl = new();
        RecordingFileOperationPort wslToWindows = new();
        ProviderFileOperationPort router = new(windowsLocal, wsl, windowsLocalToWsl, wslToWindows);
        FileEntrySnapshot source = Snapshot("\\\\wsl.localhost\\Ubuntu\\item.txt", "first");
        FileEntrySnapshot second = Snapshot("\\\\wsl.localhost\\ubuntu\\other.txt", "other");
        FileSystemPath destination = Path("C:\\destination");

        _ = await router.PreflightTransferAsync([source, second], destination, CancellationToken.None);
        await InvokeEveryMemberAsync(router, source, destination);

        CollectionAssert.AreEqual(ExpectedCrossTransferCalls(), wslToWindows.Calls);
        CollectionAssert.AreEqual(ExpectedSourceOwnedCalls(), wsl.Calls);
        Assert.HasCount(0, windowsLocalToWsl.Calls);
        Assert.HasCount(0, windowsLocal.Calls);
    }

    /// <summary>
    /// Proves the composed router reports no atomic move for the Windows local to WSL pair, so the
    /// gateway composes copy, verification, and source deletion, while every pair without a route
    /// still answers <c>ProviderUnavailable</c> before any effect.
    /// </summary>
    [TestMethod]
    public async Task GetAtomicMoveCapabilityAsyncWhenComposedAllowsBothWindowsWslCompositeDirections()
    {
        ProviderFileOperationPort router = new(new WindowsLocalIoExecutionBoundary());
        FileEntrySnapshot local = Snapshot("C:\\item", "local");
        FileEntrySnapshot ubuntu = Snapshot("\\\\wsl.localhost\\Ubuntu\\item", "ubuntu");

        AtomicMoveCapabilityOutcome cross = await router.GetAtomicMoveCapabilityAsync(
            local,
            Path("\\\\wsl.localhost\\Ubuntu\\destination"),
            CancellationToken.None);
        AtomicMoveCapabilityOutcome reverse = await router.GetAtomicMoveCapabilityAsync(
            ubuntu,
            Path("C:\\destination"),
            CancellationToken.None);
        AtomicMoveCapabilityOutcome distributions = await router.GetAtomicMoveCapabilityAsync(
            ubuntu,
            Path("\\\\wsl.localhost\\Debian\\destination"),
            CancellationToken.None);
        AtomicMoveCapabilityOutcome unc = await router.GetAtomicMoveCapabilityAsync(
            local,
            Path("\\\\server\\share\\destination"),
            CancellationToken.None);

        Assert.AreSame(AtomicMoveCapabilityOutcome.Unsupported, cross);
        Assert.AreSame(AtomicMoveCapabilityOutcome.Unsupported, reverse);
        Assert.AreSame(AtomicMoveCapabilityOutcome.Unsupported, distributions);
        Assert.AreSame(
            FileOperationFailureKind.ProviderUnavailable,
            Assert.IsInstanceOfType<AtomicMoveCapabilityFailed>(unc).Failure);
    }

    /// <summary>
    /// Proves every pair without its own route, and every empty or mixed batch, fails closed at the
    /// router before any adapter or the cross transfer runs.
    /// </summary>
    /// <param name="sourceText">Source path text.</param>
    /// <param name="destinationText">Destination path text.</param>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-022")]
    [DataRow("\\\\server\\share\\item", "C:\\destination")]
    [DataRow("\\\\server\\share\\item", "\\\\wsl.localhost\\Ubuntu\\destination")]
    [DataRow("\\\\server\\share\\item", "\\\\server\\share\\destination")]
    [DataRow("C:\\item", "\\\\server\\share\\destination")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\item", "\\\\server\\share\\destination")]
    public async Task TransferMembersWhenPairHasNoRouteFailClosedWithoutDelegation(
        string sourceText,
        string destinationText)
    {
        RecordingFileOperationPort windowsLocal = new();
        RecordingFileOperationPort wsl = new();
        RecordingFileOperationPort windowsLocalToWsl = new();
        ProviderFileOperationPort router = new(windowsLocal, wsl, windowsLocalToWsl, new RecordingFileOperationPort());
        FileEntrySnapshot source = Snapshot(sourceText, "source");
        FileSystemPath destination = Path(destinationText);

        TransferPreflightOutcome preflight = await router.PreflightTransferAsync(
            [source],
            destination,
            CancellationToken.None);
        AtomicMoveCapabilityOutcome capability = await router.GetAtomicMoveCapabilityAsync(
            source,
            destination,
            CancellationToken.None);
        ProviderStepOutcome move = await router.MoveAsync(source, destination, CancellationToken.None);
        ProviderStepOutcome copy = await router.CopyAsync(source, destination, CancellationToken.None);
        CopyVerificationOutcome verify = await router.VerifyCopyAsync(source, destination, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, preflight.Failure);
        Assert.AreSame(
            FileOperationFailureKind.ProviderUnavailable,
            Assert.IsInstanceOfType<AtomicMoveCapabilityFailed>(capability).Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, move.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, copy.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, verify.Failure);
        Assert.HasCount(0, windowsLocal.Calls);
        Assert.HasCount(0, wsl.Calls);
        Assert.HasCount(0, windowsLocalToWsl.Calls);
    }

    /// <summary>Proves unsupported sources, empty batches, and mixed batches fail before any adapter invocation.</summary>
    [TestMethod]
    public async Task OperationsWhenProviderIsUnsupportedOrBatchIsMixedFailClosedWithoutDelegation()
    {
        RecordingFileOperationPort windowsLocal = new();
        RecordingFileOperationPort wsl = new();
        RecordingFileOperationPort windowsLocalToWsl = new();
        ProviderFileOperationPort router = new(windowsLocal, wsl, windowsLocalToWsl, new RecordingFileOperationPort());
        FileSystemPath unc = Path("\\\\server\\share\\item");

        await AssertInspectionAndBatchesFailClosedAsync(router, unc);
        await AssertUnsupportedSourceStepsFailClosedAsync(router, unc);

        Assert.HasCount(0, windowsLocal.Calls);
        Assert.HasCount(0, wsl.Calls);
        Assert.HasCount(0, windowsLocalToWsl.Calls);
    }

    private static async Task AssertInspectionAndBatchesFailClosedAsync(ProviderFileOperationPort router, FileSystemPath unc)
    {
        FileSystemPath ubuntuDestination = Path("\\\\wsl.localhost\\Ubuntu\\destination");
        FileEntrySnapshot local = Snapshot("C:\\item", "local");
        FileEntrySnapshot ubuntu = Snapshot("\\\\wsl.localhost\\Ubuntu\\item", "ubuntu");
        FileEntrySnapshot debian = Snapshot("\\\\wsl.localhost\\Debian\\item", "debian");

        FileInspectionOutcome inspection = await router.InspectAsync(unc, CancellationToken.None);
        TransferPreflightOutcome empty = await router.PreflightTransferAsync(
            [],
            ubuntuDestination,
            CancellationToken.None);
        TransferPreflightOutcome mixedProvider = await router.PreflightTransferAsync(
            [local, ubuntu], ubuntuDestination, CancellationToken.None);
        TransferPreflightOutcome mixedProviderReversed = await router.PreflightTransferAsync(
            [ubuntu, local], ubuntuDestination, CancellationToken.None);
        TransferPreflightOutcome mixedDistribution = await router.PreflightTransferAsync(
            [ubuntu, debian], ubuntuDestination, CancellationToken.None);
        TransferPreflightOutcome mixedReverseDistribution = await router.PreflightTransferAsync(
            [ubuntu, debian], Path("C:\\destination"), CancellationToken.None);

        Assert.AreSame(
            FileOperationFailureKind.ProviderUnavailable,
            Assert.IsInstanceOfType<FileInspectionFailed>(inspection).Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, empty.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, mixedProvider.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, mixedProviderReversed.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, mixedDistribution.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, mixedReverseDistribution.Failure);
    }

    private static async Task AssertUnsupportedSourceStepsFailClosedAsync(ProviderFileOperationPort router, FileSystemPath unc)
    {
        FileEntrySnapshot uncSource = Snapshot("\\\\server\\share\\item", "unc");

        ProviderStepOutcome unsupportedDelete = await router.DeleteAsync(
            uncSource,
            DeletionExecutionMode.Permanent,
            CancellationToken.None);
        ProviderStepOutcome unsupportedCreate = await router.CreateDirectoryAsync(
            uncSource,
            unc,
            CancellationToken.None);
        ProviderStepOutcome unsupportedRename = await router.RenameAsync(
            uncSource,
            unc,
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, unsupportedDelete.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, unsupportedCreate.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, unsupportedRename.Failure);
    }

    /// <summary>Proves required router arguments reject defects synchronously.</summary>
    [TestMethod]
    public void BoundariesWhenArgumentIsNullRejectDefect()
    {
        RecordingFileOperationPort port = new();
        ProviderFileOperationPort router = new(port, port, port, port);
        FileEntrySnapshot source = Snapshot("C:\\item", "source");
        FileSystemPath target = Path("C:\\target");

        AssertConstructorsRejectNull(port);
        AssertTransferMembersRejectNull(router, source, target);
        AssertSourceOwnedMembersRejectNull(router, source, target);
        Assert.HasCount(0, port.Calls);
    }

    private static void AssertConstructorsRejectNull(RecordingFileOperationPort port)
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ProviderFileOperationPort(null!, port, port, port));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ProviderFileOperationPort(port, null!, port, port));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ProviderFileOperationPort(port, port, null!, port));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ProviderFileOperationPort(port, port, port, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ProviderFileOperationPort(null!));
    }

    private static void AssertTransferMembersRejectNull(
        ProviderFileOperationPort router, FileEntrySnapshot source, FileSystemPath target)
    {
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
            () => router.MoveAsync(null!, target, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => router.MoveAsync(source, null!, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => router.CopyAsync(null!, target, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => router.CopyAsync(source, null!, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => router.VerifyCopyAsync(null!, target, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => router.VerifyCopyAsync(source, null!, CancellationToken.None));
    }

    private static void AssertSourceOwnedMembersRejectNull(
        ProviderFileOperationPort router, FileEntrySnapshot source, FileSystemPath target)
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => router.DeleteAsync(null!, DeletionExecutionMode.Permanent, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => router.DeleteAsync(source, null!, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => router.CreateDirectoryAsync(null!, target, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => router.CreateDirectoryAsync(source, null!, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => router.RenameAsync(null!, target, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => router.RenameAsync(source, null!, CancellationToken.None));
    }

    private static async Task InvokeEveryMemberAsync(
        ProviderFileOperationPort router,
        FileEntrySnapshot source,
        FileSystemPath destination)
    {
        _ = await router.PreflightTransferAsync([source], destination, CancellationToken.None);
        _ = await router.GetAtomicMoveCapabilityAsync(source, destination, CancellationToken.None);
        _ = await router.MoveAsync(source, destination, CancellationToken.None);
        _ = await router.CopyAsync(source, destination, CancellationToken.None);
        _ = await router.VerifyCopyAsync(source, destination, CancellationToken.None);
        _ = await router.DeleteAsync(source, DeletionExecutionMode.Permanent, CancellationToken.None);
        _ = await router.CreateDirectoryAsync(source, destination, CancellationToken.None);
        _ = await router.RenameAsync(source, destination, CancellationToken.None);
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

    private static string[] ExpectedCrossTransferCalls()
    {
        return ["preflight", "preflight", "capability", "move", "copy", "verify"];
    }

    private static string[] ExpectedSourceOwnedCalls()
    {
        return ["delete", "create", "rename"];
    }
}
