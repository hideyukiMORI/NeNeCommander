using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Preserves LiveWslTestRootTests evidence for LiveWslRootSetupTests.</summary>
[TestClass]
public sealed class LiveWslRootSetupTests
{
    /// <summary>Proves unregistered residue during setup refuses before any fixture effect.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void SetupWhenForeignResidueAppearsRejectsBeforeEffect()
    {
        using TestOwnedTemporaryRoot host = LiveWslRootTestContext.CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(LiveWslRootTestContext.Open(host)).Root;
        string foreign = host.WriteFile(LiveWslRootTestContext.ConfiguredRelative(LiveWslRootTestContext.RunName + "\\foreign.txt"), "foreign");

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => root.CreateDirectory("blocked"));

        Assert.IsTrue(File.Exists(foreign));
        Assert.IsFalse(Directory.Exists(host.Resolve(LiveWslRootTestContext.ConfiguredRelative(LiveWslRootTestContext.RunName + "\\blocked"))));
    }

    /// <summary>Proves fixture setup cannot mutate a replacement at the approved run path.</summary>
    [TestMethod]
    [DataRow("directory")]
    [DataRow("file")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void SetupWhenRunChildIsReplacedRejectsBeforeEffect(string operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        using TestOwnedTemporaryRoot host = LiveWslRootTestContext.CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(LiveWslRootTestContext.Open(host)).Root;
        string run = host.Resolve(LiveWslRootTestContext.ConfiguredRelative(LiveWslRootTestContext.RunName));
        string parked = host.Resolve(LiveWslRootTestContext.ConfiguredRelative("parked"));
        Directory.Move(run, parked);
        _ = Directory.CreateDirectory(run);

        _ = operation.Equals("directory", StringComparison.Ordinal)
            ? Assert.ThrowsExactly<InvalidOperationException>(() => root.CreateDirectory("blocked"))
            : Assert.ThrowsExactly<InvalidOperationException>(() => root.WriteFile("blocked.txt", [1]));

        Assert.IsFalse(Directory.Exists(System.IO.Path.Join(run, "blocked")));
        Assert.IsFalse(File.Exists(System.IO.Path.Join(run, "blocked.txt")));
    }

    /// <summary>
    /// Proves replacing the configured root after admission refuses the next fixture effect, which
    /// is the only interval the C# owner can observe now that it captures identity itself.
    /// </summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void SetupWhenConfiguredRootIsReplacedRejectsBeforeEffect()
    {
        using TestOwnedTemporaryRoot host = LiveWslRootTestContext.CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(LiveWslRootTestContext.Open(host)).Root;
        string configured = host.Resolve(LiveWslRootTestContext.ConfiguredRelative(string.Empty));
        string parked = host.Resolve("distribution\\tmp\\parked");
        Directory.Move(configured, parked);
        _ = Directory.CreateDirectory(configured);
        Directory.Move(
            System.IO.Path.Join(parked, LiveWslRootTestContext.RunName),
            System.IO.Path.Join(configured, LiveWslRootTestContext.RunName));

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => root.CreateDirectory("blocked"));

        Assert.IsTrue(Directory.Exists(System.IO.Path.Join(configured, LiveWslRootTestContext.RunName)));
        Assert.IsFalse(Directory.Exists(System.IO.Path.Join(configured, LiveWslRootTestContext.RunName, "blocked")));
    }

    /// <summary>Proves an ancestor replaced by a link after admission refuses the next effect.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void SetupWhenTemporaryRootBecomesLinkRejectsBeforeEffect()
    {
        using TestOwnedTemporaryRoot host = LiveWslRootTestContext.CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(LiveWslRootTestContext.Open(host)).Root;
        Directory.Move(host.Resolve("distribution\\tmp"), host.Resolve("distribution\\parked-tmp"));
        _ = host.CreateJunction("distribution\\tmp", "distribution\\parked-tmp");

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => root.CreateDirectory("blocked"));

        Assert.IsTrue(Directory.Exists(host.Resolve(LiveWslRootTestContext.ConfiguredRelative(LiveWslRootTestContext.RunName))));
        Assert.IsFalse(Directory.Exists(host.Resolve(LiveWslRootTestContext.ConfiguredRelative(LiveWslRootTestContext.RunName + "\\blocked"))));
    }

    /// <summary>
    /// Proves a same-length owned fixture rewrite that restores creation and last-write time is
    /// still refused, because the ADR-0049 identity carries the kernel-assigned change time.
    /// </summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-020")]
    public void SetupWhenOwnedFixtureIsReplacedPreservingMetadataRejectsBeforeEffect()
    {
        using TestOwnedTemporaryRoot host = LiveWslRootTestContext.CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(LiveWslRootTestContext.Open(host)).Root;
        _ = root.WriteFile("payload.bin", [0x61, 0x62, 0x63, 0x64]);
        host.ReplaceFilePreservingMetadata(LiveWslRootTestContext.ConfiguredRelative(LiveWslRootTestContext.RunName + "\\payload.bin"), "wxyz");

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => root.WriteFile("blocked.bin", [1]));

        Assert.IsFalse(File.Exists(host.Resolve(LiveWslRootTestContext.ConfiguredRelative(LiveWslRootTestContext.RunName + "\\blocked.bin"))));
        Assert.IsTrue(File.Exists(host.Resolve(LiveWslRootTestContext.ConfiguredRelative(LiveWslRootTestContext.RunName + "\\payload.bin"))));
    }
}
