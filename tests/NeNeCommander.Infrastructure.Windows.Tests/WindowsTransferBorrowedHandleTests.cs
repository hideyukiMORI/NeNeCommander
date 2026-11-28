using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves relative opens cannot use a borrowed handle after its owner has closed it.</summary>
[TestClass]
public sealed class WindowsTransferBorrowedHandleTests
{
    /// <summary>A closed retained parent is rejected by SafeHandle ownership before any native relative open can use its stale value.</summary>
    [TestMethod]
    [DataRow("directory")]
    [DataRow("file")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void OpenWhenBorrowedParentIsClosedRefusesItsNativeValue(string kind)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath anchor = WindowsTransferRecoveryTestFixture.Path(root.WriteFile("anchor", "original"));
        WindowsLocalPath child = WindowsTransferRecoveryTestFixture.Path(kind == "directory"
            ? root.CreateDirectory("child") : root.WriteFile("child", "foreign"));
        using WindowsTransferPathLease owner = WindowsTransferPathLease.Open(anchor, FileAccess.Read, new WindowsDriveBoundary());
        SafeFileHandle parent = owner.ParentHandle;
        WindowsLocalPath parentPath = owner.ParentPath;
        owner.Dispose();
        Assert.IsTrue(parent.IsClosed);

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            using SafeFileHandle refused = kind == "directory"
                ? WindowsTransferEntryOpen.OpenDirectory(parent, parentPath, child)
                : WindowsTransferEntryOpen.OpenFile(parent, parentPath, child, FileAccess.Read);
        });

        Assert.AreEqual("original", File.ReadAllText(anchor.CanonicalText));
        Assert.AreEqual(kind == "directory", Directory.Exists(child.CanonicalText));
        if (kind == "file")
        {
            Assert.AreEqual("foreign", File.ReadAllText(child.CanonicalText));
        }
    }
}
