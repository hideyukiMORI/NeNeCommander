using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves each recovery-writer entry observes its own cancellation boundary rather than relying on a later attempt stage.</summary>
[TestClass]
public sealed class WindowsTransferWriterCancellationTests
{
    /// <summary>Native initial-flush acknowledgement is retained even when cancellation forbids returning a successful writer snapshot.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task WriteInitialAsyncWhenFlushAcknowledgementCancelsRetainsConfirmedFrameAndStopsAsync()
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsTransferRecoveryMapping mapping = WindowsTransferRecoveryTestFixture.Mapping(files.Pair.Source.Path.CanonicalText, files.Pair.Target.Path.CanonicalText);
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        using WindowsTransferArtifactOwner owner = WindowsTransferArtifactOwner.Prepare(TransferArtifactRole.RecoveryRecord,
            mapping.Names.Recovery, files.Pair.Target, WindowsTransferArtifactQueries.Native);
        owner.CreateNative(security);
        using CancellationTokenSource cancellation = new();
        int flushes = 0;
        WindowsTransferContentIo io = new(RandomAccess.ReadAsync, RandomAccess.WriteAsync, RandomAccess.SetLength, handle =>
        {
            RandomAccess.FlushToDisk(handle);
            flushes++;
            cancellation.Cancel();
        });

        _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(() =>
            WindowsTransferRecoveryWriter.WriteInitialAsync(owner, mapping, io, cancellation.Token));

