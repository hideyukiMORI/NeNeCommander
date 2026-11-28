using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves native acknowledgement and SafeHandle lifetime failures do not become removal success or a retry.</summary>
[TestClass]
public sealed class WindowsTransferArtifactRemovalProtocolTests
{
    /// <summary>Proves disposition failures do not invoke close and keep the original live object for the caller's uncertain receipt.</summary>
    [TestMethod]
    [DataRow("status")]
    [DataRow("exception")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public void RemoveWhenDispositionFailsDoesNotCloseOrRetry(string defect)
    {
        using WindowsTransferRemovalFixture fixture = WindowsTransferRemovalFixture.Create();
        int requests = 0;
        int closes = 0;
        WindowsTransferArtifactRemoval removal = new(_ =>
        {
            requests++;
            return defect == "status" ? unchecked((int)0xC0000001) : throw new IOException("Injected disposition failure.");
        }, _ =>
        {
            closes++;
            return false;
        });

        _ = Assert.ThrowsExactly<IOException>(() => removal.Remove(fixture.Handle));
        Assert.AreEqual(1, requests);
        Assert.AreEqual(0, closes);
        fixture.RequireOriginalContent();
        Assert.IsTrue(File.Exists(fixture.Path.CanonicalText));
    }

    /// <summary>Proves a successful disposition without acknowledged close throws, while the held bytes remain readable and later disposal may remove the link.</summary>
    [TestMethod]
    [DataRow("false")]
    [DataRow("exception")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public void RemoveWhenCloseIsUnconfirmedDoesNotClaimOrRetryRemoval(string defect)
    {
        using WindowsTransferRemovalFixture fixture = WindowsTransferRemovalFixture.Create();
        int closes = 0;
        WindowsTransferArtifactRemoval removal = new(WindowsTransferArtifactRemoval.Native.RequestDisposition, _ =>
        {
            closes++;
            return defect switch
            {
                "false" => false,
                _ => throw new IOException("Injected close failure before native close."),
            };
        });

        _ = Assert.ThrowsExactly<IOException>(() => removal.Remove(fixture.Handle));
        Assert.AreEqual(1, closes);
        fixture.RequireOriginalContent();
        fixture.Handle.Dispose();
        Assert.IsFalse(File.Exists(fixture.Path.CanonicalText));
    }

    /// <summary>Proves invalid or already closed handles never reach native disposition or close.</summary>
    [TestMethod]
    [DataRow("invalid")]
    [DataRow("closed")]
    public void RemoveWhenHandleIsUnavailableNeverEntersNativeCalls(string defect)
    {
        using SafeFileHandle handle = new(IntPtr.Zero, ownsHandle: false);
        int calls = 0;
        WindowsTransferArtifactRemoval removal = new(_ => { calls++; return 0; }, _ => { calls++; return true; });
        if (defect == "closed")
        {
            handle.Dispose();
            _ = Assert.ThrowsExactly<ObjectDisposedException>(() => removal.Remove(handle));
        }
        else
        {
            _ = Assert.ThrowsExactly<ArgumentException>(() => removal.Remove(handle));
        }
        Assert.AreEqual(0, calls);
    }
}
