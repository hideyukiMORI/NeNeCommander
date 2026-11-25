using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves location, containment, links and fresh drive facts before empty-file creation.</summary>
[TestClass]
public sealed class WindowsEmptyFileAdmissionTests
{
    /// <summary>Proves an existing deeper directory cannot expand the operation beyond its direct child.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-003")]
    public void ExecuteWhenTargetIsNestedRefusesWithoutEffect()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("parent\\deeper");
        FileEntrySnapshot location = WindowsLocalOperationTestContext.LocalSnapshot(root.Resolve("parent"));
        FileSystemPath target = WindowsLocalOperationTestContext.ParsePath(root.Resolve("parent\\deeper\\new.txt"));

        ProviderStepOutcome outcome = WindowsLocalEmptyFileCreation.Execute(location, target, new WindowsDriveBoundary());

        Assert.AreSame(FileOperationFailureKind.Inspection, outcome.Failure);
        Assert.IsFalse(File.Exists(target.CanonicalText));
    }

    /// <summary>Proves a non-link location below a junction cannot redirect the creation.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-003")]
    public void ExecuteWhenAncestorIsJunctionRefusesWithoutEffect()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("real\\child");
        _ = root.CreateJunction("alias", "real");
        FileEntrySnapshot location = WindowsLocalOperationTestContext.LocalSnapshot(root.Resolve("alias\\child"));
        FileSystemPath target = WindowsLocalOperationTestContext.ParsePath(root.Resolve("alias\\child\\new.txt"));

        ProviderStepOutcome outcome = WindowsLocalEmptyFileCreation.Execute(location, target, new WindowsDriveBoundary());

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
        Assert.IsFalse(File.Exists(root.Resolve("real\\child\\new.txt")));
    }

    /// <summary>Proves a location replacement is refused even though a directory exists at the same path.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void ExecuteWhenLocationWasReplacedRefusesWithoutEffect()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string parent = root.CreateDirectory("parent");
        FileEntrySnapshot location = WindowsLocalOperationTestContext.LocalSnapshot(parent);
        Directory.Move(parent, root.Resolve("parked"));
        _ = root.CreateDirectory("parent");
        FileSystemPath target = WindowsLocalOperationTestContext.ParsePath(root.Resolve("parent\\new.txt"));

        ProviderStepOutcome outcome = WindowsLocalEmptyFileCreation.Execute(location, target, new WindowsDriveBoundary());

        Assert.AreSame(FileOperationFailureKind.IdentityChanged, outcome.Failure);
        Assert.IsFalse(File.Exists(target.CanonicalText));
        Assert.IsTrue(Directory.Exists(root.Resolve("parked")));
    }

    /// <summary>Proves drive admission is rechecked after preflight and before the native effect.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-024")]
    public void ExecuteWhenLocationDriveRemapsAtFinalCheckRefusesWithoutEffect()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string parent = root.CreateDirectory("parent");
        FileEntrySnapshot location = WindowsLocalOperationTestContext.LocalSnapshot(parent);
        FileSystemPath target = WindowsLocalOperationTestContext.ParsePath(root.Resolve("parent\\new.txt"));
        ScriptedDriveTypes drives = new(parent, 1);

        ProviderStepOutcome outcome = WindowsLocalEmptyFileCreation.Execute(location, target, drives.Boundary());

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
        Assert.IsFalse(File.Exists(target.CanonicalText));
    }
    /// <summary>Proves file creation uses the existing local-drive contract rather than transfer-only restrictions.</summary>
    [TestMethod]
    [DataRow(DriveType.Fixed)]
    [DataRow(DriveType.Removable)]
    [DataRow(DriveType.Ram)]
    [DataRow(DriveType.CDRom)]
    public void ExecuteWhenDriveIsAdmittedLocallyReachesExclusiveCreation(DriveType driveType)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string parent = root.CreateDirectory("parent");
        FileEntrySnapshot location = WindowsLocalOperationTestContext.LocalSnapshot(parent);
        FileSystemPath target = WindowsLocalOperationTestContext.ParsePath(root.Resolve("parent/new.txt"));

        ProviderStepOutcome outcome = WindowsLocalEmptyFileCreation.Execute(location, target, new WindowsDriveBoundary(_ => driveType));

        Assert.IsNull(outcome.Failure);
        Assert.AreEqual(0L, new FileInfo(target.CanonicalText).Length);
    }

    /// <summary>Proves unavailable and network drive facts cannot reach the creation step.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-024")]
    [DataRow(DriveType.Network)]
    [DataRow(DriveType.Unknown)]
    [DataRow(DriveType.NoRootDirectory)]
    public void ExecuteWhenDriveIsNotLocalRefusesBeforeCreation(DriveType driveType)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string parent = root.CreateDirectory("parent");
        FileEntrySnapshot location = WindowsLocalOperationTestContext.LocalSnapshot(parent);
        FileSystemPath target = WindowsLocalOperationTestContext.ParsePath(root.Resolve("parent/new.txt"));

        ProviderStepOutcome outcome = WindowsLocalEmptyFileCreation.Execute(location, target, new WindowsDriveBoundary(_ => driveType));

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
        Assert.IsFalse(File.Exists(target.CanonicalText));
    }

}
