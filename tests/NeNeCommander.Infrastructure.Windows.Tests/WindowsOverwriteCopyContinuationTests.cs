using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the provider continuation needs no caller context with explicit flush-success injection; actual native flush is proved by separate tests.</summary>
[TestClass]
public sealed class WindowsOverwriteCopyContinuationTests
{
    /// <summary>Each provider stage is independently stopped at its first actual I/O acknowledgement.</summary>
    [TestMethod]
    [DataRow("prepare")]
    [DataRow("overwrite")]
    [DataRow("append")]
    [DataRow("cleanup")]
    public async Task CopyAsyncWhenEachStageSuspendsReturnsReceiptWithoutCallerContextAsync(string stage)
    {
        int suspension = await CallsBeforeAsync(stage) + 1;
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = WindowsLocalOperationTestContext.LocalSnapshot(root.WriteFile("source", "source"));
        WindowsLocalPath target = WindowsTransferRecoveryTestFixture.Path(root.WriteFile("target", "target"));
        TransferOverwriteEvidence original;
        using (WindowsOverwritePairLease pair = WindowsOverwritePairLease.Open(source, target, new WindowsDriveBoundary()))
        {
            original = pair.Evidence;
        }
        TransferSuspensionIo io = new(_ => { });
        io.Arm(suspension);
        WindowsOverwriteCopy provider = new(new WindowsOverwriteDependencies(new DeterministicIdentifierSource(), io.Io,
            WindowsTransferArtifactQueries.Native, WindowsTransferArtifactRemoval.Native), new WindowsDriveBoundary());
        TransferContinuationContext context = new();
        Task<ProviderStepOutcome>? result = null;
        Task operation = context.StartAsync(() => result = provider.CopyAsync(source, original, CancellationToken.None));
        try
        {
            Assert.IsTrue(io.Suspended, $"The {stage} provider boundary was not reached.");
            Assert.IsFalse(operation.IsCompleted);
        }
        finally
        {
            io.Release();
            await context.DrainAsync(operation);
        }
        Assert.AreEqual(0, context.Posts);
        Assert.AreEqual(2, io.Flushes);
        Assert.IsNotNull(result);
        ProviderStepOutcome outcome = await result;
        Assert.AreSame(ProviderStepCompletion.Succeeded, outcome.Completion);
        _ = Assert.IsInstanceOfType<DesiredTargetContentVerified>(outcome.Receipt.Target);
        Assert.HasCount(3, outcome.Receipt.Artifacts);
        Assert.HasCount(2, Directory.GetFiles(root.Path.CanonicalText));
        Assert.AreEqual("source", File.ReadAllText(source.Path.CanonicalText));
        Assert.AreEqual("source", File.ReadAllText(target.CanonicalText));
    }

    private static async Task<int> CallsBeforeAsync(string stage)
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        TransferSuspensionIo io = new(_ => { });
        using WindowsOverwriteAttempt attempt = files.Attempt(new DeterministicIdentifierSource(), io.Io, WindowsTransferArtifactQueries.Native);
        if (stage == "prepare")
        {
            return 0;
        }
        _ = await attempt.PrepareAsync(CancellationToken.None);
        if (stage is "append" or "cleanup")
        {
            await attempt.OverwritePreparedAsync(CancellationToken.None);
        }
        if (stage == "cleanup")
        {
            await attempt.AppendCompletionAsync(CancellationToken.None);
        }
        Assert.AreEqual(stage == "cleanup" ? 2 : 1, io.Flushes);
        return io.Calls;
    }
}
