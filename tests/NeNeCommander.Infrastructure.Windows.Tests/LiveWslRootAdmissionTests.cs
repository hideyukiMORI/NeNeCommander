using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Preserves LiveWslTestRootTests evidence for LiveWslRootAdmissionTests.</summary>
[TestClass]
public sealed class LiveWslRootAdmissionTests
{
    /// <summary>Proves an omitted opt-in is reported as unexecuted without filesystem access.</summary>
    [TestMethod]
    public void OpenWhenParameterIsAbsentReportsUnexecuted()
    {
        LiveWslRootOpenOutcome outcome = LiveWslTestRoot.Open(
            LiveWslRootAdmission.Create(null, null, null),
            [],
            LiveWslRootTestContext.RejectingFileSystem(),
            LiveWslRootTestContext.RejectResolution,
            LiveWslRootTestContext.RejectLinkCreation,
            LiveWslRootTestContext.RejectUnlink);

        LiveWslRootOpenRejected rejected = Assert.IsInstanceOfType<LiveWslRootOpenRejected>(outcome);
        Assert.AreSame(LiveWslRootFailureKind.Unexecuted, rejected.Failure);
    }

    /// <summary>Proves malformed, unsafe, and unregistered roots are rejected before resolution.</summary>
    [TestMethod]
    public void OpenWhenConfigurationIsInvalidRejectsBeforeResolution()
    {
        IReadOnlyList<WslPath> registered = [LiveWslRootTestContext.WslRoot(LiveWslRootTestContext.DistributionName)];
        LiveWslRootOpenOutcome malformed = LiveWslTestRoot.Open(
            LiveWslRootTestContext.Admission("C:\\temp"),
            registered,
            LiveWslRootTestContext.RejectingFileSystem(),
            LiveWslRootTestContext.RejectResolution,
            LiveWslRootTestContext.RejectLinkCreation,
            LiveWslRootTestContext.RejectUnlink);
        LiveWslRootOpenOutcome unsafeRoot = LiveWslTestRoot.Open(
            LiveWslRootTestContext.Admission("\\\\wsl.localhost\\Ubuntu\\home\\NeNeCommander-Live-Proof"),
            registered,
            LiveWslRootTestContext.RejectingFileSystem(),
            LiveWslRootTestContext.RejectResolution,
            LiveWslRootTestContext.RejectLinkCreation,
            LiveWslRootTestContext.RejectUnlink);
        LiveWslRootOpenOutcome unregistered = LiveWslTestRoot.Open(
            LiveWslRootTestContext.Admission("\\\\wsl.localhost\\Debian\\tmp\\NeNeCommander-Live-Proof"),
            registered,
            LiveWslRootTestContext.RejectingFileSystem(),
            LiveWslRootTestContext.RejectResolution,
            LiveWslRootTestContext.RejectLinkCreation,
            LiveWslRootTestContext.RejectUnlink);

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
            LiveWslRootTestContext.ConfiguredWslText(),
            missingFact.Equals("home", StringComparison.Ordinal) ? null : LiveWslRootAdmission.HomeFactAccepted,
            missingFact.Equals("mount", StringComparison.Ordinal) ? null : LiveWslRootAdmission.MountFactAccepted);

        LiveWslRootOpenOutcome outcome = LiveWslTestRoot.Open(
            admission,
            [LiveWslRootTestContext.WslRoot(LiveWslRootTestContext.DistributionName)],
            LiveWslRootTestContext.RejectingFileSystem(),
            LiveWslRootTestContext.RejectResolution,
            LiveWslRootTestContext.RejectLinkCreation,
            LiveWslRootTestContext.RejectUnlink);

        Assert.AreSame(
            LiveWslRootFailureKind.InvalidConfiguration,
            Assert.IsInstanceOfType<LiveWslRootOpenRejected>(outcome).Failure);
    }

    /// <summary>Proves an initially nonempty configured root is rejected without deleting its entry.</summary>
    [TestMethod]
    public void OpenWhenConfiguredRootIsNonemptyRejectsWithoutMutation()
    {
        using TestOwnedTemporaryRoot host = LiveWslRootTestContext.CreateHost();
        string residue = host.WriteFile(LiveWslRootTestContext.ConfiguredRelative("foreign.txt"), "foreign");

        LiveWslRootOpenOutcome outcome = LiveWslRootTestContext.Open(host);

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
        using TestOwnedTemporaryRoot host = LiveWslRootTestContext.CreateHostWithoutConfiguredRoot();
        _ = host.CreateDirectory("distribution\\sentinel");
        string sentinel = host.WriteFile("distribution\\sentinel\\keep.txt", "keep");
        _ = host.CreateJunction(LiveWslRootTestContext.ConfiguredRelative(string.Empty), "distribution\\sentinel");

        LiveWslRootOpenOutcome outcome = LiveWslRootTestContext.Open(host);

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
        _ = host.CreateDirectory("distribution\\actual-tmp\\" + LiveWslRootTestContext.ConfiguredName);
        _ = host.CreateJunction("distribution\\tmp", "distribution\\actual-tmp");

        LiveWslRootOpenOutcome outcome = LiveWslRootTestContext.Open(host);

        Assert.AreSame(
            LiveWslRootFailureKind.LinkDetected,
            Assert.IsInstanceOfType<LiveWslRootOpenRejected>(outcome).Failure);
        Assert.IsEmpty(Directory.GetFileSystemEntries(host.Resolve("distribution\\actual-tmp\\" + LiveWslRootTestContext.ConfiguredName)));
    }
}
