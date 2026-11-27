using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves a rejected or disposed lease cannot retry admission through later native observations.</summary>
[TestClass]
public sealed class WindowsTransferAdmissionOrderTests
{
    /// <summary>The first rejected drive classification stops traversal even if a later query would admit the drive.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void OpenWhenFirstDriveObservationRejectsDoesNotRetryAdmission()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath path = WindowsTransferRecoveryTestFixture.Path(root.WriteFile("file", "original"));
        int queries = 0;
        WindowsDriveBoundary boundary = new(_ => ++queries == 1 ? DriveType.Network : DriveType.Fixed);

        _ = Assert.ThrowsExactly<WindowsDriveBoundaryRejectedException>(() =>
        {
            using WindowsTransferPathLease refused = WindowsTransferPathLease.Open(path, FileAccess.Read, boundary);
        });

        Assert.AreEqual(1, queries);
        Assert.AreEqual("original", File.ReadAllText(path.CanonicalText));
    }

    /// <summary>Disposed lease validation rejects at its lifetime boundary before asking a live drive adapter anything.</summary>
    [TestMethod]
    [DataRow("original")]
    [DataRow("object")]
    public void RevalidateWhenDisposedDoesNotQueryDriveOrNativeHandle(string observation)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath path = WindowsTransferRecoveryTestFixture.Path(root.WriteFile("file", "original"));
        int queries = 0;
        WindowsDriveBoundary boundary = new(_ =>
        {
            queries++;
            return DriveType.Fixed;
        });
        using WindowsTransferPathLease lease = WindowsTransferPathLease.Open(path, FileAccess.Read, boundary);
        Assert.IsGreaterThan(0, queries);
        lease.Dispose();
        queries = 0;

        ObjectDisposedException failure = Assert.ThrowsExactly<ObjectDisposedException>(
            observation == "original" ? lease.Revalidate : lease.RevalidateObject);

        Assert.AreEqual(typeof(WindowsTransferPathLease).FullName, failure.ObjectName);
        Assert.AreEqual(0, queries);
        Assert.IsTrue(lease.Handle.IsClosed);
        Assert.IsTrue(lease.ParentHandle.IsClosed);
        Assert.AreEqual("original", File.ReadAllText(path.CanonicalText));
    }
}
