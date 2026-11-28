using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Checks every owned handle, including the drive root that file-open and rename probes cannot detect.</summary>
[TestClass]
public sealed class WindowsTransferLeaseReleaseTests
{
    /// <summary>Disposal closes the entire retained chain exactly once and leaves file contents untouched.</summary>
    [TestMethod]
    public void DisposeWhenLeaseOwnsRootAndAncestorsClosesEveryRetainedHandle()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("parent");
        WindowsLocalPath path = WindowsTransferRecoveryTestFixture.Path(root.WriteFile("parent/file", "original"));
        using WindowsTransferPathLease lease = WindowsTransferPathLease.Open(path, FileAccess.Read, new WindowsDriveBoundary());
        FieldInfo field = typeof(WindowsTransferPathLease).GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new AssertFailedException("Retained owner inspection is unavailable.");
        List<WindowsTransferHeldEntry> entries = Assert.IsInstanceOfType<List<WindowsTransferHeldEntry>>(field.GetValue(lease));
        Assert.IsGreaterThan(2, entries.Count);
        Assert.IsNull(entries[0].Path.Parent);
        SafeFileHandle[] handles = [.. entries.ConvertAll(entry => entry.Handle)];
        foreach (SafeFileHandle handle in handles)
        {
            Assert.IsFalse(handle.IsClosed);
        }

        lease.Dispose();
        lease.Dispose();

        foreach (SafeFileHandle handle in handles)
        {
            Assert.IsTrue(handle.IsClosed);
        }
        Assert.AreEqual("original", File.ReadAllText(path.CanonicalText));
    }
}
