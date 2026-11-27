using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves native observation shapes refuse unsupported attributes and incomplete file facts.</summary>
[TestClass]
public sealed class WindowsOverwriteFileAdmissionTests
{
    /// <summary>Proves ordinary metadata is admitted without treating other flags as harmless combinations.</summary>
    [TestMethod]
    [DataRow(FileAttributes.Normal)]
    [DataRow(FileAttributes.Archive)]
    public void RequireShapeWhenFileHasOnlyAdmittedAttributesAcceptsIt(FileAttributes attributes)
    {
        WindowsOverwriteFileAdmission.RequireShape(Facts(attributes, new WindowsLocalStandardFacts(0, 1, 0, 0), "NTFS"));
    }

    /// <summary>Proves each excluded attribute, including unknown or empty flag shapes, refuses.</summary>
    [TestMethod]
    [DataRow((FileAttributes)0)]
    [DataRow(FileAttributes.Hidden)]
    [DataRow(FileAttributes.ReadOnly)]
    [DataRow(FileAttributes.System)]
    [DataRow(FileAttributes.Directory)]
    [DataRow(FileAttributes.ReparsePoint)]
    [DataRow(FileAttributes.SparseFile)]
    [DataRow(FileAttributes.Compressed)]
    [DataRow(FileAttributes.Encrypted)]
    [DataRow(FileAttributes.Offline)]
    [DataRow(FileAttributes.Device)]
    [DataRow(FileAttributes.Normal | FileAttributes.Archive)]
    [DataRow(FileAttributes.Archive | FileAttributes.Hidden)]
    [DataRow((FileAttributes)int.MinValue)]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void RequireShapeWhenFileAttributesAreUnsupportedRefusesThem(FileAttributes attributes)
    {
        _ = Assert.ThrowsExactly<WindowsOverwriteRefusalException>(() => WindowsOverwriteFileAdmission.RequireShape(
            Facts(attributes, new WindowsLocalStandardFacts(1, 1, 0, 0), "NTFS")));
    }

    /// <summary>Proves unknown or contradictory native facts never grant a file capability.</summary>
    [TestMethod]
    [DataRow(-1L, 1u, (byte)0, (byte)0)]
    [DataRow(0L, 0u, (byte)0, (byte)0)]
    [DataRow(0L, 2u, (byte)0, (byte)0)]
    [DataRow(0L, 1u, (byte)1, (byte)0)]
    [DataRow(0L, 1u, (byte)2, (byte)0)]
    [DataRow(0L, 1u, (byte)0, (byte)1)]
    [DataRow(0L, 1u, (byte)0, (byte)2)]
    public void RequireShapeWhenFileFactsAreUnsupportedRefusesThem(long length, uint links, byte deleted, byte directory)
    {
        _ = Assert.ThrowsExactly<WindowsOverwriteRefusalException>(() => WindowsOverwriteFileAdmission.RequireShape(
            Facts(FileAttributes.Archive, new WindowsLocalStandardFacts(length, links, deleted, directory), "NTFS")));
    }

    /// <summary>Proves filesystem labels require the actual exact admitted name.</summary>
    [TestMethod]
    [DataRow("ReFS")]
    [DataRow("FAT32")]
    [DataRow("9P")]
    [DataRow("ntfs")]
    public void RequireShapeWhenFileSystemIsUnsupportedRefusesIt(string fileSystem)
    {
        _ = Assert.ThrowsExactly<WindowsOverwriteRefusalException>(() => WindowsOverwriteFileAdmission.RequireShape(
            Facts(FileAttributes.Normal, new WindowsLocalStandardFacts(0, 1, 0, 0), fileSystem)));
    }

    /// <summary>Proves required observations cannot be absent.</summary>
    [TestMethod]
    public void RequireShapeAndObserveWhenInputsAreAbsentRejectThem()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => WindowsOverwriteFileAdmission.RequireShape(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => WindowsOverwriteFileAdmission.Observe(null!));
    }

    private static WindowsLocalHandleFacts Facts(FileAttributes attributes, WindowsLocalStandardFacts standard, string fileSystem)
    {
        return new WindowsLocalHandleFacts(FileObjectIdentity.Create("owned-object"), new WindowsLocalBasicFacts(0, 0, 0, (uint)attributes), standard, fileSystem);
    }
}
