using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves a live file cannot qualify its lease when a retained ancestor has already closed.</summary>
[TestClass]
public sealed class WindowsTransferAncestorLifetimeTests
{
    /// <summary>Both original and post-write observation must visit the retained parent instead of accepting only the file and a reopened root.</summary>
    [TestMethod]
    [DataRow("original")]
    [DataRow("object")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void RevalidateWhenRetainedParentWasClosedRefusesLiveLeaf(string observation)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath path = WindowsTransferRecoveryTestFixture.Path(root.WriteFile("file", "original"));
        using WindowsTransferPathLease lease = WindowsTransferPathLease.Open(path, FileAccess.Read, new WindowsDriveBoundary());
        lease.ParentHandle.Dispose();
        Assert.IsTrue(lease.ParentHandle.IsClosed);
        Assert.IsFalse(lease.Handle.IsClosed);

        _ = Assert.ThrowsExactly<ObjectDisposedException>(
            observation == "original" ? lease.Revalidate : lease.RevalidateObject);

        Assert.IsFalse(lease.Handle.IsClosed);
        Assert.AreEqual("original", File.ReadAllText(path.CanonicalText));
    }
}
