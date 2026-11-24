using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves absent link targets remain absent and never become cleanup-owned residue.</summary>
[TestClass]
public sealed class LiveWslDanglingLinkOwnershipTests
{
    /// <summary>Proves a dangling link is unlinked without creating or deleting a target.</summary>
    [TestMethod]
    public void CleanupWhenOwnedLinkTargetIsAbsentRemovesOnlyOwnedRun()
    {
        using TestOwnedTemporaryRoot host = LiveWslRootTestContext.CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(LiveWslRootTestContext.Open(host)).Root;
        _ = root.CreateDanglingFileSymbolicLink("source/link.txt", "source/absent.txt");
        Assert.IsFalse(root.Exists("source/absent.txt"));
        _ = Assert.IsInstanceOfType<LiveWslRootCheckAccepted>(root.VerifyForEffect());

        LiveWslRootCleanupOutcome outcome = root.Cleanup();

        _ = Assert.IsInstanceOfType<LiveWslRootCleanupCompleted>(outcome);
        Assert.IsEmpty(Directory.GetFileSystemEntries(host.Resolve(LiveWslRootTestContext.ConfiguredRelative(string.Empty))));
    }

    /// <summary>Proves a newly populated target remains foreign and stops cleanup before unlink.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void CleanupWhenAbsentTargetBecomesForeignFileRetainsBothEntries()
    {
        using TestOwnedTemporaryRoot host = LiveWslRootTestContext.CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(LiveWslRootTestContext.Open(host)).Root;
        WslPath link = root.CreateDanglingFileSymbolicLink("source/link.txt", "source/absent.txt");
        string target = host.Resolve(LiveWslRootTestContext.ConfiguredRelative(LiveWslRootTestContext.RunName + "/source/absent.txt"));
        File.WriteAllBytes(target, [7, 9]);

        LiveWslRootCleanupOutcome outcome = root.Cleanup();

        Assert.AreSame(LiveWslRootFailureKind.ForeignResidue,
            Assert.IsInstanceOfType<LiveWslRootCleanupRejected>(outcome).Failure);
        CollectionAssert.AreEqual(new byte[] { 7, 9 }, File.ReadAllBytes(target));
        Assert.IsTrue((File.GetAttributes(LiveWslRootTestContext.Resolve(host, link)) & FileAttributes.ReparsePoint) != 0);
    }

    /// <summary>Proves unlink cannot adopt a target created during that boundary.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void CleanupWhenUnlinkCreatesAbsentTargetRefusesRecursiveRemoval()
    {
        using TestOwnedTemporaryRoot host = LiveWslRootTestContext.CreateHost();
        string target = host.Resolve(LiveWslRootTestContext.ConfiguredRelative(LiveWslRootTestContext.RunName + "/source/absent.txt"));
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(LiveWslRootTestContext.Open(host, link =>
        {
            File.Delete(LiveWslRootTestContext.Resolve(host, link));
            File.WriteAllBytes(target, [2, 4]);
        })).Root;
        _ = root.CreateDanglingFileSymbolicLink("source/link.txt", "source/absent.txt");

        LiveWslRootCleanupOutcome outcome = root.Cleanup();

        Assert.AreSame(LiveWslRootFailureKind.IdentityChanged,
            Assert.IsInstanceOfType<LiveWslRootCleanupRejected>(outcome).Failure);
        CollectionAssert.AreEqual(new byte[] { 2, 4 }, File.ReadAllBytes(target));
        Assert.IsTrue(Directory.Exists(host.Resolve(LiveWslRootTestContext.ConfiguredRelative(LiveWslRootTestContext.RunName))));
    }

    /// <summary>Proves the absent-target declaration rejects an already owned entry before link creation.</summary>
    [TestMethod]
    public void CreateDanglingLinkWhenTargetExistsRefusesBeforeCreatingLink()
    {
        using TestOwnedTemporaryRoot host = LiveWslRootTestContext.CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(LiveWslRootTestContext.Open(host)).Root;
        _ = root.WriteFile("source/target.txt", [3]);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
            root.CreateDanglingFileSymbolicLink("source/link.txt", "source/target.txt"));

        Assert.IsFalse(root.Exists("source/link.txt"));
        CollectionAssert.AreEqual(new byte[] { 3 }, root.ReadFile("source/target.txt"));
        _ = Assert.IsInstanceOfType<LiveWslRootCleanupCompleted>(root.Cleanup());
    }
}
