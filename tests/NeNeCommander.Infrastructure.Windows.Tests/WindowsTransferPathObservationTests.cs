using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves native pathname observations cannot be relabelled as a different entry or a network volume.</summary>
[TestClass]
public sealed class WindowsTransferPathObservationTests
{
    /// <summary>Proves a complete native volume GUID root qualifies, with or without descendants.</summary>
    [TestMethod]
    [DataRow(@"\\?\Volume{12345678-1234-1234-1234-123456789abc}\")]
    [DataRow(@"\\?\Volume{12345678-1234-1234-1234-123456789abc}\child")]
    public void HasVolumeGuidWhenCompleteLocalRootIsObservedAcceptsIt(string path)
    {
        Assert.IsTrue(WindowsTransferPathObservation.HasVolumeGuid(path));
    }

    /// <summary>Proves malformed, short and network observations cannot qualify as a volume GUID root.</summary>
    [TestMethod]
    [DataRow(@"\\?\Volume{12345678-1234-1234-1234-123456789abc}")]
    [DataRow(@"\\?\Volume{12345678-1234-1234-1234-123456789abz}\")]
    [DataRow(@"\\?\Volume{12345678-1234-1234-1234-123456789abc}/")]
    [DataRow(@"\\?\UNC\server\share\file")]
    [DataRow(@"\\?\D:\file")]
    [DataRow("")]
    public void HasVolumeGuidWhenNativeTextIsMalformedRefusesIt(string path)
    {
        Assert.IsFalse(WindowsTransferPathObservation.HasVolumeGuid(path));
    }

    /// <summary>Proves a held file can qualify its exact path and cannot qualify a foreign occupant's path.</summary>
    [TestMethod]
    public void RequireWhenExpectedPathNamesAnotherFileRefusesIt()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath original = Parse(root.WriteFile("first.txt", "same"));
        WindowsLocalPath foreign = Parse(root.WriteFile("other.txt", "same"));
        using SafeFileHandle handle = WindowsFileIdentifier.OpenForAttributes(original.CanonicalText);
        WindowsTransferPathObservation.Require(handle, original);
        _ = Assert.Throws<IOException>(() => WindowsTransferPathObservation.Require(handle, foreign));
    }

    /// <summary>Proves a parent-relative primitive rejects a multicomponent or unrelated typed child before the native call.</summary>
    [TestMethod]
    [DataRow("nested")]
    [DataRow("other")]
    public void OpenDirectoryWhenChildIsNotExactDirectChildRefusesIt(string defect)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath parent = Parse(root.CreateDirectory("parent"));
        WindowsLocalPath wrong = Parse(root.Resolve(defect == "nested" ? "parent/nested/child" : "other"));
        using SafeFileHandle handle = WindowsFileIdentifier.OpenForAttributes(parent.CanonicalText);
        _ = Assert.Throws<ArgumentException>(() =>
        {
            using SafeFileHandle child = WindowsTransferEntryOpen.OpenDirectory(handle, parent, wrong);
        });
    }

    /// <summary>Proves the native root entry refuses a file-shaped path and a closed handle never qualifies a current path.</summary>
    [TestMethod]
    public void NativeBoundaryWhenRootOrHandleIsInvalidRefusesIt()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath path = Parse(root.WriteFile("file.txt", "owned"));
        _ = Assert.Throws<ArgumentException>(() =>
        {
            using SafeFileHandle handle = WindowsTransferEntryOpen.OpenRoot(path);
        });
        using SafeFileHandle invalid = new(IntPtr.Zero, false);
        _ = Assert.Throws<IOException>(() => WindowsTransferPathObservation.Require(invalid, path));
    }

    /// <summary>Proves a renamed held entry never qualifies its old pathname.</summary>
    [TestMethod]
    public void RequireWhenHeldEntryIsRenamedRejectsItsLastKnownName()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath original = Parse(root.WriteFile("first.txt", "owned"));
        WindowsLocalPath moved = Parse(root.Resolve("moved.txt"));
        using SafeFileHandle handle = WindowsFileIdentifier.OpenForAttributes(original.CanonicalText);
        File.Move(original.CanonicalText, moved.CanonicalText);
        _ = Assert.Throws<IOException>(() => WindowsTransferPathObservation.Require(handle, original));
        WindowsTransferPathObservation.Require(handle, moved);
    }

    /// <summary>Proves alternate casing is conservatively refused rather than silently reinterpreted.</summary>
    [TestMethod]
    public void RequireWhenCanonicalCasingDiffersRefusesAmbiguity()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath original = Parse(root.WriteFile("MixedCase.txt", "owned"));
        WindowsLocalPath alternative = Parse(root.Resolve("mixedcase.txt"));
        using SafeFileHandle handle = WindowsFileIdentifier.OpenForAttributes(original.CanonicalText);
        _ = Assert.Throws<IOException>(() => WindowsTransferPathObservation.Require(handle, alternative));
        WindowsTransferPathObservation.Require(handle, original);
    }

    private static WindowsLocalPath Parse(string text)
    {
        return Assert.IsInstanceOfType<WindowsLocalPath>(Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path);
    }
}