        Assert.AreEqual(1, flushes);
        RecoveryMappingContent saved = Assert.IsInstanceOfType<RecoveryMappingContent>(Assert.IsInstanceOfType<IdentifiedTransferArtifact>(owner.Receipt).Content);
        Assert.AreEqual(RecoveryFlushObservation.Confirmed, saved.InitialFrame.Flush);
        Assert.AreEqual(RecoveryAppendObservation.NotAttempted, saved.Append);
        Assert.AreEqual(WindowsTransferRecoveryCodec.Inspect(WindowsTransferRecoveryCodec.EncodeInitial(mapping)).Initial, saved.InitialFrame.Frame);
        Assert.AreEqual("source", files.Read(files.Pair.Source.Path));
        Assert.AreEqual("target", files.Read(files.Pair.Target.Path));
    }

    /// <summary>An already cancelled writer or checker cannot refresh metadata or invalidate saved evidence before reporting cancellation.</summary>
    [TestMethod]
    [DataRow("initial")]
    [DataRow("requireInitial")]
    [DataRow("append")]
    [DataRow("requireCompleted")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task WriterAsyncWhenAlreadyCancelledDoesNotObserveOrChangeOwnerAsync(string operation)
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsTransferRecoveryMapping mapping = WindowsTransferRecoveryTestFixture.Mapping(files.Pair.Source.Path.CanonicalText, files.Pair.Target.Path.CanonicalText);
        int queries = 0;
        WindowsTransferArtifactQueries native = WindowsTransferArtifactQueries.Native;
        WindowsTransferArtifactQueries observations = new(native.ReadIdentity, handle =>
        {
            queries++;
            return native.ReadFacts(handle);
        }, native.RequirePath);
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        using WindowsTransferArtifactOwner owner = WindowsTransferArtifactOwner.Prepare(TransferArtifactRole.RecoveryRecord,
            mapping.Names.Recovery, files.Pair.Target, observations);
        owner.CreateNative(security);
        await EstablishAsync(owner, mapping, operation);
        TransferArtifactReceipt? previous = owner.Receipt;
        string bytes = files.Read(mapping.Names.Recovery);
        queries = 0;
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => InvokeAsync(owner, mapping, operation, cancellation.Token));

        Assert.AreEqual(0, queries);
        Assert.AreEqual(previous, owner.Receipt);
        Assert.AreEqual(bytes, files.Read(mapping.Names.Recovery));
        Assert.AreEqual("source", files.Read(files.Pair.Source.Path));
        Assert.AreEqual("target", files.Read(files.Pair.Target.Path));
    }

    /// <summary>A completion flush acknowledgement remains confirmed while cancellation stops before another metadata readmission.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task AppendCompletionAsyncWhenFlushAcknowledgementCancelsStopsBeforeMetadataReadmissionAsync()
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsTransferRecoveryMapping mapping = WindowsTransferRecoveryTestFixture.Mapping(files.Pair.Source.Path.CanonicalText, files.Pair.Target.Path.CanonicalText);
        int facts = 0;
        WindowsTransferArtifactQueries native = WindowsTransferArtifactQueries.Native;
        WindowsTransferArtifactQueries observations = new(native.ReadIdentity, handle => { facts++; return native.ReadFacts(handle); }, native.RequirePath);
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        using WindowsTransferArtifactOwner owner = WindowsTransferArtifactOwner.Prepare(TransferArtifactRole.RecoveryRecord,
            mapping.Names.Recovery, files.Pair.Target, observations);
        owner.CreateNative(security);
        _ = await WindowsTransferRecoveryWriter.WriteInitialAsync(owner, mapping, WindowsTransferContentIo.Native, CancellationToken.None);
        RecoveryMappingContent initial = Assert.IsInstanceOfType<RecoveryMappingContent>(Assert.IsInstanceOfType<IdentifiedTransferArtifact>(owner.Receipt).Content);
        using CancellationTokenSource cancellation = new();
        int flushes = 0;
        WindowsTransferContentIo io = new(RandomAccess.ReadAsync, RandomAccess.WriteAsync, RandomAccess.SetLength, handle =>
        {
            RandomAccess.FlushToDisk(handle);
            flushes++;
            facts = 0;
            cancellation.Cancel();
        });

        _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(() =>
            WindowsTransferRecoveryWriter.AppendCompletionAsync(owner, mapping, io, cancellation.Token));

        Assert.AreEqual(1, flushes);
        Assert.AreEqual(0, facts);
        RecoveryMappingContent saved = Assert.IsInstanceOfType<RecoveryMappingContent>(Assert.IsInstanceOfType<IdentifiedTransferArtifact>(owner.Receipt).Content);
        Assert.AreEqual(initial.InitialFrame, saved.InitialFrame);
        Assert.AreEqual(RecoveryFlushObservation.Confirmed, Assert.IsInstanceOfType<VerifiedRecoveryAppend>(saved.Append).Frame.Flush);
        Assert.AreEqual("source", files.Read(files.Pair.Source.Path));
        Assert.AreEqual("target", files.Read(files.Pair.Target.Path));
    }

    private static async Task EstablishAsync(WindowsTransferArtifactOwner owner, WindowsTransferRecoveryMapping mapping, string operation)
    {
        if (operation != "initial")
        {
            _ = await WindowsTransferRecoveryWriter.WriteInitialAsync(owner, mapping, WindowsTransferContentIo.Native, CancellationToken.None);
        }
        if (operation == "requireCompleted")
        {
            _ = await WindowsTransferRecoveryWriter.AppendCompletionAsync(owner, mapping, WindowsTransferContentIo.Native, CancellationToken.None);
        }
    }

    private static Task InvokeAsync(WindowsTransferArtifactOwner owner, WindowsTransferRecoveryMapping mapping, string operation, CancellationToken token)
    {
        return operation switch
        {
            "initial" => WindowsTransferRecoveryWriter.WriteInitialAsync(owner, mapping, WindowsTransferContentIo.Native, token),
            "requireInitial" => WindowsTransferRecoveryWriter.RequireInitialAsync(owner, mapping, WindowsTransferContentIo.Native, token),
            "append" => WindowsTransferRecoveryWriter.AppendCompletionAsync(owner, mapping, WindowsTransferContentIo.Native, token),
            "requireCompleted" => WindowsTransferRecoveryWriter.RequireCompletedAsync(owner, mapping, WindowsTransferContentIo.Native, token),
            _ => throw new ArgumentOutOfRangeException(nameof(operation)),
        };
    }
}
