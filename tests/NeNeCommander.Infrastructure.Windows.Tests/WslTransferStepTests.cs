using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Preserves WslFileOperationAdapterTests evidence for WslTransferStepTests.</summary>
[TestClass]
public sealed class WslTransferStepTests
{
    /// <summary>Proves same-distribution transfer members support composite move but no atomic shortcut.</summary>
    [TestMethod]
    public async Task TransferMembersWhenSameDistributionUseCopyVerificationAndCompositeMove()
    {
        ScriptedWslFileSystem fileSystem = new();
        WslFileSystemEntry entry = WslOperationTestContext.Entry(
            WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home\\item"),
            "source",
            DirectoryEntryKind.File);
        FileEntrySnapshot source = WslOperationTestContext.Snapshot(entry);
        WslPath destination = WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\dest");
        fileSystem.Set(entry);
        fileSystem.Set(WslOperationTestContext.Entry(destination, "destination", DirectoryEntryKind.Directory));
        WslFileOperationAdapter adapter = WslOperationTestContext.Adapter(fileSystem);

        TransferPreflightOutcome preflight = await adapter.PreflightTransferAsync(
            [source], destination, CancellationToken.None);
        AtomicMoveCapabilityOutcome capability = await adapter.GetAtomicMoveCapabilityAsync(
            source, destination, CancellationToken.None);
        ProviderStepOutcome move = await adapter.MoveAsync(source, destination, CancellationToken.None);
        ProviderStepOutcome copy = await adapter.CopyAsync(source, destination, CancellationToken.None);
        CopyVerificationOutcome verify = await adapter.VerifyCopyAsync(source, destination, CancellationToken.None);
        AtomicMoveCapabilityOutcome foreignCapability = await adapter.GetAtomicMoveCapabilityAsync(
            source,
            WslOperationTestContext.Wsl("\\\\wsl.localhost\\Debian\\dest"),
            CancellationToken.None);

        Assert.IsNull(preflight.Failure);
        Assert.AreSame(AtomicMoveCapabilityOutcome.Unsupported, capability);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, move.Failure);
        Assert.IsNull(copy.Failure);
        Assert.IsNull(verify.Failure);
        Assert.AreSame(AtomicMoveCapabilityOutcome.Unsupported, foreignCapability);
    }

    /// <summary>Proves each transfer step independently revalidates provider, destination, and links.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-003")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task TransferStepsWhenBoundaryChangesFailClosed()
    {
        ScriptedWslFileSystem fileSystem = new();
        WslFileSystemEntry sourceEntry = WslOperationTestContext.Entry(
            WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home\\source"),
            "source",
            DirectoryEntryKind.Directory);
        FileEntrySnapshot source = WslOperationTestContext.Snapshot(sourceEntry);
        WslPath destination = WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\target");
        WslPath foreign = WslOperationTestContext.Wsl("\\\\wsl.localhost\\Debian\\target");
        WslPath linkedDestination = ConfigureTransferDestinations(fileSystem, sourceEntry, destination, foreign);
        WslFileOperationAdapter adapter = WslOperationTestContext.Adapter(fileSystem);

        ProviderStepOutcome foreignCopy = await adapter.CopyAsync(source, foreign, CancellationToken.None);
        ProviderStepOutcome missingCopy = await adapter.CopyAsync(
            source,
            WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\missing"),
            CancellationToken.None);
        ProviderStepOutcome linkedCopy = await adapter.CopyAsync(
            source, linkedDestination, CancellationToken.None);
        CopyVerificationOutcome foreignVerify = await adapter.VerifyCopyAsync(
            source, foreign, CancellationToken.None);
        CopyVerificationOutcome missingVerify = await adapter.VerifyCopyAsync(
            source,
            WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\missing"),
            CancellationToken.None);
        fileSystem.TargetContainsReparsePoint = true;
        CopyVerificationOutcome linkedTargetVerify = await adapter.VerifyCopyAsync(
            source, destination, CancellationToken.None);
        fileSystem.TargetContainsReparsePoint = false;
        await AssertNestedSourceRefusalsAsync(adapter, source, destination, fileSystem);

        Assert.AreSame(FileOperationFailureKind.Conflict, foreignCopy.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, missingCopy.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, linkedCopy.Failure);
        Assert.IsNull(foreignVerify.Failure);
        Assert.AreSame(FileOperationFailureKind.Verification, missingVerify.Failure);
        Assert.AreSame(FileOperationFailureKind.Verification, linkedTargetVerify.Failure);
        Assert.HasCount(0, fileSystem.Copied);
    }

    /// <summary>Proves copy repeats recursive containment validation at its side-effect boundary.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task CopyAsyncWhenDestinationIsInsideSourceReturnsConflictWithoutEffect()
    {
        ScriptedWslFileSystem fileSystem = new();
        WslFileSystemEntry sourceEntry = WslOperationTestContext.Entry(
            WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home\\source"),
            "source",
            DirectoryEntryKind.Directory);
        WslPath destination = WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home\\source\\child");
        fileSystem.Set(sourceEntry);
        fileSystem.Set(WslOperationTestContext.Entry(destination, "child", DirectoryEntryKind.Directory));
        WslFileOperationAdapter adapter = WslOperationTestContext.Adapter(fileSystem);

        ProviderStepOutcome outcome = await adapter.CopyAsync(
            WslOperationTestContext.Snapshot(sourceEntry),
            destination,
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Conflict, outcome.Failure);
        Assert.HasCount(0, fileSystem.Copied);
    }

    /// <summary>Proves verification refuses links introduced into either tree after copy.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-003")]
    [DataRow("Ubuntu")]
    [DataRow("Debian")]
    public async Task VerifyCopyAsyncWhenSourceOrTargetTreeGainsLinkReturnsVerificationFailure(string destinationDistribution)
    {
        ScriptedWslFileSystem fileSystem = new();
        WslFileSystemEntry sourceEntry = WslOperationTestContext.Entry(
            WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home\\source"),
            "source",
            DirectoryEntryKind.Directory);
        WslPath destination = WslOperationTestContext.Wsl(@"\\wsl.localhost\" + destinationDistribution + @"\target");
        WslPath target = WslOperationTestContext.Wsl(@"\\wsl.localhost\" + destinationDistribution + @"\target\source");
        fileSystem.Set(sourceEntry);
        fileSystem.Set(WslOperationTestContext.Entry(destination, "destination", DirectoryEntryKind.Directory));
        fileSystem.Set(WslOperationTestContext.Entry(target, "target", DirectoryEntryKind.Directory));
        WslFileOperationAdapter adapter = WslOperationTestContext.Adapter(fileSystem);

        fileSystem.ContainsNestedReparsePoint = true;
        CopyVerificationOutcome linkedSource = await adapter.VerifyCopyAsync(
            WslOperationTestContext.Snapshot(sourceEntry), destination, CancellationToken.None);
        fileSystem.ContainsNestedReparsePoint = false;
        fileSystem.TargetContainsReparsePoint = true;
        CopyVerificationOutcome linkedTarget = await adapter.VerifyCopyAsync(
            WslOperationTestContext.Snapshot(sourceEntry), destination, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Verification, linkedSource.Failure);
        Assert.AreSame(FileOperationFailureKind.Verification, linkedTarget.Failure);
    }

    private static WslPath ConfigureTransferDestinations(ScriptedWslFileSystem fileSystem, WslFileSystemEntry sourceEntry, WslPath destination, WslPath foreign)
    {
        WslPath linkedDestination = WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\linked");
        fileSystem.Set(sourceEntry);
        fileSystem.Set(WslOperationTestContext.Entry(destination, "destination", DirectoryEntryKind.Directory));
        fileSystem.Set(WslOperationTestContext.Entry(foreign, "foreign", DirectoryEntryKind.Directory));
        fileSystem.Set(WslOperationTestContext.Entry(
            WslOperationTestContext.Wsl("\\\\wsl.localhost\\Debian\\target\\source"),
            "foreign-copy",
            DirectoryEntryKind.Directory));
        fileSystem.Set(WslOperationTestContext.Entry(
            linkedDestination,
            "linked",
            DirectoryEntryKind.Directory,
            FileAttributes.ReparsePoint));
        return linkedDestination;
    }

    private static async Task AssertNestedSourceRefusalsAsync(WslFileOperationAdapter adapter, FileEntrySnapshot source, WslPath destination, ScriptedWslFileSystem fileSystem)
    {
        fileSystem.ContainsNestedReparsePoint = true;
        TransferPreflightOutcome nestedLinkPreflight = await adapter.PreflightTransferAsync(
            [source], destination, CancellationToken.None);
        ProviderStepOutcome nestedLinkCopy = await adapter.CopyAsync(
            source, destination, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, nestedLinkPreflight.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, nestedLinkCopy.Failure);
    }
}
