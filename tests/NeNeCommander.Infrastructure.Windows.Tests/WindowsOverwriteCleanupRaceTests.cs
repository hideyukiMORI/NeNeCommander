using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves each cleanup effect is followed only by a newly admitted continuation, never stale success or disposal resurrection.</summary>
[TestClass]
public sealed class WindowsOverwriteCleanupRaceTests
{
    /// <summary>Proves changed source, target or remaining artifacts after the first removal stop before the second removal.</summary>
    [TestMethod]
    [DataRow("source")]
    [DataRow("target")]
    [DataRow("backup")]
    [DataRow("recovery")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task CleanupAsyncWhenStateChangesBetweenRemovalsRetainsRemainingLinksAsync(string change)
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsOverwriteCleanupFixture calls = new();
        int removed = 0;
        bool Close(IntPtr handle)
        {
            bool closed = WindowsTransferArtifactRemoval.Native.Close(handle);
            if (++removed == 1)
            {
                Change(files, calls, change);
            }
            return closed;
        }
        WindowsTransferArtifactRemoval removal = new(WindowsTransferArtifactRemoval.Native.RequestDisposition, Close);
        using WindowsOverwriteAttempt attempt = files.Attempt(new DeterministicIdentifierSource(), calls.Io, WindowsTransferArtifactQueries.Native, removal);
        calls.Attempt = attempt;
        _ = await WindowsOverwriteCleanupFixture.CompleteAsync(attempt);

        _ = await Assert.ThrowsAsync<IOException>(() => attempt.CleanupAsync(CancellationToken.None));
        Assert.AreEqual(1, removed);
        WindowsOverwriteCleanupTests.RequirePartial(attempt, 1, ArtifactLinkObservation.Removed);
        Assert.AreEqual("source", files.Read(files.Pair.Source.Path));
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.CleanupAsync(CancellationToken.None));
    }

    private static void Change(WindowsOverwritePreparationFixture files, WindowsOverwriteCleanupFixture calls, string change)
    {
        if (change == "source")
        {
            files.Root.SetLastWriteTimeUtc("source", new DateTime(2000, 1, 2, 3, 4, 5, DateTimeKind.Utc));
            return;
        }
        RandomAccess.Write(change == "target" ? files.Pair.Target.Handle
            : calls.Handle(change == "backup" ? TransferArtifactRole.OriginalBackup : TransferArtifactRole.RecoveryRecord), [42], 0);
    }

    /// <summary>Proves reentrant disposal during acknowledged native close cannot resume or resurrect the attempt or owner.</summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(3)]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-014")]
    public async Task CleanupAsyncWhenDisposedDuringNativeCloseKeepsAcknowledgementAndStopsAsync(int disposeAt)
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsOverwriteAttempt? observed = null;
        int removed = 0;
        WindowsTransferArtifactRemoval removal = new(WindowsTransferArtifactRemoval.Native.RequestDisposition, handle =>
        {
            if (++removed == disposeAt)
            {
                observed?.Dispose();
            }
            return WindowsTransferArtifactRemoval.Native.Close(handle);
        });
        using WindowsOverwriteAttempt attempt = files.Attempt(new DeterministicIdentifierSource(), WindowsTransferContentIo.Native, WindowsTransferArtifactQueries.Native, removal);
        observed = attempt;
        _ = await WindowsOverwriteCleanupFixture.CompleteAsync(attempt);

        _ = await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => attempt.CleanupAsync(CancellationToken.None));
        Assert.AreEqual(disposeAt, removed);
        WindowsOverwriteCleanupTests.RequirePartial(attempt, disposeAt, ArtifactLinkObservation.Removed);
        Assert.AreEqual("source", files.Read(files.Pair.Source.Path));
        _ = await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => attempt.CleanupAsync(CancellationToken.None));
    }
}
