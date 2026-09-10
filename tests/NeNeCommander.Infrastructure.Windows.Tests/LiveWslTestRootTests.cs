using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the live WSL owner fails closed before touching an operator root.</summary>
[TestClass]
public sealed class LiveWslTestRootTests
{
    private const string DistributionName = "Ubuntu";
    private const string ConfiguredName = "NeNeCommander-Live-Proof";
    private const string RunName = "NeNeCommander-Live-Run";
    private const string MarkerName = ".nene-commander-owner";

    /// <summary>Proves an omitted opt-in is reported as unexecuted without filesystem access.</summary>
    [TestMethod]
    public void OpenWhenParameterIsAbsentReportsUnexecuted()
    {
        LiveWslRootOpenOutcome outcome = LiveWslTestRoot.Open(
            LiveWslRootAdmission.Create(null, null, null),
            [],
            RejectingFileSystem(),
            RejectResolution);

        LiveWslRootOpenRejected rejected = Assert.IsInstanceOfType<LiveWslRootOpenRejected>(outcome);
        Assert.AreSame(LiveWslRootFailureKind.Unexecuted, rejected.Failure);
    }

    /// <summary>Proves malformed, unsafe, and unregistered roots are rejected before resolution.</summary>
    [TestMethod]
    public void OpenWhenConfigurationIsInvalidRejectsBeforeResolution()
    {
        IReadOnlyList<WslPath> registered = [WslRoot(DistributionName)];
        LiveWslRootOpenOutcome malformed = LiveWslTestRoot.Open(
            Admission("C:\\temp"),
            registered,
            RejectingFileSystem(),
            RejectResolution);
        LiveWslRootOpenOutcome unsafeRoot = LiveWslTestRoot.Open(
            Admission("\\\\wsl.localhost\\Ubuntu\\home\\NeNeCommander-Live-Proof"),
            registered,
            RejectingFileSystem(),
            RejectResolution);
        LiveWslRootOpenOutcome unregistered = LiveWslTestRoot.Open(
            Admission("\\\\wsl.localhost\\Debian\\tmp\\NeNeCommander-Live-Proof"),
            registered,
            RejectingFileSystem(),
            RejectResolution);

        Assert.AreSame(
            LiveWslRootFailureKind.InvalidConfiguration,
            Assert.IsInstanceOfType<LiveWslRootOpenRejected>(malformed).Failure);
        Assert.AreSame(
            LiveWslRootFailureKind.UnsafeRoot,
            Assert.IsInstanceOfType<LiveWslRootOpenRejected>(unsafeRoot).Failure);
        Assert.AreSame(
            LiveWslRootFailureKind.DistributionUnavailable,
            Assert.IsInstanceOfType<LiveWslRootOpenRejected>(unregistered).Failure);
    }

    /// <summary>Proves missing runner facts reject before the configured root is resolved.</summary>
    [TestMethod]
    [DataRow("home")]
    [DataRow("mount")]
    public void OpenWhenRunnerAdmissionIsIncompleteRejectsBeforeResolution(string missingFact)
    {
        ArgumentNullException.ThrowIfNull(missingFact);
        LiveWslRootAdmission admission = LiveWslRootAdmission.Create(
            ConfiguredWslText(),
            missingFact.Equals("home", StringComparison.Ordinal) ? null : LiveWslRootAdmission.HomeFactAccepted,
            missingFact.Equals("mount", StringComparison.Ordinal) ? null : LiveWslRootAdmission.MountFactAccepted);

        LiveWslRootOpenOutcome outcome = LiveWslTestRoot.Open(
            admission,
            [WslRoot(DistributionName)],
            RejectingFileSystem(),
            RejectResolution);

        Assert.AreSame(
            LiveWslRootFailureKind.InvalidConfiguration,
            Assert.IsInstanceOfType<LiveWslRootOpenRejected>(outcome).Failure);
    }

    /// <summary>Proves an initially nonempty configured root is rejected without deleting its entry.</summary>
    [TestMethod]
    public void OpenWhenConfiguredRootIsNonemptyRejectsWithoutMutation()
    {
        using TestOwnedTemporaryRoot host = CreateHost();
        string residue = host.WriteFile(ConfiguredRelative("foreign.txt"), "foreign");

        LiveWslRootOpenOutcome outcome = Open(host);

        Assert.AreSame(
            LiveWslRootFailureKind.RootNotEmpty,
            Assert.IsInstanceOfType<LiveWslRootOpenRejected>(outcome).Failure);
        Assert.IsTrue(File.Exists(residue));
    }

