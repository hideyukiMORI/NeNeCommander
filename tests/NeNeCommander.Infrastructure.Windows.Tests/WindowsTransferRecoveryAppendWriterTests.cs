using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves recovery append binds the first exact mapping, supports bounded short reads and refuses prior corruption.</summary>
[TestClass]
public sealed class WindowsTransferRecoveryAppendWriterTests
{
    /// <summary>Proves multibuffer initial mappings are preserved while the post-content parent check admits the same target object.</summary>
    [TestMethod]
    public async Task AppendCompletionAsyncWhenInitialSpansBuffersPreservesExactPrefixAfterTargetChangeAsync()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsTransferRecoveryMapping mapping = WindowsTransferRecoveryTestFixture.Mapping(@"D:\" + new string('日', 20000), fixture.Pair.Target.Path.CanonicalText);
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        using WindowsTransferArtifactOwner owner = WindowsTransferArtifactOwner.Prepare(TransferArtifactRole.RecoveryRecord, mapping.Names.Recovery, fixture.Pair.Target, WindowsTransferArtifactQueries.Native);
        owner.CreateNative(security);
        _ = await WindowsTransferRecoveryWriter.WriteInitialAsync(owner, mapping, WindowsTransferContentIo.Native, CancellationToken.None);
        RecoveryMappingContent initial = Saved(owner);
        WindowsOverwriteCompletionFault calls = new("none");
        calls.Arm(initial.InitialFrame.Frame);
        byte[] alteredTarget = [1, 2];
        RandomAccess.Write(fixture.Pair.Target.Handle, alteredTarget, 0);
        RandomAccess.SetLength(fixture.Pair.Target.Handle, 2);

        _ = await WindowsTransferRecoveryWriter.AppendCompletionAsync(owner, mapping, calls.Io, CancellationToken.None);
        Assert.IsTrue(calls.Reads > 10);
        Assert.AreEqual(1, calls.Writes);
        Assert.AreEqual(1, calls.Flushes);
        byte[] prefix = WindowsTransferRecoveryCodec.EncodeInitial(mapping);
        byte[] bytes = Encoding.UTF8.GetBytes(fixture.Read(mapping.Names.Recovery));
        CollectionAssert.AreEqual(prefix, bytes[..prefix.Length]);
        WindowsTransferRecoveryCompleted decoded = Assert.IsInstanceOfType<WindowsTransferRecoveryCompleted>(WindowsTransferRecoveryCodec.Inspect(bytes));
        Assert.AreEqual(mapping, decoded.Mapping);
        Assert.AreEqual(initial.InitialFrame, Saved(owner).InitialFrame);
        Assert.AreEqual(decoded.Completion, Assert.IsInstanceOfType<VerifiedRecoveryAppend>(Saved(owner).Append).Frame.Frame);
        _ = await Assert.ThrowsAsync<IOException>(() => WindowsTransferRecoveryWriter.AppendCompletionAsync(owner, mapping, calls.Io, CancellationToken.None));
        Assert.AreEqual(1, calls.Writes);
    }

    /// <summary>Proves initial content, EOF, confirmation and exact mapping evidence must match before any append call.</summary>
    [TestMethod]
    [DataRow("prefix")]
    [DataRow("extra")]
    [DataRow("foreignMapping")]
    [DataRow("unconfirmed")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task AppendCompletionAsyncWhenInitialAdmissionDiffersRefusesBeforeWriteAsync(string defect)
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsTransferRecoveryMapping mapping = WindowsTransferRecoveryTestFixture.Mapping(fixture.Pair.Source.Path.CanonicalText, fixture.Pair.Target.Path.CanonicalText);
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        using WindowsTransferArtifactOwner owner = WindowsTransferArtifactOwner.Prepare(TransferArtifactRole.RecoveryRecord, mapping.Names.Recovery, fixture.Pair.Target, WindowsTransferArtifactQueries.Native);
        owner.CreateNative(security);
        await EstablishInitialAsync(owner, mapping, defect);
        RecoveryMappingContent initial = Saved(owner);
        WindowsOverwriteCompletionFault calls = new("none");
        calls.Arm(initial.InitialFrame.Frame);
        WindowsTransferRecoveryMapping requested = defect == "foreignMapping"
            ? WindowsTransferRecoveryTestFixture.Mapping(fixture.Root.Resolve("foreign"), fixture.Pair.Target.Path.CanonicalText) : mapping;
        string before = fixture.Read(mapping.Names.Recovery);

        _ = await Assert.ThrowsAsync<IOException>(() => WindowsTransferRecoveryWriter.AppendCompletionAsync(owner, requested, calls.Io, CancellationToken.None));
        Assert.AreEqual(0, calls.Writes);
        Assert.AreEqual(0, calls.Flushes);
        Assert.AreEqual(initial, Saved(owner));
        Assert.AreEqual(before, fixture.Read(mapping.Names.Recovery));
        Assert.AreEqual("target", fixture.Read(fixture.Pair.Target.Path));
    }

    private static async Task EstablishInitialAsync(WindowsTransferArtifactOwner owner, WindowsTransferRecoveryMapping mapping, string defect)
    {
        WindowsTransferContentIo io = new(RandomAccess.ReadAsync, RandomAccess.WriteAsync, RandomAccess.SetLength, _ => throw new IOException("Injected initial flush failure."));
        if (defect == "unconfirmed")
        {
            _ = await Assert.ThrowsAsync<IOException>(() => WindowsTransferRecoveryWriter.WriteInitialAsync(owner, mapping, io, CancellationToken.None));
            return;
        }
        _ = await WindowsTransferRecoveryWriter.WriteInitialAsync(owner, mapping, WindowsTransferContentIo.Native, CancellationToken.None);
        if (defect == "prefix")
        {
            byte[] changedPrefix = [0];
            RandomAccess.Write(owner.Handle, changedPrefix, 0);
        }
        if (defect == "extra")
        {
            byte[] extra = [42];
            RandomAccess.Write(owner.Handle, extra, RandomAccess.GetLength(owner.Handle));
        }
    }

    private static RecoveryMappingContent Saved(WindowsTransferArtifactOwner owner)
    {
        return Assert.IsInstanceOfType<RecoveryMappingContent>(Assert.IsInstanceOfType<IdentifiedTransferArtifact>(owner.Receipt).Content);
    }
}
