using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves native artifact lifetimes retain verified bytes and monotonic recovery flush observations.</summary>
[TestClass]
public sealed class WindowsTransferArtifactContentReceiptTests
{
    /// <summary>Proves renewed read-only verification retains historical content while any attempt to rewrite its established baseline refuses.</summary>
    [TestMethod]
    public void InvalidatePathObservationWhenContentWasVerifiedPreservesBaselineAndRefusesRewrite()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        using WindowsTransferPathLease parent = OpenParent(root);
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        using WindowsTransferArtifactOwner owner = WindowsTransferArtifactOwner.Prepare(TransferArtifactRole.OriginalBackup, Parse(root.Resolve("artifact")), parent, WindowsTransferArtifactQueries.Native);
        owner.CreateNative(security);
        FileContentEvidence content = WindowsTransferRecoveryTestFixture.Mapping().Original;
        owner.RecordVerifiedContent(content);
        IdentifiedTransferArtifact before = Known(owner);

        owner.InvalidatePathObservation();
        IdentifiedTransferArtifact after = Known(owner);
        Assert.AreEqual(before.Content, after.Content);
        Assert.AreEqual(TransferPathObservation.LastKnownPath, after.Artifact.PathObservation);
        Assert.AreEqual(before.Artifact.ObjectIdentity, after.Artifact.ObjectIdentity);
        _ = Assert.ThrowsExactly<InvalidOperationException>(owner.InvalidateContent);
        _ = Assert.ThrowsExactly<InvalidOperationException>(() => owner.RecordVerifiedContent(content));
        Assert.AreSame(after, owner.Receipt);
    }

    /// <summary>Proves flush failures or later disposal cannot forget the complete mapping frame, and successful acknowledgement cannot be downgraded or rewritten.</summary>
    [TestMethod]
    [DataRow("pending")]
    [DataRow("unconfirmed")]
    [DataRow("confirmed")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public void RecordInitialFrameWhenFlushAdvancesPreservesFrameAndRefusesRewrites(string phase)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        using WindowsTransferPathLease parent = OpenParent(root);
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        using WindowsTransferArtifactOwner owner = WindowsTransferArtifactOwner.Prepare(TransferArtifactRole.RecoveryRecord, Parse(root.Resolve("artifact")), parent, WindowsTransferArtifactQueries.Native);
        owner.CreateNative(security);
        RecoveryFrameEvidence frame = WindowsTransferRecoveryCodec.Inspect(WindowsTransferRecoveryCodec.EncodeInitial(WindowsTransferRecoveryTestFixture.Mapping())).Initial;
        _ = Assert.ThrowsExactly<InvalidOperationException>(owner.BeginInitialFlush);
        _ = Assert.ThrowsExactly<InvalidOperationException>(owner.ConfirmInitialFlush);
        owner.InvalidateContent();
        owner.RecordInitialFrame(frame);
        _ = Assert.ThrowsExactly<InvalidOperationException>(owner.ConfirmInitialFlush);
        if (phase != "pending")
        {
            owner.BeginInitialFlush();
        }
        if (phase == "confirmed")
        {
            owner.ConfirmInitialFlush();
        }
        _ = Assert.ThrowsExactly<InvalidOperationException>(owner.InvalidateContent);
        _ = Assert.ThrowsExactly<InvalidOperationException>(() => owner.RecordInitialFrame(frame));
        RecoveryMappingContent before = Assert.IsInstanceOfType<RecoveryMappingContent>(Known(owner).Content);
        owner.Dispose();
        RecoveryMappingContent after = Assert.IsInstanceOfType<RecoveryMappingContent>(Known(owner).Content);
        Assert.AreEqual(before, after);
        Assert.AreEqual(frame, after.InitialFrame.Frame);
        Assert.AreEqual(phase == "pending" ? RecoveryFlushObservation.NotAttempted : phase == "unconfirmed" ? RecoveryFlushObservation.OutcomeUnconfirmed : RecoveryFlushObservation.Confirmed, after.InitialFrame.Flush);
        Assert.AreEqual(RecoveryAppendObservation.NotAttempted, after.Append);
        _ = Assert.ThrowsExactly<ObjectDisposedException>(owner.InvalidateContent);
        parent.Revalidate();
    }

    /// <summary>Proves content roles cannot be substituted and rejected updates preserve prior receipts.</summary>
    [TestMethod]
    [DataRow("ordinary")]
    [DataRow("recovery")]
    public void RecordVerifiedContentWhenRoleDoesNotMatchRefusesWithoutChangingReceipt(string role)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        using WindowsTransferPathLease parent = OpenParent(root);
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        using WindowsTransferArtifactOwner owner = WindowsTransferArtifactOwner.Prepare(role == "ordinary" ? TransferArtifactRole.OriginalBackup : TransferArtifactRole.RecoveryRecord, Parse(root.Resolve("artifact")), parent, WindowsTransferArtifactQueries.Native);
        owner.CreateNative(security);
        IdentifiedTransferArtifact before = Known(owner);
        WindowsTransferRecoveryMapping mapping = WindowsTransferRecoveryTestFixture.Mapping();
        _ = role == "ordinary"
            ? Assert.ThrowsExactly<ArgumentException>(() => owner.RecordInitialFrame(WindowsTransferRecoveryCodec.Inspect(WindowsTransferRecoveryCodec.EncodeInitial(mapping)).Initial))
            : Assert.ThrowsExactly<ArgumentException>(() => owner.RecordVerifiedContent(mapping.Original));
        Assert.AreSame(before, owner.Receipt);
    }

    /// <summary>Proves completion transitions preserve the initial frame, require prior verification and cannot be replayed or downgraded.</summary>
    [TestMethod]
    [DataRow("verified")]
    [DataRow("unconfirmed")]
    [DataRow("confirmed")]
    public void RecordCompletionFrameWhenFlushAdvancesPreservesInitialAndRejectsOutOfOrderChanges(string phase)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        using WindowsTransferPathLease parent = OpenParent(root);
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        using WindowsTransferArtifactOwner owner = WindowsTransferArtifactOwner.Prepare(TransferArtifactRole.RecoveryRecord, Parse(root.Resolve("record")), parent, WindowsTransferArtifactQueries.Native);
        owner.CreateNative(security);
        byte[] initial = WindowsTransferRecoveryCodec.EncodeInitial(WindowsTransferRecoveryTestFixture.Mapping());
        WindowsTransferRecoveryCompleted decoded = Assert.IsInstanceOfType<WindowsTransferRecoveryCompleted>(WindowsTransferRecoveryCodec.Inspect(WindowsTransferRecoveryTestFixture.Join(initial, WindowsTransferRecoveryCodec.EncodeCompletion(initial))));
        _ = Assert.ThrowsExactly<InvalidOperationException>(owner.BeginCompletionAppend);
        owner.RecordInitialFrame(decoded.Initial);
        _ = Assert.ThrowsExactly<InvalidOperationException>(owner.BeginCompletionAppend);
        owner.BeginInitialFlush();
        _ = Assert.ThrowsExactly<InvalidOperationException>(owner.BeginCompletionAppend);
        owner.ConfirmInitialFlush();
        _ = Assert.ThrowsExactly<InvalidOperationException>(() => owner.RecordCompletionFrame(decoded.Completion));
        owner.BeginCompletionAppend();
        _ = Assert.ThrowsExactly<InvalidOperationException>(owner.BeginCompletionFlush);
        _ = Assert.ThrowsExactly<InvalidOperationException>(owner.ConfirmCompletionFlush);
        owner.RecordCompletionFrame(decoded.Completion);
        _ = Assert.ThrowsExactly<InvalidOperationException>(owner.ConfirmCompletionFlush);
        if (phase != "verified")
        {
            owner.BeginCompletionFlush();
        }
        if (phase == "confirmed")
        {
            owner.ConfirmCompletionFlush();
        }
        IdentifiedTransferArtifact before = Known(owner);
        _ = Assert.ThrowsExactly<InvalidOperationException>(owner.BeginCompletionAppend);
        _ = Assert.ThrowsExactly<InvalidOperationException>(() => owner.RecordCompletionFrame(decoded.Completion));
        Assert.AreSame(before, owner.Receipt);
        RecoveryMappingContent saved = Assert.IsInstanceOfType<RecoveryMappingContent>(before.Content);
        Assert.AreEqual(decoded.Initial, saved.InitialFrame.Frame);
        Assert.AreEqual(RecoveryFlushObservation.Confirmed, saved.InitialFrame.Flush);
        Assert.AreEqual(decoded.Completion, Assert.IsInstanceOfType<VerifiedRecoveryAppend>(saved.Append).Frame.Frame);
    }

    private static WindowsTransferPathLease OpenParent(TestOwnedTemporaryRoot root)
    {
        return WindowsTransferPathLease.Open(Parse(root.WriteFile("anchor", "original")), FileAccess.Read, new WindowsDriveBoundary());
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
