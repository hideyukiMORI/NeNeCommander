using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the held-link native primitive without claiming higher-level cleanup admission or provider success.</summary>
[TestClass]
public sealed class WindowsTransferArtifactRemovalTests
{
    /// <summary>Proves acknowledged native removal consumes only the held link and repeated disposal cannot affect its new occupant.</summary>
    [TestMethod]
    public void RemoveWhenNativeCallsSucceedConsumesOwnedHandleAndPreservesNewOccupant()
    {
        using WindowsTransferRemovalFixture fixture = WindowsTransferRemovalFixture.Create();
        string identity = WindowsFileIdentifier.DescribeHandle(fixture.Handle);

        WindowsTransferArtifactRemoval.Native.Remove(fixture.Handle);
        Assert.IsTrue(fixture.Handle.IsClosed);
        Assert.IsFalse(File.Exists(fixture.Path.CanonicalText));
        _ = fixture.Root.WriteFile("artifact", "foreign");
        using SafeFileHandle replacement = File.OpenHandle(fixture.Path.CanonicalText, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        Assert.AreNotEqual(identity, WindowsFileIdentifier.DescribeHandle(replacement));
        fixture.Handle.Dispose();
        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => WindowsTransferArtifactRemoval.Native.Remove(fixture.Handle));
        Assert.AreEqual(7L, RandomAccess.GetLength(replacement));
        Assert.AreEqual("foreign", File.ReadAllText(fixture.Path.CanonicalText));
        fixture.Parent.Revalidate();
    }

    /// <summary>Proves an existing reader retains its original object and bytes after the owned link is removed.</summary>
    [TestMethod]
    public void RemoveWhenAnotherReaderExistsPreservesItsHandleAndOriginalBytes()
    {
        using WindowsTransferRemovalFixture fixture = WindowsTransferRemovalFixture.Create();
        using SafeFileHandle reader = File.OpenHandle(fixture.Path.CanonicalText, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        string identity = WindowsFileIdentifier.DescribeHandle(reader);

        WindowsTransferArtifactRemoval.Native.Remove(fixture.Handle);
        Assert.IsFalse(File.Exists(fixture.Path.CanonicalText));
        _ = fixture.Root.WriteFile("artifact", "foreign");
        Assert.AreEqual(identity, WindowsFileIdentifier.DescribeHandle(reader));
        byte[] bytes = new byte[3];
        Assert.AreEqual(3, RandomAccess.Read(reader, bytes, 0));
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, bytes);
        reader.Dispose();
        fixture.Handle.Dispose();
        Assert.AreEqual("foreign", File.ReadAllText(fixture.Path.CanonicalText));
    }

    /// <summary>Proves a late alias remains and only the held original link is removed; this is not an alias-freeze guarantee.</summary>
    [TestMethod]
    public void RemoveWhenLateAliasExistsLeavesAliasBytesAndNewNameOccupant()
    {
        using WindowsTransferRemovalFixture fixture = WindowsTransferRemovalFixture.Create();
        string alias = fixture.Root.CreateHardLink("alias", "artifact");

        WindowsTransferArtifactRemoval.Native.Remove(fixture.Handle);
        Assert.IsFalse(File.Exists(fixture.Path.CanonicalText));
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, File.ReadAllBytes(alias));
        _ = fixture.Root.WriteFile("artifact", "foreign");
        fixture.Handle.Dispose();
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, File.ReadAllBytes(alias));
        Assert.AreEqual("foreign", File.ReadAllText(fixture.Path.CanonicalText));
    }

    /// <summary>Proves readonly native refusal does not close the handle or enable any alternate deletion path.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public void RemoveWhenNativeDispositionRefusesReadonlyPreservesHandleAndBytes()
    {
        using WindowsTransferRemovalFixture fixture = WindowsTransferRemovalFixture.Create();
        File.SetAttributes(fixture.Path.CanonicalText, FileAttributes.ReadOnly);
        try
        {
            _ = Assert.ThrowsExactly<IOException>(() => WindowsTransferArtifactRemoval.Native.Remove(fixture.Handle));
            fixture.RequireOriginalContent();
            Assert.IsTrue(File.Exists(fixture.Path.CanonicalText));
        }
        finally
        {
            File.SetAttributes(fixture.Path.CanonicalText, FileAttributes.Normal);
        }
    }

    /// <summary>Proves a dispose inside the acknowledged native close cannot release a reused handle value through SafeHandle again.</summary>
    [TestMethod]
    public void RemoveWhenCloseReentersDisposeKeepsReferenceUntilInvalidation()
    {
        using WindowsTransferRemovalFixture fixture = WindowsTransferRemovalFixture.Create();
        SafeFileHandle? replacement = null;
        WindowsTransferArtifactRemoval removal = new(WindowsTransferArtifactRemoval.Native.RequestDisposition, native =>
        {
            fixture.Handle.Dispose();
            Assert.IsFalse(fixture.Handle.IsClosed);
            bool closed = WindowsTransferArtifactRemoval.Native.Close(native);
            Assert.IsTrue(closed);
            _ = fixture.Root.WriteFile("artifact", "foreign");
            replacement = File.OpenHandle(fixture.Path.CanonicalText, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            return closed;
        });
        try
        {
            removal.Remove(fixture.Handle);
            Assert.IsTrue(fixture.Handle.IsClosed);
            Assert.IsNotNull(replacement);
            fixture.Handle.Dispose();
            Assert.AreEqual(7L, RandomAccess.GetLength(replacement));
        }
        finally
        {
            replacement?.Dispose();
        }
    }
}