    /// <summary>Proves a configured root that is a link is rejected without following it.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void OpenWhenConfiguredRootIsLinkRejectsWithoutFollowingTarget()
    {
        using TestOwnedTemporaryRoot host = CreateHostWithoutConfiguredRoot();
        _ = host.CreateDirectory("distribution\\sentinel");
        string sentinel = host.WriteFile("distribution\\sentinel\\keep.txt", "keep");
        _ = host.CreateJunction(ConfiguredRelative(string.Empty), "distribution\\sentinel");

        LiveWslRootOpenOutcome outcome = Open(host);

        Assert.AreSame(
            LiveWslRootFailureKind.LinkDetected,
            Assert.IsInstanceOfType<LiveWslRootOpenRejected>(outcome).Failure);
        Assert.IsTrue(File.Exists(sentinel));
    }

    /// <summary>Proves an ancestor above the configured root that is a link is rejected at admission.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void OpenWhenTemporaryRootIsLinkRejectsWithoutFollowingTarget()
    {
        using TestOwnedTemporaryRoot host = TestOwnedTemporaryRoot.Create();
        _ = host.CreateDirectory("distribution");
        _ = host.CreateDirectory("distribution\\actual-tmp");
        _ = host.CreateDirectory("distribution\\actual-tmp\\" + ConfiguredName);
        _ = host.CreateJunction("distribution\\tmp", "distribution\\actual-tmp");

        LiveWslRootOpenOutcome outcome = Open(host);

        Assert.AreSame(
            LiveWslRootFailureKind.LinkDetected,
            Assert.IsInstanceOfType<LiveWslRootOpenRejected>(outcome).Failure);
        Assert.IsEmpty(Directory.GetFileSystemEntries(host.Resolve("distribution\\actual-tmp\\" + ConfiguredName)));
    }

    /// <summary>Proves replacing the ownership marker makes cleanup refuse the complete run tree.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void CleanupWhenOwnershipMarkerIsReplacedRefusesRecursiveDelete()
    {
        using TestOwnedTemporaryRoot host = CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(Open(host)).Root;
        host.ReplaceFilePreservingMetadata(ConfiguredRelative(RunName + "\\" + MarkerName), "Fake");

        LiveWslRootCleanupOutcome outcome = root.Cleanup();

        Assert.AreSame(
            LiveWslRootFailureKind.IdentityChanged,
            Assert.IsInstanceOfType<LiveWslRootCleanupRejected>(outcome).Failure);
        Assert.IsTrue(Directory.Exists(host.Resolve(ConfiguredRelative(RunName))));
    }

    /// <summary>Proves rewriting marker bytes under the same path makes cleanup refuse.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void CleanupWhenOwnershipMarkerBytesChangeRefusesRecursiveDelete()
    {
        using TestOwnedTemporaryRoot host = CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(Open(host)).Root;
        string marker = host.Resolve(ConfiguredRelative(RunName + "\\" + MarkerName));
        File.WriteAllText(marker, "Fake");

        LiveWslRootCleanupOutcome outcome = root.Cleanup();

        Assert.AreSame(
            LiveWslRootFailureKind.IdentityChanged,
            Assert.IsInstanceOfType<LiveWslRootCleanupRejected>(outcome).Failure);
        Assert.IsTrue(Directory.Exists(host.Resolve(ConfiguredRelative(RunName))));
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
        using TestOwnedTemporaryRoot host = CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(Open(host)).Root;
        string marker = host.Resolve(ConfiguredRelative(RunName + "\\" + MarkerName));
        host.ReplaceFilePreservingMetadata(ConfiguredRelative(RunName + "\\" + MarkerName), "NeNe");

        LiveWslRootCleanupOutcome outcome = root.Cleanup();

        CollectionAssert.AreEqual("NeNe"u8.ToArray(), File.ReadAllBytes(marker));
        Assert.AreSame(
            LiveWslRootFailureKind.IdentityChanged,
            Assert.IsInstanceOfType<LiveWslRootCleanupRejected>(outcome).Failure);
        Assert.IsTrue(Directory.Exists(host.Resolve(ConfiguredRelative(RunName))));
    }

