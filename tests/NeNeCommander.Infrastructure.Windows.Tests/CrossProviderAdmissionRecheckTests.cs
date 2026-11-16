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

/// <summary>
/// Proves cross-provider and WSL steps recheck their Windows drive or WSL destination admission at the
/// last moment, so a remap after the earlier admission alone stops the step.
/// </summary>
[TestClass]
public sealed class CrossProviderAdmissionRecheckTests
{
    private const string WslDestination = "\\\\wsl.localhost\\Ubuntu\\destination";

    /// <summary>Proves a Windows-to-WSL copy stops when the source drive is remapped just before the copy.</summary>
    [TestMethod]
    public async Task CopyAsyncWhenSourceDriveIsRemappedBeforeEffectFailsWithoutCopyingAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string sourceText = root.WriteFile("source.txt", "source");
        ScriptedWslFileSystem fileSystem = DestinationFileSystem();
        ScriptedDriveTypes drives = new(sourceText, 2);
        WindowsToWslTransfer transfer = new(new WindowsLocalIoExecutionBoundary(), fileSystem, drives.Boundary());

        ProviderStepOutcome outcome = await transfer.CopyAsync(
            WindowsLocalOperationTestContext.LocalSnapshot(sourceText), WslOperationTestContext.Wsl(WslDestination),
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
        Assert.AreEqual(3, drives.Queries);
        Assert.IsEmpty(fileSystem.CopiedFromWindowsLocal);
    }

    /// <summary>Proves Windows-to-WSL verification stops when the source drive is remapped at its final check.</summary>
    [TestMethod]
    public async Task VerifyCopyAsyncWhenSourceDriveIsRemappedAtFinalCheckFailsProviderUnavailableAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string sourceText = root.WriteFile("source.txt", "source");
        ScriptedWslFileSystem fileSystem = DestinationFileSystem();
        fileSystem.Set(WslOperationTestContext.Entry(
            WslOperationTestContext.Wsl(WslDestination + "\\source.txt"), "copied", DirectoryEntryKind.File));
        ScriptedDriveTypes drives = new(sourceText, 4);
        WindowsToWslTransfer transfer = new(new WindowsLocalIoExecutionBoundary(), fileSystem, drives.Boundary());

        CopyVerificationOutcome outcome = await transfer.VerifyCopyAsync(
            WindowsLocalOperationTestContext.LocalSnapshot(sourceText), WslOperationTestContext.Wsl(WslDestination),
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, Assert.IsInstanceOfType<CopyVerificationFailed>(outcome).Failure);
        Assert.AreEqual(5, drives.Queries);
    }

    /// <summary>Proves WSL-to-Windows verification refuses a copied target whose drive is not admitted.</summary>
    [TestMethod]
    public async Task VerifyCopyAsyncWhenWindowsTargetDriveIsRejectedFailsProviderUnavailableAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileSystemPath destination = WindowsLocalOperationTestContext.ParsePath(root.CreateDirectory("destination"));
        WslFileSystemEntry source = WslOperationTestContext.Entry(
            WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\source.txt"), "wsl-source", DirectoryEntryKind.File);
        ScriptedWslFileSystem fileSystem = new();
        fileSystem.Set(source);
        ScriptedDriveTypes drives = new(root.Resolve("destination\\source.txt"), 0);
        WslToWindowsTransfer transfer = new(new WindowsLocalIoExecutionBoundary(), fileSystem, drives.Boundary());

        CopyVerificationOutcome outcome = await transfer.VerifyCopyAsync(
            WslOperationTestContext.Snapshot(source), destination, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, Assert.IsInstanceOfType<CopyVerificationFailed>(outcome).Failure);
    }

    /// <summary>Proves the WSL adapter offers no move capability for a WSL source and a Windows destination.</summary>
    [TestMethod]
    public async Task GetAtomicMoveCapabilityAsyncWhenDestinationIsWindowsLocalFailsProviderUnavailableAsync()
    {
        WslFileSystemEntry source = WslOperationTestContext.Entry(
            WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\source.txt"), "wsl-source", DirectoryEntryKind.File);
        WslFileOperationAdapter adapter = WslOperationTestContext.Adapter(new ScriptedWslFileSystem());

        AtomicMoveCapabilityOutcome outcome = await adapter.GetAtomicMoveCapabilityAsync(
            WslOperationTestContext.Snapshot(source), WslOperationTestContext.Local("C:\\destination"),
            CancellationToken.None);

        Assert.AreEqual(AtomicMoveCapabilityOutcome.Failed(FileOperationFailureKind.ProviderUnavailable), outcome);
    }

    /// <summary>Proves WSL verification refuses when the destination directory is no longer usable.</summary>
    [TestMethod]
    public async Task VerifyCopyAsyncWhenWslDestinationIsAbsentFailsVerificationAsync()
    {
        WslFileSystemEntry source = WslOperationTestContext.Entry(
            WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\source.txt"), "wsl-source", DirectoryEntryKind.File);
        ScriptedWslFileSystem fileSystem = new();
        fileSystem.Set(source);
        fileSystem.Set(WslOperationTestContext.Entry(
            WslOperationTestContext.Wsl(WslDestination + "\\source.txt"), "copied", DirectoryEntryKind.File));
        WslFileOperationAdapter adapter = WslOperationTestContext.Adapter(fileSystem);

        CopyVerificationOutcome outcome = await adapter.VerifyCopyAsync(
            WslOperationTestContext.Snapshot(source), WslOperationTestContext.Wsl(WslDestination), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Verification, Assert.IsInstanceOfType<CopyVerificationFailed>(outcome).Failure);
    }

    /// <summary>Proves recycle admission reports the parent drive refusal before inspecting the entry.</summary>
    [TestMethod]
    public void InspectWhenParentDriveIsRejectedReportsParentFailureBeforeEntry()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath absent = Assert.IsInstanceOfType<WindowsLocalPath>(
            WindowsLocalOperationTestContext.ParsePath(root.Resolve("absent.txt")));
        ScriptedDriveTypes drives = new(root.Path.CanonicalText, 0, DriveType.Ram);

        FileOperationFailureKind? failure = WindowsRecycleAdmission.Inspect(absent, drives.Boundary());

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, failure);
    }

    private static ScriptedWslFileSystem DestinationFileSystem()
    {
        ScriptedWslFileSystem fileSystem = new();
        fileSystem.Set(WslOperationTestContext.Entry(
            WslOperationTestContext.Wsl(WslDestination), "destination", DirectoryEntryKind.Directory));
        return fileSystem;
    }
}
