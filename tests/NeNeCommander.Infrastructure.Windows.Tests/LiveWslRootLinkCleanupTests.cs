using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Preserves LiveWslTestRootTests evidence for LiveWslRootLinkCleanupTests.</summary>
[TestClass]
public sealed class LiveWslRootLinkCleanupTests
{
    /// <summary>Proves an owned link is unlinked before the run child is recursively removed.</summary>
    [TestMethod]
    public void CleanupWhenOwnedLinkTargetsOwnedSentinelUnlinksEntryAndRetainsConfiguredRoot()
    {
        using TestOwnedTemporaryRoot host = LiveWslRootTestContext.CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(LiveWslRootTestContext.Open(host)).Root;
        _ = root.WriteFile("sentinel.txt", [1, 2, 3]);
        _ = root.CreateFileSymbolicLink("source/link", "sentinel.txt");

        LiveWslRootCleanupOutcome outcome = root.Cleanup();

        _ = Assert.IsInstanceOfType<LiveWslRootCleanupCompleted>(outcome);
        Assert.IsTrue(Directory.Exists(host.Resolve(LiveWslRootTestContext.ConfiguredRelative(string.Empty))));
        Assert.IsEmpty(Directory.GetFileSystemEntries(host.Resolve(LiveWslRootTestContext.ConfiguredRelative(string.Empty))));
    }

    /// <summary>
    /// Proves a link entry that survives its unlink stops cleanup before the recursive removal,
    /// so a tree containing a link is never partially deleted.
    /// </summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void CleanupWhenLinkSurvivesUnlinkRefusesRecursiveDelete()
    {
        using TestOwnedTemporaryRoot host = LiveWslRootTestContext.CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(
            LiveWslRootTestContext.Open(host, static _ => { })).Root;
        _ = root.WriteFile("sentinel.txt", [1, 2, 3]);
        _ = root.CreateFileSymbolicLink("source/link", "sentinel.txt");

        LiveWslRootCleanupOutcome outcome = root.Cleanup();

        Assert.AreSame(
            LiveWslRootFailureKind.CleanupFailed,
            Assert.IsInstanceOfType<LiveWslRootCleanupRejected>(outcome).Failure);
        Assert.IsTrue(Directory.Exists(host.Resolve(LiveWslRootTestContext.ConfiguredRelative(LiveWslRootTestContext.RunName))));
        Assert.IsTrue(File.Exists(host.Resolve(LiveWslRootTestContext.ConfiguredRelative(LiveWslRootTestContext.RunName + "\\sentinel.txt"))));
    }

    /// <summary>Proves an unlink that removes the link target refuses instead of continuing.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void CleanupWhenUnlinkRemovesLinkTargetRefusesRecursiveDelete()
    {
        using TestOwnedTemporaryRoot host = LiveWslRootTestContext.CreateHost();
        LiveWslRootOpenOutcome opened = LiveWslRootTestContext.Open(host, link =>
        {
            File.Delete(LiveWslRootTestContext.Resolve(host, link));
            File.Delete(host.Resolve(LiveWslRootTestContext.ConfiguredRelative(LiveWslRootTestContext.RunName + "\\sentinel.txt")));
        });
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(opened).Root;
        _ = root.WriteFile("sentinel.txt", [1, 2, 3]);
        _ = root.CreateFileSymbolicLink("source/link", "sentinel.txt");

        LiveWslRootCleanupOutcome outcome = root.Cleanup();

        Assert.AreSame(
            LiveWslRootFailureKind.IdentityChanged,
            Assert.IsInstanceOfType<LiveWslRootCleanupRejected>(outcome).Failure);
        Assert.IsTrue(Directory.Exists(host.Resolve(LiveWslRootTestContext.ConfiguredRelative(LiveWslRootTestContext.RunName))));
    }

    /// <summary>Proves any reparse entry left in the run child refuses the recursive removal.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void CleanupWhenReparseEntryRemainsRefusesRecursiveDelete()
    {
        using TestOwnedTemporaryRoot host = LiveWslRootTestContext.CreateHost();
        LiveWslRootOpenOutcome opened = LiveWslRootTestContext.Open(host, link =>
        {
            File.Delete(LiveWslRootTestContext.Resolve(host, link));
            _ = File.CreateSymbolicLink(
                host.Resolve(LiveWslRootTestContext.ConfiguredRelative(LiveWslRootTestContext.RunName + "\\foreign-link.txt")),
                host.Resolve(LiveWslRootTestContext.ConfiguredRelative(LiveWslRootTestContext.RunName + "\\sentinel.txt")));
        });
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(opened).Root;
        _ = root.WriteFile("sentinel.txt", [1, 2, 3]);
        _ = root.CreateFileSymbolicLink("source/link", "sentinel.txt");

        LiveWslRootCleanupOutcome outcome = root.Cleanup();

        Assert.AreSame(
            LiveWslRootFailureKind.LinkDetected,
            Assert.IsInstanceOfType<LiveWslRootCleanupRejected>(outcome).Failure);
        Assert.IsTrue(Directory.Exists(host.Resolve(LiveWslRootTestContext.ConfiguredRelative(LiveWslRootTestContext.RunName))));
        Assert.IsTrue(File.Exists(host.Resolve(LiveWslRootTestContext.ConfiguredRelative(LiveWslRootTestContext.RunName + "\\sentinel.txt"))));
    }
}
