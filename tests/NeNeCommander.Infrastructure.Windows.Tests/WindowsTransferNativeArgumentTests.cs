using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves missing path or handle ownership is rejected before the native transfer boundary.</summary>
[TestClass]
public sealed class WindowsTransferNativeArgumentTests
{
    /// <summary>Absent path evidence cannot be interpreted as a current directory or an implicit native handle.</summary>
    [TestMethod]
    public void OpenWhenPathOrHandleIsAbsentRejectsBeforeNativeAccess()
    {
        WindowsLocalPath parent = WindowsTransferRecoveryTestFixture.Path(@"D:\recovery");
        WindowsLocalPath child = WindowsTransferRecoveryTestFixture.Path(@"D:\recovery\target.txt");
        using SafeFileHandle unavailable = new(IntPtr.Zero, false);

        TransferArgumentAssertions.Rejects(() => WindowsTransferEntryOpen.OpenRoot(null!), "path");
        TransferArgumentAssertions.Rejects(() => WindowsTransferEntryOpen.OpenDirectory(null!, parent, child), "parent");
        TransferArgumentAssertions.Rejects(() => WindowsTransferEntryOpen.OpenDirectory(unavailable, null!, child), "parent");
        TransferArgumentAssertions.Rejects(() => WindowsTransferEntryOpen.OpenDirectory(unavailable, parent, null!), "child");
        TransferArgumentAssertions.Rejects(() => WindowsTransferPathObservation.Require(null!, child), "handle");
        TransferArgumentAssertions.Rejects(() => WindowsTransferPathObservation.Require(unavailable, null!), "path");
        TransferArgumentAssertions.Rejects(() => WindowsTransferPathObservation.HasVolumeGuid(null!), "path");
        TransferArgumentAssertions.Rejects(() => WindowsTransferHeldEntry.Take(null!, unavailable), "path");
        TransferArgumentAssertions.Rejects(() => WindowsTransferHeldEntry.Take(child, null!), "handle");
        TransferArgumentAssertions.Rejects(() => WindowsTransferPathLease.Open(null!, FileAccess.Read, new WindowsDriveBoundary()), "path");
        TransferArgumentAssertions.Rejects(() => WindowsTransferPathLease.Open(child, FileAccess.Read, null!), "driveBoundary");
        TransferArgumentAssertions.Rejects(() => WindowsOverwriteFileAdmission.Observe(null!), "lease");
    }

    /// <summary>Preparation factories require their exact role, candidate, parent lease and observation owner.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public void PrepareWhenArtifactOwnershipIsIncompleteRejectsWithoutCreatingFiles()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsTransferArtifactNames names = WindowsTransferArtifactNames.Create(fixture.Pair.Target.Path, WindowsTransferRecoveryTestFixture.Token);
        WindowsTransferArtifactQueries queries = WindowsTransferArtifactQueries.Native;

        TransferArgumentAssertions.Rejects(() => WindowsTransferArtifactOwner.Prepare(null!, names.Recovery, fixture.Pair.Target, queries), "role");
        TransferArgumentAssertions.Rejects(() => WindowsTransferArtifactOwner.Prepare(TransferArtifactRole.RecoveryRecord, null!, fixture.Pair.Target, queries), "candidate");
        TransferArgumentAssertions.Rejects(() => WindowsTransferArtifactOwner.Prepare(TransferArtifactRole.RecoveryRecord, names.Recovery, null!, queries), "parentOwner");
        TransferArgumentAssertions.Rejects(() => WindowsTransferArtifactOwner.Prepare(TransferArtifactRole.RecoveryRecord, names.Recovery, fixture.Pair.Target, null!), "queries");
        TransferArgumentAssertions.Rejects(() => WindowsTransferPreparationArtifacts.Create(null!, fixture.Pair.Target, queries), "names");
        TransferArgumentAssertions.Rejects(() => WindowsTransferPreparationArtifacts.Create(names, null!, queries), "parent");
        TransferArgumentAssertions.Rejects(() => WindowsTransferPreparationArtifacts.Create(names, fixture.Pair.Target, null!), "queries");
        Assert.IsFalse(File.Exists(names.Recovery.CanonicalText));
        Assert.AreEqual("source", fixture.Read(fixture.Pair.Source.Path));
        Assert.AreEqual("target", fixture.Read(fixture.Pair.Target.Path));
    }

    /// <summary>A candidate under another parent cannot acquire this lease's ownership.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-003")]
    public void PrepareWhenCandidateBelongsToAnotherParentRefusesOwnership()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsLocalPath foreign = WindowsTransferRecoveryTestFixture.Path(@"D:\foreign\record");

        ArgumentException failure = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            using WindowsTransferArtifactOwner owner = WindowsTransferArtifactOwner.Prepare(
                TransferArtifactRole.RecoveryRecord, foreign, fixture.Pair.Target, WindowsTransferArtifactQueries.Native);
        });

        Assert.AreEqual("candidate", failure.ParamName);
        Assert.AreEqual("target", fixture.Read(fixture.Pair.Target.Path));
    }

    /// <summary>Native creation never substitutes ambient ownership for an absent security descriptor.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public void CreateWhenSecurityOrParentOwnerIsAbsentRejectsBeforeCreation()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsTransferArtifactNames names = WindowsTransferArtifactNames.Create(fixture.Pair.Target.Path, WindowsTransferRecoveryTestFixture.Token);
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        using WindowsTransferArtifactOwner owner = WindowsTransferArtifactOwner.Prepare(TransferArtifactRole.RecoveryRecord,
            names.Recovery, fixture.Pair.Target, WindowsTransferArtifactQueries.Native);

        TransferArgumentAssertions.Rejects(() => owner.CreateNative(null!), "security");
        TransferArgumentAssertions.Rejects(() => WindowsTransferEntryOpen.CreateArtifact(null!, names.Recovery, security), "owner");
        TransferArgumentAssertions.Rejects(() => WindowsTransferEntryOpen.CreateArtifact(fixture.Pair.Target, names.Recovery, null!), "security");
        Assert.IsNull(owner.Receipt);
        Assert.IsFalse(File.Exists(names.Recovery.CanonicalText));
        Assert.AreEqual("source", fixture.Read(fixture.Pair.Source.Path));
        Assert.AreEqual("target", fixture.Read(fixture.Pair.Target.Path));
    }
}