    /// <summary>Proves unregistered residue makes cleanup refuse the complete run tree.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void CleanupWhenForeignResidueAppearsRefusesRecursiveDelete()
    {
        using TestOwnedTemporaryRoot host = CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(Open(host)).Root;
        string foreign = host.WriteFile(ConfiguredRelative(RunName + "\\foreign.txt"), "foreign");

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
        using TestOwnedTemporaryRoot host = CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(Open(host)).Root;
        string foreign = host.WriteFile(ConfiguredRelative("foreign.txt"), "foreign");

        LiveWslRootCleanupOutcome outcome = root.Cleanup();

        Assert.AreSame(
            LiveWslRootFailureKind.ForeignResidue,
            Assert.IsInstanceOfType<LiveWslRootCleanupRejected>(outcome).Failure);
        Assert.IsTrue(File.Exists(foreign));
        Assert.IsTrue(Directory.Exists(host.Resolve(ConfiguredRelative(RunName))));
    }

    /// <summary>Proves unregistered residue during setup refuses before any fixture effect.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void SetupWhenForeignResidueAppearsRejectsBeforeEffect()
    {
        using TestOwnedTemporaryRoot host = CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(Open(host)).Root;
        string foreign = host.WriteFile(ConfiguredRelative(RunName + "\\foreign.txt"), "foreign");

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => root.CreateDirectory("blocked"));

