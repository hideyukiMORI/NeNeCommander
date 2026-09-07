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

    /// <summary>Proves an omitted opt-in is reported as unexecuted without filesystem access.</summary>
    [TestMethod]
    public void OpenWhenParameterIsAbsentReportsUnexecuted()
    {
        LiveWslRootOpenOutcome outcome = LiveWslTestRoot.Open(
            Admission(null, null, null),
            [],
            static _ => throw new InvalidOperationException());

        LiveWslRootOpenRejected rejected = Assert.IsInstanceOfType<LiveWslRootOpenRejected>(outcome);
        Assert.AreSame(LiveWslRootFailureKind.Unexecuted, rejected.Failure);
    }

    /// <summary>Proves malformed, unsafe, and unregistered roots are rejected before resolution.</summary>
    [TestMethod]
    public void OpenWhenConfigurationIsInvalidRejectsBeforeResolution()
    {
        IReadOnlyList<WslPath> registered = [WslRoot(DistributionName)];
        LiveWslRootOpenOutcome malformed = LiveWslTestRoot.Open(
            Admission("C:\\temp", Identifier('A'), Identifier('B')),
            registered,
            RejectResolution);
        LiveWslRootOpenOutcome unsafeRoot = LiveWslTestRoot.Open(
            Admission(
                "\\\\wsl.localhost\\Ubuntu\\home\\NeNeCommander-Live-Proof",
                Identifier('A'),
                Identifier('B')),
            registered,
            RejectResolution);
        LiveWslRootOpenOutcome unregistered = LiveWslTestRoot.Open(
            Admission(
                "\\\\wsl.localhost\\Debian\\tmp\\NeNeCommander-Live-Proof",
                Identifier('A'),
                Identifier('B')),
            registered,
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
    public void OpenWhenRunnerAdmissionIsIncompleteRejectsBeforeResolution()
    {
        LiveWslRootAdmission admission = LiveWslRootAdmission.Create(
            ConfiguredWslText(),
            Identifier('A'),
            null,
            LiveWslRootAdmission.HomeFactAccepted,
            LiveWslRootAdmission.MountFactAccepted);

        LiveWslRootOpenOutcome outcome = LiveWslTestRoot.Open(
            admission,
            [WslRoot(DistributionName)],
            RejectResolution);

        Assert.AreSame(
            LiveWslRootFailureKind.InvalidConfiguration,
            Assert.IsInstanceOfType<LiveWslRootOpenRejected>(outcome).Failure);
    }

    /// <summary>Proves an identity from another root rejects before the run child is created.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void OpenWhenAdmissionIdentifiesAnotherRootRejectsBeforeMutation()
    {
        using TestOwnedTemporaryRoot host = CreateHost();
        string other = host.CreateDirectory("distribution\\tmp\\NeNeCommander-Live-Other");
        LiveWslRootAdmission admission = Admission(
            ConfiguredWslText(),
            WindowsFileIdentifier.Describe(host.Resolve("distribution\\tmp")),
            WindowsFileIdentifier.Describe(other));

        LiveWslRootOpenOutcome outcome = LiveWslTestRoot.Open(
            admission,
            [WslRoot(DistributionName)],
            path => Resolve(host, path));

        Assert.AreSame(
            LiveWslRootFailureKind.IdentityChanged,
            Assert.IsInstanceOfType<LiveWslRootOpenRejected>(outcome).Failure);
        Assert.IsEmpty(Directory.GetFileSystemEntries(host.Resolve(ConfiguredRelative(string.Empty))));
    }

    /// <summary>Proves replacing an admitted root rejects the replacement and preserves the original.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void OpenWhenAdmittedRootIsReplacedRejectsBeforeMutation()
    {
        using TestOwnedTemporaryRoot host = CreateHost();
        string configured = host.Resolve(ConfiguredRelative(string.Empty));
        LiveWslRootAdmission admission = Admission(
            ConfiguredWslText(),
            WindowsFileIdentifier.Describe(host.Resolve("distribution\\tmp")),
            WindowsFileIdentifier.Describe(configured));
        string parked = host.Resolve("distribution\\tmp\\parked");
        Directory.Move(configured, parked);
        _ = Directory.CreateDirectory(configured);

        LiveWslRootOpenOutcome outcome = LiveWslTestRoot.Open(
            admission,
            [WslRoot(DistributionName)],
            path => Resolve(host, path));

        Assert.AreSame(
            LiveWslRootFailureKind.IdentityChanged,
            Assert.IsInstanceOfType<LiveWslRootOpenRejected>(outcome).Failure);
        Assert.IsEmpty(Directory.GetFileSystemEntries(configured));
        Assert.IsTrue(Directory.Exists(parked));
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

    /// <summary>Proves replacing the ownership marker makes cleanup refuse the complete run tree.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void CleanupWhenOwnershipMarkerIsReplacedRefusesRecursiveDelete()
    {
        using TestOwnedTemporaryRoot host = CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(Open(host)).Root;
        host.ReplaceFilePreservingMetadata(ConfiguredRelative(RunName + "\\.nene-commander-owner"), "Fake");

        LiveWslRootCleanupOutcome outcome = root.Cleanup();

        Assert.AreSame(
            LiveWslRootFailureKind.IdentityChanged,
            Assert.IsInstanceOfType<LiveWslRootCleanupRejected>(outcome).Failure);
        Assert.IsTrue(Directory.Exists(host.Resolve(ConfiguredRelative(RunName))));
    }

    /// <summary>Proves rewriting marker bytes under the same identity makes cleanup refuse.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void CleanupWhenOwnershipMarkerBytesChangeRefusesRecursiveDelete()
    {
        using TestOwnedTemporaryRoot host = CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(Open(host)).Root;
        string marker = host.Resolve(ConfiguredRelative(RunName + "\\.nene-commander-owner"));
        File.WriteAllText(marker, "Fake");

        LiveWslRootCleanupOutcome outcome = root.Cleanup();

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
        return LiveWslTestRoot.Open(
            Admission(
                ConfiguredWslText(),
                WindowsFileIdentifier.Describe(host.Resolve("distribution\\tmp")),
                WindowsFileIdentifier.Describe(host.Resolve(ConfiguredRelative(string.Empty)))),
            [WslRoot(DistributionName)],
            path => Resolve(host, path));
    }

    private static LiveWslRootAdmission Admission(
        string? configuredRoot,
        string? temporaryRootIdentity,
        string? configuredRootIdentity)
    {
        return LiveWslRootAdmission.Create(
            configuredRoot,
            temporaryRootIdentity,
            configuredRootIdentity,
            LiveWslRootAdmission.HomeFactAccepted,
            LiveWslRootAdmission.MountFactAccepted);
    }

    private static string Identifier(char value)
    {
        return new string(value, 48);
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
