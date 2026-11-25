using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves held-object observations preserve the canonical identity while remaining separate from pathname authority.</summary>
[TestClass]
public sealed class WindowsLocalHeldIdentityTests
{
    /// <summary>Proves native file and directory observations compose exactly the existing windows-v2 token.</summary>
    [TestMethod]
    [DataRow("file")]
    [DataRow("empty")]
    [DataRow("directory")]
    public void CaptureHeldWhenEntryIsStableMatchesExistingIdentity(string kind)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string text = kind == "directory" ? root.CreateDirectory("entry") : root.WriteFile("entry", kind == "empty" ? string.Empty : "content");
        WindowsLocalPath path = Parse(text);
        using SafeFileHandle handle = WindowsFileIdentifier.OpenForAttributes(text);

        WindowsLocalHandleFacts facts = WindowsFileIdentifier.ReadLocalFacts(handle);
        VerifiedEntrySnapshot captured = WindowsLocalEntryIdentity.CaptureHeld(path, facts);
        FileSystemInfo entry = Assert.IsInstanceOfType<FileSystemInfo>(WindowsLocalEntryIdentity.Find(path));

        Assert.AreEqual(WindowsLocalEntryIdentity.Describe(entry), captured.Identity);
        Assert.AreEqual(WindowsFileIdentifier.Describe(text), captured.ObjectIdentity.Value);
        Assert.AreEqual("NTFS", facts.FileSystemName);
        Assert.AreEqual(kind == "file" ? 7L : 0L, facts.Standard.EndOfFile);
        Assert.AreEqual(kind == "directory" ? (byte)1 : (byte)0, facts.Standard.Directory);
        Assert.AreEqual((byte)0, facts.Standard.DeletePending);
        Assert.AreEqual(1u, facts.Standard.NumberOfLinks);
        Assert.IsGreaterThan(0L, facts.Basic.CreationFileTimeUtc);
        Assert.IsGreaterThan(0L, facts.Basic.LastWriteFileTimeUtc);
        Assert.IsGreaterThan(0L, facts.Basic.ChangeFileTimeUtc);
    }

    /// <summary>Proves replacing the old name does not redirect a held-object observation to the new occupant.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void ReadLocalFactsWhenOriginalNameIsReplacedStillReportsHeldObject()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string text = root.WriteFile("entry", "before!");
        using SafeFileHandle handle = WindowsFileIdentifier.OpenForAttributes(text);
        WindowsLocalHandleFacts before = WindowsFileIdentifier.ReadLocalFacts(handle);

        root.ReplaceFilePreservingMetadata("entry", "after!!");
        WindowsLocalHandleFacts held = WindowsFileIdentifier.ReadLocalFacts(handle);

        Assert.AreEqual(before.ObjectIdentity, held.ObjectIdentity);
        Assert.AreEqual(WindowsFileIdentifier.Describe(root.Resolve("entry.original")), held.ObjectIdentity.Value);
        Assert.AreNotEqual(WindowsFileIdentifier.Describe(text), held.ObjectIdentity.Value);
        Assert.AreEqual(before.Standard.EndOfFile, held.Standard.EndOfFile);
        Assert.AreEqual(before.Basic.CreationFileTimeUtc, held.Basic.CreationFileTimeUtc);
    }

    /// <summary>Proves no-follow observations report a junction's own identity and reparse attribute.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void ReadLocalFactsWhenEntryIsJunctionDoesNotObserveItsTarget()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string target = root.CreateDirectory("target");
        string link = root.CreateJunction("junction", "target");
        using SafeFileHandle handle = WindowsFileIdentifier.OpenForAttributes(link);

        WindowsLocalHandleFacts facts = WindowsFileIdentifier.ReadLocalFacts(handle);

        Assert.AreEqual(WindowsFileIdentifier.Describe(link), facts.ObjectIdentity.Value);
        Assert.AreNotEqual(WindowsFileIdentifier.Describe(target), facts.ObjectIdentity.Value);
        Assert.AreNotEqual(0u, facts.Basic.Attributes & (uint)FileAttributes.ReparsePoint);
    }

    /// <summary>Proves invalid native metadata cannot become a canonical full identity.</summary>
    [TestMethod]
    [DataRow("length")]
    [DataRow("directoryByte")]
    [DataRow("pendingByte")]
    [DataRow("fileAttribute")]
    [DataRow("directoryAttribute")]
    [DataRow("negativeTime")]
    [DataRow("excessiveTime")]
    [DataRow("negativeWriteTime")]
    [DataRow("excessiveWriteTime")]
    public void CaptureHeldWhenNativeMetadataIsUnrepresentableRejectsIt(string defect)
    {
        WindowsLocalHandleFacts facts = InvalidFacts(defect);

        _ = Assert.ThrowsExactly<IOException>(() => WindowsLocalEntryIdentity.CaptureHeld(Parse("C:\\entry"), facts));
    }

    /// <summary>Proves absent and invalid handles cannot create observations and required facts cannot be omitted.</summary>
    [TestMethod]
    public void HeldIdentityBoundariesWhenRequiredInputIsAbsentRejectIt()
    {
        WindowsLocalBasicFacts basic = new(0, 0, 0, 0);
        WindowsLocalStandardFacts standard = new(0, 1, 0, 0);
        FileObjectIdentity identity = FileObjectIdentity.Create("object");
        WindowsLocalHandleFacts facts = new(identity, basic, standard, "NTFS");
        using SafeFileHandle invalid = new(IntPtr.Zero, false);

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => WindowsFileIdentifier.ReadLocalFacts(null!));
        _ = Assert.ThrowsExactly<IOException>(() => WindowsFileIdentifier.ReadLocalFacts(invalid));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => WindowsFileIdentifier.OpenForAttributes(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => WindowsLocalEntryIdentity.CaptureHeld(null!, facts));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => WindowsLocalEntryIdentity.CaptureHeld(Parse("C:\\entry"), null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WindowsLocalHandleFacts(null!, basic, standard, "NTFS"));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WindowsLocalHandleFacts(identity, null!, standard, "NTFS"));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WindowsLocalHandleFacts(identity, basic, null!, "NTFS"));
        _ = Assert.ThrowsExactly<ArgumentException>(() => new WindowsLocalHandleFacts(identity, basic, standard, string.Empty));
    }

    private static WindowsLocalHandleFacts InvalidFacts(string defect)
    {
        WindowsLocalStandardFacts standard = defect switch
        {
            "length" => new(-1, 1, 0, 0),
            "directoryByte" => new(0, 1, 0, 2),
            "pendingByte" => new(0, 1, 2, 0),
            "directoryAttribute" => new(0, 1, 0, 1),
            _ => new(0, 1, 0, 0),
        };
        WindowsLocalBasicFacts basic = defect switch
        {
            "fileAttribute" => new(0, 0, 0, 16),
            "negativeTime" => new(-1, 0, 0, 0),
            "excessiveTime" => new(long.MaxValue, 0, 0, 0),
            "negativeWriteTime" => new(0, -1, 0, 0),
            "excessiveWriteTime" => new(0, long.MaxValue, 0, 0),
            _ => new(0, 0, 0, 0),
        };
        return new WindowsLocalHandleFacts(FileObjectIdentity.Create("object"), basic, standard, "NTFS");
    }

    private static WindowsLocalPath Parse(string text)
    {
        return Assert.IsInstanceOfType<WindowsLocalPath>(Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path);
    }
}
