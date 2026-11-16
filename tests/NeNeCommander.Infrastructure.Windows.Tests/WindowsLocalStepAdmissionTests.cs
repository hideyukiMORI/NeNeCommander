using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>
/// Proves each local mutation step rechecks the drive boundary of every path it is about to touch
/// immediately before the effect, so one rejected path alone stops the step.
/// </summary>
[TestClass]
public sealed class WindowsLocalStepAdmissionTests
{
    /// <summary>Proves a move stops when only the derived target path is no longer an admitted drive.</summary>
    [TestMethod]
    public void MoveWhenOnlyTargetDriveIsRejectedFailsWithoutMoving()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string sourceText = root.WriteFile("source.txt", "source");
        FileSystemPath destination = WindowsLocalOperationTestContext.ParsePath(root.CreateDirectory("destination"));
        ScriptedDriveTypes drives = new(root.Resolve("destination\\source.txt"), 0);
        WindowsLocalOperationSteps steps = new(new ScriptedWslFileSystem(), drives.Boundary());

        ProviderStepOutcome outcome = steps.Move(WindowsLocalOperationTestContext.LocalSnapshot(sourceText), destination);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
        Assert.IsTrue(File.Exists(sourceText));
        Assert.IsFalse(File.Exists(root.Resolve("destination\\source.txt")));
    }

    /// <summary>Proves a rename stops when only the source drive is remapped after revalidation.</summary>
    [TestMethod]
    public void RenameWhenSourceDriveIsRemappedAfterRevalidationFailsWithoutRenaming()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string sourceText = root.WriteFile("before.txt", "source");
        FileSystemPath target = WindowsLocalOperationTestContext.ParsePath(root.Resolve("after.txt"));
        ScriptedDriveTypes drives = new(sourceText, 1);
        WindowsLocalOperationSteps steps = new(new ScriptedWslFileSystem(), drives.Boundary());

        ProviderStepOutcome outcome = steps.Rename(WindowsLocalOperationTestContext.LocalSnapshot(sourceText), target);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
        Assert.AreEqual(2, drives.Queries);
        Assert.IsTrue(File.Exists(sourceText));
        Assert.IsFalse(File.Exists(root.Resolve("after.txt")));
    }

    /// <summary>Proves directory creation stops when only the location drive is remapped after revalidation.</summary>
    [TestMethod]
    public void CreateDirectoryWhenLocationDriveIsRemappedAfterRevalidationFailsWithoutCreating()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string locationText = root.CreateDirectory("location");
        FileSystemPath target = WindowsLocalOperationTestContext.ParsePath(root.Resolve("location\\created"));
        ScriptedDriveTypes drives = new(locationText, 1);
        WindowsLocalOperationSteps steps = new(new ScriptedWslFileSystem(), drives.Boundary());

        ProviderStepOutcome outcome = steps.CreateDirectory(WindowsLocalOperationTestContext.LocalSnapshot(locationText), target);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
        Assert.AreEqual(2, drives.Queries);
        Assert.IsFalse(Directory.Exists(root.Resolve("location\\created")));
    }

    /// <summary>Proves the local step deletes only in permanent mode and refuses an unverified recycle request.</summary>
    [TestMethod]
    public void DeleteWhenModeIsRecycleFailsWithoutDeleting()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string sourceText = root.WriteFile("kept.txt", "source");
        WindowsLocalOperationSteps steps = new(new ScriptedWslFileSystem(), new WindowsDriveBoundary(_ => DriveType.Fixed));

        ProviderStepOutcome outcome = steps.Delete(WindowsLocalOperationTestContext.LocalSnapshot(sourceText), DeletionExecutionMode.Recycle);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
        Assert.IsTrue(File.Exists(sourceText));
    }
}
