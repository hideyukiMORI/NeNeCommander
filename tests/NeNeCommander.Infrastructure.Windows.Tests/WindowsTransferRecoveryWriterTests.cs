using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves bounded initial recovery writing and exact readback through the existing protected native artifact owner.</summary>
[TestClass]
public sealed class WindowsTransferRecoveryWriterTests
{
    /// <summary>Proves multi-buffer frames and short reads preserve exact encoded bytes, EOF and native flush acknowledgement.</summary>
    [TestMethod]
    public async Task WriteInitialAsyncWhenFrameExceedsBufferWritesAndReadsBoundedChunksAsync()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsTransferRecoveryMapping mapping = WindowsTransferRecoveryTestFixture.Mapping(@"D:\" + new string('日', 20000), fixture.Pair.Target.Path.CanonicalText);
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        using WindowsTransferArtifactOwner owner = WindowsTransferArtifactOwner.Prepare(TransferArtifactRole.RecoveryRecord, mapping.Names.Recovery, fixture.Pair.Target, WindowsTransferArtifactQueries.Native);
        owner.CreateNative(security);
        int writes = 0;
        int reads = 0;
        WindowsTransferContentIo io = new((handle, bytes, offset, token) =>
        {
            Assert.IsLessThanOrEqualTo(65536, bytes.Length);
            reads++;
            return RandomAccess.ReadAsync(handle, bytes[..Math.Min(4093, bytes.Length)], offset, token);
        }, (handle, bytes, offset, token) =>
        {
            Assert.IsLessThanOrEqualTo(65536, bytes.Length);
            writes++;
            return RandomAccess.WriteAsync(handle, bytes, offset, token);
        }, RandomAccess.SetLength, RandomAccess.FlushToDisk);

        VerifiedEntrySnapshot entry = await WindowsTransferRecoveryWriter.WriteInitialAsync(owner, mapping, io, CancellationToken.None);
        await WindowsTransferRecoveryWriter.RequireInitialAsync(owner, mapping, io, CancellationToken.None);
        Assert.IsTrue(writes > 1 && reads > writes);
        Assert.AreEqual(entry.ObjectIdentity, owner.AdmitBeforeTargetMutation().ObjectIdentity);
        RecoveryMappingContent saved = Assert.IsInstanceOfType<RecoveryMappingContent>(Assert.IsInstanceOfType<IdentifiedTransferArtifact>(owner.Receipt).Content);
        Assert.AreEqual(RecoveryFlushObservation.Confirmed, saved.InitialFrame.Flush);
        Assert.AreEqual(WindowsTransferRecoveryCodec.Inspect(WindowsTransferRecoveryCodec.EncodeInitial(mapping)).Initial, saved.InitialFrame.Frame);
        Assert.AreEqual("target", fixture.Read(fixture.Pair.Target.Path));
    }

    /// <summary>Proves readback mismatch or either EOF defect leaves unverified content and never reaches flush.</summary>
    [TestMethod]
    [DataRow("corrupt")]
    [DataRow("short")]
    [DataRow("extra")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task WriteInitialAsyncWhenReadbackIsIncompleteOrDifferentRetainsUnknownAsync(string defect)
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsTransferRecoveryMapping mapping = WindowsTransferRecoveryTestFixture.Mapping(fixture.Pair.Source.Path.CanonicalText, fixture.Pair.Target.Path.CanonicalText);
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        using WindowsTransferArtifactOwner owner = WindowsTransferArtifactOwner.Prepare(TransferArtifactRole.RecoveryRecord, mapping.Names.Recovery, fixture.Pair.Target, WindowsTransferArtifactQueries.Native);
        owner.CreateNative(security);
        int flushes = 0;
        WindowsTransferContentIo io = new(async (handle, bytes, offset, token) =>
        {
            int count = await RandomAccess.ReadAsync(handle, bytes, offset, token);
            if (defect == "corrupt" && count > 0)
            {
                bytes.Span[0] = 0;
            }
            return defect == "short" ? 0 : count;
        }, RandomAccess.WriteAsync, (handle, length) => RandomAccess.SetLength(handle, defect == "extra" ? length + 1 : length), _ => flushes++);

        _ = await Assert.ThrowsAsync<IOException>(() => WindowsTransferRecoveryWriter.WriteInitialAsync(owner, mapping, io, CancellationToken.None));
        Assert.AreEqual(0, flushes);
        IdentifiedTransferArtifact receipt = Assert.IsInstanceOfType<IdentifiedTransferArtifact>(owner.Receipt);
        Assert.AreEqual(ArtifactContentObservation.Unknown, receipt.Content);
        Assert.AreEqual(TransferPathObservation.LastKnownPath, receipt.Artifact.PathObservation);
        Assert.IsTrue(RandomAccess.GetLength(owner.Handle) > 0);
    }

    /// <summary>Proves a wrong role or candidate cannot be written as the recovery record.</summary>
    [TestMethod]
    [DataRow("role")]
    [DataRow("path")]
    public async Task WriteInitialAsyncWhenOwnerDoesNotMatchRecordRefusesBeforeWriteAsync(string defect)
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsTransferRecoveryMapping mapping = WindowsTransferRecoveryTestFixture.Mapping(fixture.Pair.Source.Path.CanonicalText, fixture.Pair.Target.Path.CanonicalText);
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        using WindowsTransferArtifactOwner owner = WindowsTransferArtifactOwner.Prepare(defect == "role" ? TransferArtifactRole.OriginalBackup : TransferArtifactRole.RecoveryRecord,
            defect == "path" ? mapping.Names.OriginalBackup : mapping.Names.Recovery, fixture.Pair.Target, WindowsTransferArtifactQueries.Native);
        owner.CreateNative(security);

        _ = await Assert.ThrowsAsync<IOException>(() => WindowsTransferRecoveryWriter.WriteInitialAsync(owner, mapping, WindowsTransferContentIo.Native, CancellationToken.None));
        Assert.AreEqual(0L, RandomAccess.GetLength(owner.Handle));
        Assert.AreEqual(ArtifactContentObservation.Unknown, Assert.IsInstanceOfType<IdentifiedTransferArtifact>(owner.Receipt).Content);
    }
}
