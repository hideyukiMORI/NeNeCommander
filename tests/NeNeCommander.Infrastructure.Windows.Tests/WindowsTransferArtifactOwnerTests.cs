using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves one-shot native artifact ownership retains every learned fact across fallible observation and disposal.</summary>
[TestClass]
public sealed class WindowsTransferArtifactOwnerTests
{
    /// <summary>Proves each native query sees the preceding canonical receipt before it can fail.</summary>
    [TestMethod]
    public void CreateNativeWhenQueriesRunRecordsPossibleCreationThenStableThenFullIdentity()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath anchor = Parse(root.WriteFile("anchor.txt", "anchor"));
        WindowsLocalPath candidate = Parse(root.Resolve("artifact.tmp"));
        using WindowsTransferPathLease parent = WindowsTransferPathLease.Open(anchor, FileAccess.Read, new WindowsDriveBoundary());
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        WindowsTransferArtifactOwner? observedOwner = null;
        List<string> order = [];
        WindowsTransferArtifactQueries queries = new(handle =>
        {
            _ = Assert.IsInstanceOfType<UnidentifiedTransferArtifact>(observedOwner?.Receipt);
            order.Add("identity");
            return WindowsTransferArtifactQueries.Native.ReadIdentity(handle);
        }, handle =>
        {
            IdentifiedTransferArtifact known = Assert.IsInstanceOfType<IdentifiedTransferArtifact>(observedOwner?.Receipt);
            Assert.AreEqual(ArtifactFullIdentityObservation.NotObserved, known.Artifact.FullIdentity);
            order.Add("facts");
            return WindowsFileIdentifier.ReadLocalFacts(handle);
        }, (handle, path) =>
        {
            IdentifiedTransferArtifact known = Assert.IsInstanceOfType<IdentifiedTransferArtifact>(observedOwner?.Receipt);
            _ = Assert.IsInstanceOfType<ObservedArtifactFullIdentity>(known.Artifact.FullIdentity);
            Assert.AreEqual(TransferPathObservation.LastKnownPath, known.Artifact.PathObservation);
            order.Add("path");
            WindowsTransferPathObservation.Require(handle, path);
        });
        using WindowsTransferArtifactOwner owner = WindowsTransferArtifactOwner.Prepare(TransferArtifactRole.OriginalBackup, candidate, parent, queries);
        observedOwner = owner;
        Assert.IsNull(owner.Receipt);
        owner.CreateNative(security);
        Assert.AreEqual("identity,facts,path", string.Join(',', order));
        Assert.AreEqual(TransferPathObservation.CurrentPathVerified, Known(owner).Artifact.PathObservation);
    }

    /// <summary>Proves query failure retains the live created object and all knowledge established before that failure.</summary>
    [TestMethod]
    [DataRow("identity")]
    [DataRow("facts")]
    [DataRow("invalidFacts")]
    [DataRow("mismatchFacts")]
    [DataRow("path")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void CreateNativeWhenObservationFailsRetainsHandleAndPartialReceipt(string phase)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath anchor = Parse(root.WriteFile("anchor.txt", "anchor"));
        WindowsLocalPath candidate = Parse(root.Resolve("artifact.tmp"));
        using WindowsTransferPathLease parent = WindowsTransferPathLease.Open(anchor, FileAccess.Read, new WindowsDriveBoundary());
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        string failure = phase;
        using WindowsTransferArtifactOwner owner = WindowsTransferArtifactOwner.Prepare(TransferArtifactRole.OriginalBackup, candidate, parent, Queries(() => failure));

        _ = Assert.Throws<IOException>(() => owner.CreateNative(security));
        Assert.IsFalse(owner.Handle.IsClosed);
        Assert.AreEqual(0L, RandomAccess.GetLength(owner.Handle));
        if (phase == "identity")
        {
            Assert.AreEqual(candidate, Assert.IsInstanceOfType<UnidentifiedTransferArtifact>(owner.Receipt).CandidatePath);
        }
        else
        {
            IdentifiedTransferArtifact known = Known(owner);
            Assert.AreEqual(WindowsTransferArtifactQueries.Native.ReadIdentity(owner.Handle), known.Artifact.ObjectIdentity);
            Assert.AreEqual(phase == "path", known.Artifact.FullIdentity is ObservedArtifactFullIdentity);
            Assert.AreEqual(TransferPathObservation.LastKnownPath, known.Artifact.PathObservation);
        }
        _ = Assert.ThrowsExactly<InvalidOperationException>(() => owner.CreateNative(security));
        failure = string.Empty;
        owner.ObserveBeforeTargetMutation();
        Assert.AreEqual(TransferPathObservation.CurrentPathVerified, Known(owner).Artifact.PathObservation);
        Assert.AreEqual("anchor", File.ReadAllText(anchor.CanonicalText));
    }

    /// <summary>Proves failed re-observation and contradictory identities never erase established evidence or leave a current-path claim.</summary>
    [TestMethod]
    [DataRow("identity")]
    [DataRow("facts")]
    [DataRow("invalidFacts")]
    [DataRow("mismatchIdentity")]
    [DataRow("mismatchFacts")]
    [DataRow("path")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void ObserveBeforeTargetMutationWhenLaterQueryFailsRetainsKnownIdentities(string phase)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath anchor = Parse(root.WriteFile("anchor.txt", "anchor"));
        WindowsLocalPath candidate = Parse(root.Resolve("artifact.tmp"));
        using WindowsTransferPathLease parent = WindowsTransferPathLease.Open(anchor, FileAccess.Read, new WindowsDriveBoundary());
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        string failure = string.Empty;
        using WindowsTransferArtifactOwner owner = WindowsTransferArtifactOwner.Prepare(TransferArtifactRole.PreparedContent, candidate, parent, Queries(() => failure));
        owner.CreateNative(security);
        IdentifiedTransferArtifact before = Known(owner);
        failure = phase;

        _ = Assert.Throws<IOException>(owner.ObserveBeforeTargetMutation);
        IdentifiedTransferArtifact after = Known(owner);
        Assert.AreEqual(before.Artifact.ObjectIdentity, after.Artifact.ObjectIdentity);
        Assert.AreEqual(before.Artifact.FullIdentity, after.Artifact.FullIdentity);
        Assert.AreEqual(TransferPathObservation.LastKnownPath, after.Artifact.PathObservation);
        Assert.AreEqual(ArtifactContentObservation.Unknown, after.Content);
        Assert.AreEqual(ArtifactLinkObservation.Retained, after.Link);
        Assert.AreEqual(TransferPathObservation.CurrentPathVerified, before.Artifact.PathObservation);
    }

    /// <summary>Proves a native collision keeps only the attempted candidate, cannot be retried, and never changes the foreign file.</summary>
    [TestMethod]
    public void CreateNativeWhenCandidateExistsPreservesForeignEntryAndForbidsRetry()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath anchor = Parse(root.WriteFile("anchor.txt", "anchor"));
        WindowsLocalPath candidate = Parse(root.WriteFile("artifact.tmp", "foreign"));
        string identity = WindowsFileIdentifier.Describe(candidate.CanonicalText);
        using WindowsTransferPathLease parent = WindowsTransferPathLease.Open(anchor, FileAccess.Read, new WindowsDriveBoundary());
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        using WindowsTransferArtifactOwner owner = WindowsTransferArtifactOwner.Prepare(TransferArtifactRole.RecoveryRecord, candidate, parent, WindowsTransferArtifactQueries.Native);

        _ = Assert.Throws<IOException>(() => owner.CreateNative(security));
        _ = Assert.ThrowsExactly<InvalidOperationException>(() => owner.CreateNative(security));
        _ = Assert.ThrowsExactly<InvalidOperationException>(() => _ = owner.Handle);
        owner.Dispose();
        Assert.AreEqual(candidate, Assert.IsInstanceOfType<UnidentifiedTransferArtifact>(owner.Receipt).CandidatePath);
        Assert.AreEqual(identity, WindowsFileIdentifier.Describe(candidate.CanonicalText));
        Assert.AreEqual("foreign", File.ReadAllText(candidate.CanonicalText));
        parent.Revalidate();
    }

    /// <summary>Proves disposal closes only the owned handle, retains a readable snapshot and does not remove content or dispose the parent.</summary>
    [TestMethod]
    public void DisposeWhenArtifactExistsRetainsFileAndReceiptAndBorrowedParent()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath anchor = Parse(root.WriteFile("anchor.txt", "anchor"));
        WindowsLocalPath candidate = Parse(root.Resolve("artifact.tmp"));
        using WindowsTransferPathLease parent = WindowsTransferPathLease.Open(anchor, FileAccess.Read, new WindowsDriveBoundary());
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        using WindowsTransferArtifactOwner owner = WindowsTransferArtifactOwner.Prepare(TransferArtifactRole.RecoveryRecord, candidate, parent, WindowsTransferArtifactQueries.Native);
        owner.CreateNative(security);
        SafeFileHandle handle = owner.Handle;
        RandomAccess.Write(handle, [1, 2, 3], 0);
        owner.ObserveBeforeTargetMutation();
        IdentifiedTransferArtifact before = Known(owner);

        owner.Dispose();
        owner.Dispose();
        Assert.IsTrue(handle.IsClosed);
        Assert.AreEqual(before.Artifact.FullIdentity, Known(owner).Artifact.FullIdentity);
        Assert.AreEqual(TransferPathObservation.LastKnownPath, Known(owner).Artifact.PathObservation);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, File.ReadAllBytes(candidate.CanonicalText));
        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => owner.CreateNative(security));
        _ = Assert.ThrowsExactly<ObjectDisposedException>(owner.ObserveBeforeTargetMutation);
        parent.Revalidate();
    }

    /// <summary>Proves the owner can be disposed before any attempt without inventing an artifact receipt.</summary>
    [TestMethod]
    public void DisposeWhenCreationHasNotStartedLeavesNoArtifactOrReceipt()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath anchor = Parse(root.WriteFile("anchor.txt", "anchor"));
        WindowsLocalPath candidate = Parse(root.Resolve("artifact.tmp"));
        using WindowsTransferPathLease parent = WindowsTransferPathLease.Open(anchor, FileAccess.Read, new WindowsDriveBoundary());
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        using WindowsTransferArtifactOwner owner = WindowsTransferArtifactOwner.Prepare(TransferArtifactRole.OriginalBackup, candidate, parent, WindowsTransferArtifactQueries.Native);

        owner.Dispose();
        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => owner.CreateNative(security));
        Assert.IsNull(owner.Receipt);
        Assert.IsFalse(File.Exists(candidate.CanonicalText));
    }

    /// <summary>Proves failed parent validation cannot discard complete identity or preserve a current-path claim.</summary>
    [TestMethod]
    public void ObserveBeforeTargetMutationWhenBorrowedParentIsDisposedRetainsEvidence()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath anchor = Parse(root.WriteFile("anchor.txt", "anchor"));
        WindowsLocalPath candidate = Parse(root.Resolve("artifact.tmp"));
        using WindowsTransferPathLease parent = WindowsTransferPathLease.Open(anchor, FileAccess.Read, new WindowsDriveBoundary());
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        using WindowsTransferArtifactOwner owner = WindowsTransferArtifactOwner.Prepare(TransferArtifactRole.OriginalBackup, candidate, parent, WindowsTransferArtifactQueries.Native);
        owner.CreateNative(security);
        IdentifiedTransferArtifact before = Known(owner);
        parent.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(owner.ObserveBeforeTargetMutation);
        Assert.AreEqual(before.Artifact.FullIdentity, Known(owner).Artifact.FullIdentity);
        Assert.AreEqual(TransferPathObservation.LastKnownPath, Known(owner).Artifact.PathObservation);
        Assert.IsFalse(owner.Handle.IsClosed);
    }

    private static WindowsTransferArtifactQueries Queries(Func<string> failure)
    {
        return new WindowsTransferArtifactQueries(handle => ReadIdentity(handle, failure()),
            handle => ReadFacts(handle, failure()), (handle, path) =>
            {
                if (failure() == "path")
                {
                    throw new IOException("Injected path observation failure.");
                }
                WindowsTransferPathObservation.Require(handle, path);
            });
    }

    private static FileObjectIdentity ReadIdentity(SafeFileHandle handle, string failure)
    {
        return failure == "identity" ? throw new IOException("Injected identity query failure.")
            : failure == "mismatchIdentity" ? FileObjectIdentity.Create("foreign")
            : WindowsTransferArtifactQueries.Native.ReadIdentity(handle);
    }

    private static WindowsLocalHandleFacts ReadFacts(SafeFileHandle handle, string failure)
    {
        if (failure == "facts")
        {
            throw new IOException("Injected metadata query failure.");
        }
        WindowsLocalHandleFacts facts = WindowsFileIdentifier.ReadLocalFacts(handle);
        return failure switch
        {
            "mismatchFacts" => new WindowsLocalHandleFacts(FileObjectIdentity.Create("foreign"), facts.Basic, facts.Standard, facts.FileSystemName),
            "invalidFacts" => new WindowsLocalHandleFacts(facts.ObjectIdentity,
                new WindowsLocalBasicFacts(long.MaxValue, facts.Basic.LastWriteFileTimeUtc, facts.Basic.ChangeFileTimeUtc, facts.Basic.Attributes), facts.Standard, facts.FileSystemName),
            _ => facts,
        };
    }

    private static IdentifiedTransferArtifact Known(WindowsTransferArtifactOwner owner)
    {
        return Assert.IsInstanceOfType<IdentifiedTransferArtifact>(owner.Receipt);
    }

    private static WindowsLocalPath Parse(string path)
    {
        return Assert.IsInstanceOfType<WindowsLocalPath>(Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(path)).Path);
    }
}
