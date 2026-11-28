using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves artifacts keep their own exact identity and admission after their retained target anchor changes content.</summary>
[TestClass]
public sealed class WindowsPostTargetArtifactTests
{
    /// <summary>Proves current artifact admission after target effects uses the same chain without pretending the original target full identity still matches.</summary>
    [TestMethod]
    public void AdmitAfterTargetMutationWhenAnchorContentChangedRetainsExactArtifactAndOriginalBaseline()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        WindowsTransferArtifactNames names = WindowsTransferArtifactNames.Create(fixture.Pair.Target.Path, Guid.Empty);
        using WindowsTransferArtifactOwner owner = WindowsTransferArtifactOwner.Prepare(TransferArtifactRole.OriginalBackup, names.OriginalBackup, fixture.Pair.Target, WindowsTransferArtifactQueries.Native);
        owner.CreateNative(security);
        VerifiedEntrySnapshot before = owner.AdmitBeforeTargetMutation();
        RandomAccess.SetLength(fixture.Pair.Target.Handle, 0);
        _ = Assert.Throws<IOException>(owner.AdmitBeforeTargetMutation);

        VerifiedEntrySnapshot after = owner.AdmitAfterTargetMutation();
        Assert.AreEqual(before, after);
        IdentifiedTransferArtifact receipt = Assert.IsInstanceOfType<IdentifiedTransferArtifact>(owner.Receipt);
        Assert.AreEqual(TransferPathObservation.CurrentPathVerified, receipt.Artifact.PathObservation);
        _ = Assert.Throws<IOException>(() => fixture.Pair.Revalidate(fixture.Pair.Evidence));
        Assert.AreEqual("source", fixture.Read(fixture.Pair.Source.Path));
        fixture.Pair.Target.Dispose();
        _ = Assert.ThrowsExactly<ObjectDisposedException>(owner.AdmitAfterTargetMutation);
        Assert.AreEqual(TransferPathObservation.LastKnownPath, Assert.IsInstanceOfType<IdentifiedTransferArtifact>(owner.Receipt).Artifact.PathObservation);
    }

    /// <summary>Proves post-target artifact checks still reject an empty named stream or alias created after their final path observation.</summary>
    [TestMethod]
    [DataRow("stream")]
    [DataRow("alias")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void AdmitAfterTargetMutationWhenFinalPathIntroducesUnsupportedArtifactRefusesIt(string defect)
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        WindowsTransferArtifactNames names = WindowsTransferArtifactNames.Create(fixture.Pair.Target.Path, Guid.Empty);
        bool armed = false;
        WindowsTransferArtifactQueries queries = new(WindowsTransferArtifactQueries.Native.ReadIdentity, WindowsFileIdentifier.ReadLocalFacts, (handle, path) =>
        {
            WindowsTransferPathObservation.Require(handle, path);
            if (armed)
            {
                armed = false;
                _ = defect == "stream" ? fixture.Root.WriteFile(Path.GetFileName(path.CanonicalText) + ":Zone.Identifier", string.Empty)
                    : fixture.Root.CreateHardLink("alias", Path.GetFileName(path.CanonicalText));
            }
        });
        using WindowsTransferArtifactOwner owner = WindowsTransferArtifactOwner.Prepare(TransferArtifactRole.PreparedContent, names.PreparedContent, fixture.Pair.Target, queries);
        owner.CreateNative(security);
        FileObjectIdentity identity = owner.AdmitBeforeTargetMutation().ObjectIdentity;
        RandomAccess.SetLength(fixture.Pair.Target.Handle, 0);
        armed = true;

        _ = Assert.Throws<IOException>(owner.AdmitAfterTargetMutation);
        Assert.IsFalse(armed);
        IdentifiedTransferArtifact receipt = Assert.IsInstanceOfType<IdentifiedTransferArtifact>(owner.Receipt);
        Assert.AreEqual(identity, receipt.Artifact.ObjectIdentity);
        Assert.AreEqual(TransferPathObservation.LastKnownPath, receipt.Artifact.PathObservation);
    }
}
