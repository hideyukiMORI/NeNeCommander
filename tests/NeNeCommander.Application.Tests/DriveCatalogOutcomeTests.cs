using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Drives;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves the closed drive listing model and its single construction paths.</summary>
[TestClass]
public sealed class DriveCatalogOutcomeTests
{
    /// <summary>Proves a drive root with a label keeps the root, kind, and label.</summary>
    [TestMethod]
    public void CreateWhenRootHasLabelKeepsEveryComponent()
    {
        WindowsLocalPath root = Root("C:\\");

        DriveLocation drive = DriveLocation.Create(root, DriveKind.Fixed, "System");

        Assert.AreSame(root, drive.Root);
        Assert.AreSame(DriveKind.Fixed, drive.Kind);
        Assert.AreEqual("System", drive.VolumeLabel);
    }

    /// <summary>Proves an absent or empty reported label both mean the volume reported none.</summary>
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public void CreateWhenLabelIsAbsentOrEmptyRecordsAbsence(string? label)
    {
        DriveLocation drive = DriveLocation.Create(Root("D:\\"), DriveKind.Removable, label);

        Assert.IsNull(drive.VolumeLabel);
    }

    /// <summary>Proves a label made of spaces is reported text, not absence.</summary>
    [TestMethod]
    public void CreateWhenLabelIsWhitespaceKeepsItVerbatim()
    {
        DriveLocation drive = DriveLocation.Create(Root("D:\\"), DriveKind.Removable, " ");

        Assert.AreEqual(" ", drive.VolumeLabel);
    }

    /// <summary>Proves a path below a drive root is rejected.</summary>
    [TestMethod]
    public void CreateWhenPathIsBelowRootThrowsArgumentException()
    {
        WindowsLocalPath child = Assert.IsInstanceOfType<WindowsLocalPath>(Parse("C:\\Users"));

        ArgumentException failure = Assert.ThrowsExactly<ArgumentException>(
            () => DriveLocation.Create(child, DriveKind.Fixed, null));

        Assert.AreEqual("root", failure.ParamName);
    }

    /// <summary>Proves absent required components are rejected.</summary>
    [TestMethod]
    public void CreateWhenRequiredComponentIsNullThrowsArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => DriveLocation.Create(null!, DriveKind.Fixed, null));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => DriveLocation.Create(Root("C:\\"), null!, null));
    }

    /// <summary>Proves a successful listing owns an ordered copy and keeps its unshown-root count.</summary>
    [TestMethod]
    public void SucceededWhenRootsAreUniqueOwnsOrderedSnapshot()
    {
        DriveLocation first = DriveLocation.Create(Root("D:\\"), DriveKind.Removable, null);
        DriveLocation second = DriveLocation.Create(Root("C:\\"), DriveKind.Fixed, "System");
        List<DriveLocation> source = [first, second];

        DriveCatalogSucceeded succeeded = Assert.IsInstanceOfType<DriveCatalogSucceeded>(
            DriveCatalogOutcome.Succeeded(source, 2));
        source.Clear();

        Assert.HasCount(2, succeeded.Drives);
        Assert.AreSame(first, succeeded.Drives[0]);
        Assert.AreSame(second, succeeded.Drives[1]);
        Assert.AreEqual(2, succeeded.UnrepresentableRootCount);
    }

    /// <summary>Proves an empty listing with nothing unshown is a valid success.</summary>
    [TestMethod]
    public void SucceededWhenEmptyIsValid()
    {
        DriveCatalogSucceeded succeeded = Assert.IsInstanceOfType<DriveCatalogSucceeded>(
            DriveCatalogOutcome.Succeeded([], 0));

        Assert.IsEmpty(succeeded.Drives);
        Assert.AreEqual(0, succeeded.UnrepresentableRootCount);
    }

    /// <summary>Proves a root listed twice, in any letter case, is rejected.</summary>
    [TestMethod]
    public void SucceededWhenRootRepeatsThrowsArgumentException()
    {
        DriveLocation upper = DriveLocation.Create(Root("C:\\"), DriveKind.Fixed, null);
        DriveLocation lower = DriveLocation.Create(Root("c:\\"), DriveKind.Unknown, null);

        ArgumentException failure = Assert.ThrowsExactly<ArgumentException>(
            () => DriveCatalogOutcome.Succeeded([upper, lower], 0));

        Assert.AreEqual("drives", failure.ParamName);
    }

    /// <summary>Proves absent lists or entries and negative counts are rejected.</summary>
    [TestMethod]
    public void SucceededWhenInputIsInvalidThrows()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => DriveCatalogOutcome.Succeeded(null!, 0));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => DriveCatalogOutcome.Succeeded([null!], 0));
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => DriveCatalogOutcome.Succeeded([], -1));
    }

    /// <summary>Proves failure keeps its closed reason and cancellation carries nothing.</summary>
    [TestMethod]
    public void FailedAndCancelledWhenCreatedKeepTheirClosedShape()
    {
        DriveCatalogFailed failed = Assert.IsInstanceOfType<DriveCatalogFailed>(
            DriveCatalogOutcome.Failed(DriveCatalogFailureKind.AccessDenied));

        Assert.AreSame(DriveCatalogFailureKind.AccessDenied, failed.Failure);
        Assert.AreNotEqual(DriveCatalogFailureKind.ProviderUnavailable, DriveCatalogFailureKind.AccessDenied);
        _ = Assert.IsInstanceOfType<DriveCatalogCancelled>(DriveCatalogOutcome.Cancelled());
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => DriveCatalogOutcome.Failed(null!));
    }

    /// <summary>Proves the five drive kinds are distinct closed members.</summary>
    [TestMethod]
    public void DriveKindMembersAreDistinct()
    {
        DriveKind[] kinds = [DriveKind.Fixed, DriveKind.Removable, DriveKind.Network, DriveKind.Optical, DriveKind.Unknown];

        Assert.HasCount(5, new HashSet<DriveKind>(kinds));
    }

    private static WindowsLocalPath Root(string text)
    {
        return Assert.IsInstanceOfType<WindowsLocalPath>(Parse(text));
    }

    private static FileSystemPath Parse(string text)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path;
    }
}
