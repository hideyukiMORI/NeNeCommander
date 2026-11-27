using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves a held entry owns failed opens and never turns its saved pathname or file kind into current evidence.</summary>
[TestClass]
public sealed class WindowsTransferHeldEntryOwnershipTests
{
    /// <summary>A handle describing another object cannot be accepted under a supplied path; failed transfer of ownership closes it.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void TakeWhenPathDescribesAnotherObjectRejectsAndClosesThePassedHandle()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string source = root.WriteFile("source", "source");
        string foreign = root.WriteFile("foreign", "foreign");
        using SafeFileHandle handle = File.OpenHandle(source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

        _ = Assert.ThrowsExactly<IOException>(() =>
        {
            using WindowsTransferHeldEntry rejected = WindowsTransferHeldEntry.Take(WindowsTransferRecoveryTestFixture.Path(foreign), handle);
        });

        Assert.IsTrue(handle.IsClosed);
        Assert.AreEqual("source", File.ReadAllText(source));
        Assert.AreEqual("foreign", File.ReadAllText(foreign));
    }

    /// <summary>A regular file cannot be borrowed as an ancestor directory even when its object and path match.</summary>
    [TestMethod]
    public void RequireDirectoryWhenHeldEntryIsAFileRefusesTheAncestorRole()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string path = root.WriteFile("file", "original");
        using SafeFileHandle handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using WindowsTransferHeldEntry entry = WindowsTransferHeldEntry.Take(WindowsTransferRecoveryTestFixture.Path(path), handle);

        _ = Assert.ThrowsExactly<IOException>(entry.RequireDirectory);

        Assert.IsFalse(handle.IsClosed);
        Assert.AreEqual("original", File.ReadAllText(path));
    }

    /// <summary>Both original-file and post-content identity checks reject a held file whose old name now belongs to another object.</summary>
    [TestMethod]
    [DataRow("original")]
    [DataRow("postContent")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void RevalidateWhenOriginalNameIsReoccupiedRefusesBothIdentityModes(string mode)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string path = root.WriteFile("file", "original");
        using SafeFileHandle handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using WindowsTransferHeldEntry entry = WindowsTransferHeldEntry.Take(WindowsTransferRecoveryTestFixture.Path(path), handle);
        File.Move(path, root.Resolve("held"));
        _ = root.WriteFile("file", "foreign");

        _ = Assert.ThrowsExactly<IOException>(mode == "original" ? entry.RequireOriginalFile : entry.RequireFileObject);
        Assert.AreEqual("original", File.ReadAllText(root.Resolve("held")));
        Assert.AreEqual("foreign", File.ReadAllText(path));
        Assert.IsFalse(handle.IsClosed);
    }
}
