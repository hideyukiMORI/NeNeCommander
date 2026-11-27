using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves direct owned-link admission consumes a failed removal without losing evidence or permitting reuse.</summary>
[TestClass]
public sealed class WindowsTransferOwnedLinkAdmissionTests
{
    /// <summary>Missing removal evidence is rejected before consuming a live owned artifact.</summary>
    [TestMethod]
    public void RemoveLinkWhenEvidenceIsMissingPreservesLiveOwnership()
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        using WindowsTransferArtifactOwner owner = Prepare(files);
        owner.CreateNative(security);
        VerifiedEntrySnapshot expected = owner.AdmitAfterTargetMutation();
        TransferArtifactReceipt? before = owner.Receipt;

        TransferArgumentAssertions.Rejects(() => owner.RemoveLink(null!, WindowsTransferArtifactRemoval.Native), "expected");
        TransferArgumentAssertions.Rejects(() => owner.RemoveLink(expected, null!), "removal");

        Assert.AreEqual(before, owner.Receipt);
        Assert.IsFalse(owner.Handle.IsClosed);
        Assert.AreEqual(expected, owner.AdmitAfterTargetMutation());
    }

    /// <summary>A different saved full identity never authorizes deletion, and the failed admission is one-shot.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void RemoveLinkWhenSavedIdentityDiffersRetainsLinkAndConsumesAccess()
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        using WindowsTransferArtifactOwner owner = Prepare(files);
        owner.CreateNative(security);
        SafeFileHandle held = owner.Handle;
        VerifiedEntrySnapshot current = owner.AdmitAfterTargetMutation();
        VerifiedEntrySnapshot foreign = WindowsTransferRecoveryTestFixture.Entry(current.Path, "foreign");
        VerifiedEntrySnapshot expected = VerifiedEntrySnapshot.Create(current.Path, foreign.Identity, current.ObjectIdentity);
        int nativeCalls = 0;
        WindowsTransferArtifactRemoval removal = new(_ => { nativeCalls++; return unchecked((int)0xC0000001); }, _ => { nativeCalls++; return false; });

        _ = Assert.ThrowsExactly<IOException>(() => owner.RemoveLink(expected, removal));

        Assert.AreEqual(0, nativeCalls);
        IdentifiedTransferArtifact receipt = Known(owner);
        Assert.AreEqual(current.ObjectIdentity, receipt.Artifact.ObjectIdentity);
        Assert.AreEqual(ArtifactLinkObservation.Retained, receipt.Link);
        Assert.AreEqual(TransferPathObservation.LastKnownPath, receipt.Artifact.PathObservation);
        Assert.IsFalse(held.IsClosed);
        Assert.IsTrue(File.Exists(current.Path.CanonicalText));
        _ = Assert.ThrowsExactly<InvalidOperationException>(() => _ = owner.Handle);
        _ = Assert.ThrowsExactly<InvalidOperationException>(() => owner.RemoveLink(current, removal));
        Assert.AreEqual(0, nativeCalls);
        Assert.AreEqual("target", files.Read(files.Pair.Target.Path));
    }

    /// <summary>Uncreated and disposed owners cannot be turned into removal authority by a saved receipt.</summary>
    [TestMethod]
    [DataRow("ready")]
    [DataRow("disposed")]
    public void RemoveLinkWhenOwnerCannotRemoveRejectsBeforeNativeEntry(string state)
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        using WindowsTransferArtifactOwner owner = Prepare(files);
        VerifiedEntrySnapshot expected = WindowsTransferRecoveryTestFixture.Entry(files.Pair.Target.Path, "unowned");
        int calls = 0;
        WindowsTransferArtifactRemoval removal = new(_ => { calls++; return unchecked((int)0xC0000001); }, _ => { calls++; return false; });
        if (state == "disposed")
        {
            owner.Dispose();
            _ = Assert.ThrowsExactly<ObjectDisposedException>(() => owner.RemoveLink(expected, removal));
        }
        else
        {
            _ = Assert.ThrowsExactly<InvalidOperationException>(() => owner.RemoveLink(expected, removal));
        }
        Assert.AreEqual(0, calls);
        Assert.IsNull(owner.Receipt);
        Assert.AreEqual("target", files.Read(files.Pair.Target.Path));
    }

    /// <summary>Reentrant disposal during an unacknowledged removal stays disposed and retains uncertainty.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public void RemoveLinkWhenNativeFailureDisposesOwnerCannotResurrectIt()
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        using WindowsTransferArtifactOwner owner = Prepare(files);
        owner.CreateNative(security);
        SafeFileHandle held = owner.Handle;
        VerifiedEntrySnapshot expected = owner.AdmitAfterTargetMutation();
        int closes = 0;
        WindowsTransferArtifactRemoval removal = new(_ =>
        {
            owner.Dispose();
            return unchecked((int)0xC0000001);
        }, _ => { closes++; return false; });

        _ = Assert.ThrowsExactly<IOException>(() => owner.RemoveLink(expected, removal));

        Assert.AreEqual(0, closes);
        Assert.IsTrue(held.IsClosed);
        Assert.AreEqual(ArtifactLinkObservation.Uncertain, Known(owner).Link);
        Assert.AreEqual(TransferPathObservation.LastKnownPath, Known(owner).Artifact.PathObservation);
        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => _ = owner.Handle);
        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => owner.RemoveLink(expected, removal));
        Assert.IsTrue(File.Exists(expected.Path.CanonicalText));
    }

    /// <summary>Two individually supported observations that disagree cannot yield a current artifact identity.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void AdmitAfterTargetMutationWhenFactsChangeDuringObservationRetainsOnlyLastKnownPath()
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        int observations = 0;
        bool armed = false;
        WindowsTransferArtifactQueries queries = new(WindowsTransferArtifactQueries.Native.ReadIdentity, handle =>
        {
            WindowsLocalHandleFacts facts = WindowsFileIdentifier.ReadLocalFacts(handle);
            if (!armed || ++observations != 2)
            {
                return facts;
            }
            WindowsLocalBasicFacts basic = new(facts.Basic.CreationFileTimeUtc, facts.Basic.LastWriteFileTimeUtc + 1,
                facts.Basic.ChangeFileTimeUtc, facts.Basic.Attributes);
            return new WindowsLocalHandleFacts(facts.ObjectIdentity, basic, facts.Standard, facts.FileSystemName);
        }, WindowsTransferArtifactQueries.Native.RequirePath);
        using WindowsTransferArtifactOwner owner = Prepare(files, queries);
        owner.CreateNative(security);
        FileObjectIdentity identity = Known(owner).Artifact.ObjectIdentity;
        armed = true;

        _ = Assert.ThrowsExactly<IOException>(owner.AdmitAfterTargetMutation);

        Assert.AreEqual(2, observations);
        Assert.AreEqual(identity, Known(owner).Artifact.ObjectIdentity);
        Assert.AreEqual(TransferPathObservation.LastKnownPath, Known(owner).Artifact.PathObservation);
        Assert.AreEqual(ArtifactLinkObservation.Retained, Known(owner).Link);
        Assert.IsFalse(owner.Handle.IsClosed);
    }

    private static WindowsTransferArtifactOwner Prepare(WindowsOverwritePreparationFixture files, WindowsTransferArtifactQueries? queries = null)
    {
        return WindowsTransferArtifactOwner.Prepare(TransferArtifactRole.OriginalBackup,
            WindowsTransferRecoveryTestFixture.Path(files.Root.Resolve("owned-artifact")), files.Pair.Target,
            queries ?? WindowsTransferArtifactQueries.Native);
    }

    private static IdentifiedTransferArtifact Known(WindowsTransferArtifactOwner owner)
    {
        return Assert.IsInstanceOfType<IdentifiedTransferArtifact>(owner.Receipt);
    }
}