        Assert.IsTrue(File.Exists(foreign));
        Assert.IsFalse(Directory.Exists(host.Resolve(ConfiguredRelative(RunName + "\\blocked"))));
    }

    /// <summary>Proves an owned link is unlinked before the run child is recursively removed.</summary>
    [TestMethod]
    public void CleanupWhenOwnedLinkTargetsOwnedSentinelUnlinksEntryAndRetainsConfiguredRoot()
    {
        using TestOwnedTemporaryRoot host = CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(Open(host)).Root;
        _ = root.WriteFile("sentinel.txt", [1, 2, 3]);
        _ = root.CreateFileSymbolicLink("source/link", "sentinel.txt");

        LiveWslRootCleanupOutcome outcome = root.Cleanup();

        _ = Assert.IsInstanceOfType<LiveWslRootCleanupCompleted>(outcome);
        Assert.IsTrue(Directory.Exists(host.Resolve(ConfiguredRelative(string.Empty))));
        Assert.IsEmpty(Directory.GetFileSystemEntries(host.Resolve(ConfiguredRelative(string.Empty))));
    }

    /// <summary>Proves replacing the run child is not mistaken for retained ownership.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void CleanupWhenRunChildIsReplacedRefusesReplacementAndOriginalTree()
    {
        using TestOwnedTemporaryRoot host = CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(Open(host)).Root;
        string run = host.Resolve(ConfiguredRelative(RunName));
        string parked = host.Resolve(ConfiguredRelative("parked"));
        Directory.Move(run, parked);
        _ = Directory.CreateDirectory(run);

        LiveWslRootCleanupOutcome outcome = root.Cleanup();

        Assert.AreSame(
            LiveWslRootFailureKind.IdentityChanged,
            Assert.IsInstanceOfType<LiveWslRootCleanupRejected>(outcome).Failure);
        Assert.IsTrue(Directory.Exists(run));
        Assert.IsTrue(Directory.Exists(parked));
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
        using TestOwnedTemporaryRoot host = CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(Open(host)).Root;
        string run = host.Resolve(ConfiguredRelative(RunName));
        string parked = host.Resolve(ConfiguredRelative("parked"));
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
        using TestOwnedTemporaryRoot host = CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(Open(host)).Root;
        string configured = host.Resolve(ConfiguredRelative(string.Empty));
        string parked = host.Resolve("distribution\\tmp\\parked");
        Directory.Move(configured, parked);
        _ = Directory.CreateDirectory(configured);
        Directory.Move(
            System.IO.Path.Join(parked, RunName),
            System.IO.Path.Join(configured, RunName));

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => root.CreateDirectory("blocked"));

        Assert.IsTrue(Directory.Exists(System.IO.Path.Join(configured, RunName)));
        Assert.IsFalse(Directory.Exists(System.IO.Path.Join(configured, RunName, "blocked")));
    }

    /// <summary>Proves an ancestor replaced by a link after admission refuses the next effect.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void SetupWhenTemporaryRootBecomesLinkRejectsBeforeEffect()
    {
        using TestOwnedTemporaryRoot host = CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(Open(host)).Root;
        Directory.Move(host.Resolve("distribution\\tmp"), host.Resolve("distribution\\parked-tmp"));
        _ = host.CreateJunction("distribution\\tmp", "distribution\\parked-tmp");

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => root.CreateDirectory("blocked"));

        Assert.IsTrue(Directory.Exists(host.Resolve(ConfiguredRelative(RunName))));
        Assert.IsFalse(Directory.Exists(host.Resolve(ConfiguredRelative(RunName + "\\blocked"))));
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
        using TestOwnedTemporaryRoot host = CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(Open(host)).Root;
        _ = root.WriteFile("payload.bin", [0x61, 0x62, 0x63, 0x64]);
        host.ReplaceFilePreservingMetadata(ConfiguredRelative(RunName + "\\payload.bin"), "wxyz");

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => root.WriteFile("blocked.bin", [1]));

        Assert.IsFalse(File.Exists(host.Resolve(ConfiguredRelative(RunName + "\\blocked.bin"))));
        Assert.IsTrue(File.Exists(host.Resolve(ConfiguredRelative(RunName + "\\payload.bin"))));
    }

    /// <summary>Proves an owned fixture keeps one identity across reads and declared-tree evidence.</summary>
    [TestMethod]
    public void ReadIdentityWhenOwnedFixtureIsUnchangedRepeatsTheSameToken()
    {
        using TestOwnedTemporaryRoot host = CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(Open(host)).Root;
        _ = root.WriteFile("payload.bin", [7, 7, 7]);
        _ = root.CreateFileSymbolicLink("payload-link.bin", "payload.bin");

        string first = root.ReadIdentity("payload.bin");
        CollectionAssert.AreEqual(new byte[] { 7, 7, 7 }, root.ReadFile("payload.bin"));
        string second = root.ReadIdentity("payload.bin");
        string link = root.ReadIdentity("payload-link.bin");

        Assert.AreEqual(first, second);
        Assert.AreNotEqual(first, link);
        _ = Assert.IsInstanceOfType<LiveWslRootCleanupCompleted>(root.Cleanup());
    }

    private static TestOwnedTemporaryRoot CreateHost()
    {
        TestOwnedTemporaryRoot host = CreateHostWithoutConfiguredRoot();
        _ = host.CreateDirectory(ConfiguredRelative(string.Empty));
        return host;
    }

    private static TestOwnedTemporaryRoot CreateHostWithoutConfiguredRoot()
    {
        TestOwnedTemporaryRoot host = TestOwnedTemporaryRoot.Create();
        _ = host.CreateDirectory("distribution");
        _ = host.CreateDirectory("distribution\\tmp");
        return host;
    }

    private static LiveWslRootOpenOutcome Open(TestOwnedTemporaryRoot host)
    {
        // The deterministic root injects the unguarded NTFS handle-facts reader through the
        // existing WindowsWslFileSystem seam, exactly as WindowsWslFileSystemTests do.
        string Resolved(WslPath path)
        {
            return Resolve(host, path);
        }

        return LiveWslTestRoot.Open(
            Admission(ConfiguredWslText()),
            [WslRoot(DistributionName)],
            new WindowsWslFileSystem(Resolved, WindowsFileIdentifier.ReadHandleFacts),
            Resolved);
    }

    private static WindowsWslFileSystem RejectingFileSystem()
    {
        return new WindowsWslFileSystem(RejectResolution, WindowsFileIdentifier.ReadHandleFacts);
    }

    private static LiveWslRootAdmission Admission(string? configuredRoot)
    {
        return LiveWslRootAdmission.Create(
            configuredRoot,
            LiveWslRootAdmission.HomeFactAccepted,
            LiveWslRootAdmission.MountFactAccepted);
    }

    private static string Resolve(TestOwnedTemporaryRoot host, WslPath path)
    {
        return path.LinuxPath.Equals("/", StringComparison.Ordinal)
            ? host.Resolve("distribution")
            : host.Resolve("distribution" + path.LinuxPath.Replace('/', '\\'));
    }

    private static string RejectResolution(WslPath path)
    {
        throw new AssertFailedException("Unsafe input reached filesystem resolution: " + path.LinuxPath.Length);
    }

    private static WslPath WslRoot(string distribution)
    {
        return Assert.IsInstanceOfType<WslPath>(
            Assert.IsInstanceOfType<PathParseSuccess>(
                FileSystemPath.Parse("\\\\wsl.localhost\\" + distribution + "\\")).Path);
    }

    private static string ConfiguredWslText()
    {
        return "\\\\wsl.localhost\\" + DistributionName + "\\tmp\\" + ConfiguredName;
    }

    private static string ConfiguredRelative(string child)
    {
        string root = "distribution\\tmp\\" + ConfiguredName;
        return child.Length == 0 ? root : root + "\\" + child;
    }
}
