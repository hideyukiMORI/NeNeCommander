using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves parent-relative native calls release borrowed SafeHandle references on success and failure.</summary>
[TestClass]
public sealed class WindowsTransferNativeOwnershipTests
{
    /// <summary>A completed directory open owns its result, while every borrowed parent reference is released before return.</summary>
    [TestMethod]
    [DataRow("existing")]
    [DataRow("missing")]
    public void OpenDirectoryWhenNativeCallReturnsReleasesBorrowedParentReference(string child)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath anchor = WindowsTransferRecoveryTestFixture.Path(root.WriteFile("anchor", "original"));
        _ = root.CreateDirectory("existing");
        WindowsLocalPath path = WindowsTransferRecoveryTestFixture.Path(root.Resolve(child));
        using WindowsTransferPathLease parent = WindowsTransferPathLease.Open(anchor, FileAccess.Read, new WindowsDriveBoundary());
        SafeFileHandle borrowed = parent.ParentHandle;
        if (child == "existing")
        {
            using SafeFileHandle result = WindowsTransferEntryOpen.OpenDirectory(borrowed, parent.ParentPath, path);
            Assert.IsFalse(result.IsInvalid);
            WindowsTransferPathObservation.Require(result, path);
        }
        else
        {
            _ = Assert.ThrowsExactly<IOException>(() =>
            {
                using SafeFileHandle result = WindowsTransferEntryOpen.OpenDirectory(borrowed, parent.ParentPath, path);
            });
        }
        parent.Dispose();
        Assert.IsTrue(borrowed.IsClosed);
        Assert.AreEqual("original", File.ReadAllText(anchor.CanonicalText));
    }

    /// <summary>Creation releases borrowed parent and security references even on a collision or a disposed descriptor.</summary>
    [TestMethod]
    [DataRow("created")]
    [DataRow("collision")]
    [DataRow("disposedSecurity")]
    public void CreateArtifactWhenNativeCallReturnsReleasesBothBorrowedOwners(string outcome)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath anchor = WindowsTransferRecoveryTestFixture.Path(root.WriteFile("anchor", "original"));
        WindowsLocalPath candidate = WindowsTransferRecoveryTestFixture.Path(root.Resolve("artifact"));
        using WindowsTransferPathLease parent = WindowsTransferPathLease.Open(anchor, FileAccess.Read, new WindowsDriveBoundary());
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        SafeFileHandle borrowed = parent.ParentHandle;
        if (outcome == "collision")
        {
            _ = root.WriteFile("artifact", "foreign");
        }
        if (outcome == "disposedSecurity")
        {
            security.Dispose();
        }
        void Create()
        {
            using SafeFileHandle artifact = WindowsTransferEntryOpen.CreateArtifact(parent, candidate, security);
            Assert.IsFalse(artifact.IsInvalid);
        }
        if (outcome == "collision")
        {
            _ = Assert.ThrowsExactly<IOException>(Create);
            Assert.AreEqual("foreign", File.ReadAllText(candidate.CanonicalText));
        }
        else if (outcome == "disposedSecurity")
        {
            _ = Assert.ThrowsExactly<ObjectDisposedException>(Create);
            Assert.IsFalse(File.Exists(candidate.CanonicalText));
        }
        else
        {
            Assert.IsFalse(security.IsInvalid);
            Create();
            Assert.AreEqual(0L, new FileInfo(candidate.CanonicalText).Length);
        }
        security.Dispose();
        parent.Dispose();
        Assert.IsTrue(security.IsClosed);
        Assert.IsTrue(borrowed.IsClosed);
        Assert.AreEqual("original", File.ReadAllText(anchor.CanonicalText));
    }
}
