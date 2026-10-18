using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Preserves LiveWslTestRootTests evidence for LiveWslRootOwnershipTests.</summary>
[TestClass]
public sealed class LiveWslRootOwnershipTests
{
    /// <summary>Proves replacing the ownership marker makes cleanup refuse the complete run tree.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void CleanupWhenOwnershipMarkerIsReplacedRefusesRecursiveDelete()
    {
        using TestOwnedTemporaryRoot host = LiveWslRootTestContext.CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(LiveWslRootTestContext.Open(host)).Root;
        host.ReplaceFilePreservingMetadata(LiveWslRootTestContext.ConfiguredRelative(LiveWslRootTestContext.RunName + "\\" + LiveWslRootTestContext.MarkerName), "Fake");

        LiveWslRootCleanupOutcome outcome = root.Cleanup();

        Assert.AreSame(
            LiveWslRootFailureKind.IdentityChanged,
            Assert.IsInstanceOfType<LiveWslRootCleanupRejected>(outcome).Failure);
        Assert.IsTrue(Directory.Exists(host.Resolve(LiveWslRootTestContext.ConfiguredRelative(LiveWslRootTestContext.RunName))));
    }

    /// <summary>Proves rewriting marker bytes under the same path makes cleanup refuse.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void CleanupWhenOwnershipMarkerBytesChangeRefusesRecursiveDelete()
    {
        using TestOwnedTemporaryRoot host = LiveWslRootTestContext.CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(LiveWslRootTestContext.Open(host)).Root;
        string marker = host.Resolve(LiveWslRootTestContext.ConfiguredRelative(LiveWslRootTestContext.RunName + "\\" + LiveWslRootTestContext.MarkerName));
        File.WriteAllText(marker, "Fake");

        LiveWslRootCleanupOutcome outcome = root.Cleanup();

        Assert.AreSame(
            LiveWslRootFailureKind.IdentityChanged,
            Assert.IsInstanceOfType<LiveWslRootCleanupRejected>(outcome).Failure);
        Assert.IsTrue(Directory.Exists(host.Resolve(LiveWslRootTestContext.ConfiguredRelative(LiveWslRootTestContext.RunName))));
    }

    /// <summary>
    /// Proves a byte-identical marker replacement is still refused, because the ADR-0049 entry
    /// identity changes even when the marker content, length, and last-write time do not.
    /// </summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-020")]
    public void CleanupWhenOwnershipMarkerIsReplacedByteIdenticallyRefusesRecursiveDelete()
    {
        using TestOwnedTemporaryRoot host = LiveWslRootTestContext.CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(LiveWslRootTestContext.Open(host)).Root;
        string marker = host.Resolve(LiveWslRootTestContext.ConfiguredRelative(LiveWslRootTestContext.RunName + "\\" + LiveWslRootTestContext.MarkerName));
        host.ReplaceFilePreservingMetadata(LiveWslRootTestContext.ConfiguredRelative(LiveWslRootTestContext.RunName + "\\" + LiveWslRootTestContext.MarkerName), "NeNe");

        LiveWslRootCleanupOutcome outcome = root.Cleanup();

        CollectionAssert.AreEqual("NeNe"u8.ToArray(), File.ReadAllBytes(marker));
        Assert.AreSame(
            LiveWslRootFailureKind.IdentityChanged,
            Assert.IsInstanceOfType<LiveWslRootCleanupRejected>(outcome).Failure);
        Assert.IsTrue(Directory.Exists(host.Resolve(LiveWslRootTestContext.ConfiguredRelative(LiveWslRootTestContext.RunName))));
    }

    /// <summary>Proves unregistered residue makes cleanup refuse the complete run tree.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void CleanupWhenForeignResidueAppearsRefusesRecursiveDelete()
    {
        using TestOwnedTemporaryRoot host = LiveWslRootTestContext.CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(LiveWslRootTestContext.Open(host)).Root;
        string foreign = host.WriteFile(LiveWslRootTestContext.ConfiguredRelative(LiveWslRootTestContext.RunName + "\\foreign.txt"), "foreign");

        LiveWslRootCleanupOutcome outcome = root.Cleanup();

        Assert.AreSame(
            LiveWslRootFailureKind.ForeignResidue,
            Assert.IsInstanceOfType<LiveWslRootCleanupRejected>(outcome).Failure);
        Assert.IsTrue(File.Exists(foreign));
    }

    /// <summary>Proves a foreign sibling beside the run child is retained with the complete run tree.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void CleanupWhenForeignRootSiblingAppearsRefusesRecursiveDelete()
    {
        using TestOwnedTemporaryRoot host = LiveWslRootTestContext.CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(LiveWslRootTestContext.Open(host)).Root;
        string foreign = host.WriteFile(LiveWslRootTestContext.ConfiguredRelative("foreign.txt"), "foreign");

        LiveWslRootCleanupOutcome outcome = root.Cleanup();

        Assert.AreSame(
            LiveWslRootFailureKind.ForeignResidue,
            Assert.IsInstanceOfType<LiveWslRootCleanupRejected>(outcome).Failure);
        Assert.IsTrue(File.Exists(foreign));
        Assert.IsTrue(Directory.Exists(host.Resolve(LiveWslRootTestContext.ConfiguredRelative(LiveWslRootTestContext.RunName))));
    }

    /// <summary>Proves replacing the run child is not mistaken for retained ownership.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void CleanupWhenRunChildIsReplacedRefusesReplacementAndOriginalTree()
    {
        using TestOwnedTemporaryRoot host = LiveWslRootTestContext.CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(LiveWslRootTestContext.Open(host)).Root;
        string run = host.Resolve(LiveWslRootTestContext.ConfiguredRelative(LiveWslRootTestContext.RunName));
        string parked = host.Resolve(LiveWslRootTestContext.ConfiguredRelative("parked"));
        Directory.Move(run, parked);
        _ = Directory.CreateDirectory(run);

        LiveWslRootCleanupOutcome outcome = root.Cleanup();

        Assert.AreSame(
            LiveWslRootFailureKind.IdentityChanged,
            Assert.IsInstanceOfType<LiveWslRootCleanupRejected>(outcome).Failure);
        Assert.IsTrue(Directory.Exists(run));
        Assert.IsTrue(Directory.Exists(parked));
    }
}
