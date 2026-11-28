using System;
using System.IO;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves fresh owned-artifact admission and retained content knowledge at real native boundaries.</summary>
[TestClass]
public sealed class WindowsTransferArtifactAdmissionTests
{
    /// <summary>Proves ordinary own writes refresh full metadata while keeping stable identity and saved byte evidence across a later query failure.</summary>
    [TestMethod]
    public void AdmitBeforeTargetMutationWhenOwnContentChangesRefreshesIdentityAndRetainsEvidence()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath anchor = Parse(root.WriteFile("anchor", "original"));
        using WindowsTransferPathLease parent = WindowsTransferPathLease.Open(anchor, FileAccess.Read, new WindowsDriveBoundary());
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        bool failPath = false;
        WindowsTransferArtifactQueries queries = new(WindowsTransferArtifactQueries.Native.ReadIdentity, WindowsFileIdentifier.ReadLocalFacts,
            (handle, path) =>
            {
                if (failPath)
                {
                    throw new IOException("Injected path failure after verified content.");
                }
                WindowsTransferPathObservation.Require(handle, path);
            });
        using WindowsTransferArtifactOwner owner = WindowsTransferArtifactOwner.Prepare(TransferArtifactRole.OriginalBackup, Parse(root.Resolve("artifact")), parent, queries);
        owner.CreateNative(security);
        VerifiedEntrySnapshot before = owner.AdmitBeforeTargetMutation();
        owner.InvalidateContent();
        RandomAccess.Write(owner.Handle, [1, 2, 3], 0);
        FileContentEvidence content = FileContentEvidence.Create((KnownEntrySize)EntrySize.Create(3), FileContentHash.Create(Convert.ToHexString(SHA256.HashData([1, 2, 3]))));
        owner.RecordVerifiedContent(content);
        VerifiedEntrySnapshot after = owner.AdmitBeforeTargetMutation();
        Assert.AreEqual(before.ObjectIdentity, after.ObjectIdentity);
        Assert.AreNotEqual(before.Identity, after.Identity);
        failPath = true;

        _ = Assert.Throws<IOException>(owner.AdmitBeforeTargetMutation);
        IdentifiedTransferArtifact receipt = Assert.IsInstanceOfType<IdentifiedTransferArtifact>(owner.Receipt);
        Assert.AreEqual(content, Assert.IsInstanceOfType<VerifiedArtifactContent>(receipt.Content).Content);
        Assert.AreEqual(after.Identity, Assert.IsInstanceOfType<ObservedArtifactFullIdentity>(receipt.Artifact.FullIdentity).Identity);
        Assert.AreEqual(TransferPathObservation.LastKnownPath, receipt.Artifact.PathObservation);
        Assert.AreEqual("original", File.ReadAllText(anchor.CanonicalText));
    }

    /// <summary>Proves the final path check cannot hide a late empty stream, alias or forbidden attribute.</summary>
    [TestMethod]
    [DataRow("emptyStream")]
    [DataRow("hardlink")]
    [DataRow("hidden")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void AdmitBeforeTargetMutationWhenLastPathCheckIntroducesUnsupportedShapeRefusesIt(string defect)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath anchor = Parse(root.WriteFile("anchor", "original"));
        using WindowsTransferPathLease parent = WindowsTransferPathLease.Open(anchor, FileAccess.Read, new WindowsDriveBoundary());
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        bool armed = false;
        WindowsTransferArtifactQueries queries = new(WindowsTransferArtifactQueries.Native.ReadIdentity, WindowsFileIdentifier.ReadLocalFacts,
            (handle, path) =>
            {
                WindowsTransferPathObservation.Require(handle, path);
                if (armed)
                {
                    armed = false;
                    ChangeArtifact(root, defect);
                }
            });
        using WindowsTransferArtifactOwner owner = WindowsTransferArtifactOwner.Prepare(TransferArtifactRole.PreparedContent, Parse(root.Resolve("artifact")), parent, queries);
        owner.CreateNative(security);
        FileObjectIdentity identity = owner.AdmitBeforeTargetMutation().ObjectIdentity;
        armed = true;

        _ = Assert.Throws<IOException>(owner.AdmitBeforeTargetMutation);
        Assert.IsFalse(armed);
        IdentifiedTransferArtifact receipt = Assert.IsInstanceOfType<IdentifiedTransferArtifact>(owner.Receipt);
        Assert.AreEqual(identity, receipt.Artifact.ObjectIdentity);
        Assert.AreEqual(TransferPathObservation.LastKnownPath, receipt.Artifact.PathObservation);
        Assert.IsFalse(owner.Handle.IsClosed);
    }

    private static void ChangeArtifact(TestOwnedTemporaryRoot root, string defect)
    {
        switch (defect)
        {
            case "emptyStream":
                _ = root.WriteFile("artifact:Zone.Identifier", string.Empty);
                break;
            case "hardlink":
                _ = root.CreateHardLink("alias", "artifact");
                break;
            case "hidden":
                File.SetAttributes(root.Resolve("artifact"), FileAttributes.Hidden);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(defect));
        }
    }

    private static WindowsLocalPath Parse(string path)
    {
        return Assert.IsInstanceOfType<WindowsLocalPath>(Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(path)).Path);
    }
}
