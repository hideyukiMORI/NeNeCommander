using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Checks held-entry content and delete-pending admission against real NTFS observations.</summary>
[TestClass]
public sealed class WindowsTransferHeldFileChangeTests
{
    /// <summary>Changing contents preserves the held object but must fail the stronger original-content check with an explanatory diagnostic.</summary>
    [TestMethod]
    public void RequireOriginalFileWhenLengthChangedRejectsWhileObjectCheckRemainsValid()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string path = root.WriteFile("file", "original");
        using SafeFileHandle handle = File.OpenHandle(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);
        using WindowsTransferHeldEntry entry = WindowsTransferHeldEntry.Take(WindowsTransferRecoveryTestFixture.Path(path), handle);
        FileIdentity baseline = entry.Baseline.Identity;
        RandomAccess.SetLength(handle, 0);

        IOException failure = Assert.ThrowsExactly<IOException>(entry.RequireOriginalFile);
        entry.RequireFileObject();

        Assert.Contains("changed", failure.Message);
        Assert.AreEqual(baseline, entry.Baseline.Identity);
        Assert.AreEqual(0L, RandomAccess.GetLength(handle));
        Assert.IsFalse(handle.IsClosed);
    }

    /// <summary>Delete-pending native state refuses before pathname observation and closes the transferred handle on failed admission.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void TakeWhenHandleIsDeletePendingRefusesItsStateAndClosesOwnership()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string path = root.WriteFile("file", "original");
        using SafeFileHandle handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, FileOptions.DeleteOnClose);
        File.Delete(path);
        Assert.AreNotEqual(0, WindowsFileIdentifier.ReadLocalFacts(handle).Standard.DeletePending);

        WindowsOverwriteRefusalException failure = Assert.ThrowsExactly<WindowsOverwriteRefusalException>(() =>
        {
            using WindowsTransferHeldEntry refused = WindowsTransferHeldEntry.Take(WindowsTransferRecoveryTestFixture.Path(path), handle);
        });

        Assert.AreSame(TransferOverwriteUnavailableReason.Unconfirmed, failure.Reason);
        Assert.IsTrue(handle.IsClosed);
    }

    /// <summary>A no-follow junction handle is refused for its own reparse attribute and never admitted as its target.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void TakeWhenHandleIsJunctionRefusesSpecialAttributesAndClosesOwnership()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string target = root.CreateDirectory("target");
        string sentinel = root.WriteFile("target/sentinel", "original");
        string targetIdentity = WindowsFileIdentifier.Describe(target);
        string link = root.CreateJunction("junction", "target");
        using SafeFileHandle handle = WindowsFileIdentifier.OpenForAttributes(link);
        Assert.AreNotEqual(0u, WindowsFileIdentifier.ReadLocalFacts(handle).Basic.Attributes & (uint)FileAttributes.ReparsePoint);

        WindowsOverwriteRefusalException failure = Assert.ThrowsExactly<WindowsOverwriteRefusalException>(() =>
        {
            using WindowsTransferHeldEntry refused = WindowsTransferHeldEntry.Take(WindowsTransferRecoveryTestFixture.Path(link), handle);
        });

        Assert.AreSame(TransferOverwriteUnavailableReason.SpecialAttributes, failure.Reason);
        Assert.IsTrue(handle.IsClosed);
        Assert.IsTrue(Directory.Exists(target));
        Assert.AreEqual(targetIdentity, WindowsFileIdentifier.Describe(target));
        Assert.AreEqual("original", File.ReadAllText(sentinel));
    }

}
