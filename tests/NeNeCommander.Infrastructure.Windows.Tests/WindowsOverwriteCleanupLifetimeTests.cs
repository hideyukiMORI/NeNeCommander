using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves cleanup cannot begin another target observation after its attempt was disposed at a completed artifact read.</summary>
[TestClass]
public sealed class WindowsOverwriteCleanupLifetimeTests
{
    /// <summary>Disposal on recovery EOF retains evidence and prevents later source/target reads or link removal.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task CleanupAsyncWhenRecoveryEofDisposesAttemptStopsBeforeAnotherTargetReadAsync()
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsOverwriteCleanupFixture calls = new();
        int removals = 0;
        WindowsTransferArtifactRemoval removal = new(_ => { removals++; return unchecked((int)0xC0000001); }, _ => false);
        using WindowsOverwriteAttempt attempt = files.Attempt(new DeterministicIdentifierSource(), calls.Io, WindowsTransferArtifactQueries.Native, removal);
        calls.Attempt = attempt;
        _ = await WindowsOverwriteCleanupFixture.CompleteAsync(attempt);
        SafeFileHandle recovery = calls.Handle(TransferArtifactRole.RecoveryRecord);
        ArtifactContentObservation previous = WindowsOverwritePreparationFixture.Artifact(attempt, TransferArtifactRole.RecoveryRecord).Content;
        bool disposed = false;
        int laterReads = 0;
        calls.Read = async (handle, bytes, offset, token) =>
        {
            laterReads += disposed ? 1 : 0;
            int count = await RandomAccess.ReadAsync(handle, bytes, offset, token);
            if (!disposed && count == 0 && ReferenceEquals(handle, recovery))
            {
                disposed = true;
                attempt.Dispose();
            }
            return count;
        };

        _ = await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => attempt.CleanupAsync(CancellationToken.None));

        Assert.IsTrue(disposed);
        Assert.AreEqual(0, laterReads);
        Assert.AreEqual(0, removals);
        Assert.AreEqual(previous, WindowsOverwritePreparationFixture.Artifact(attempt, TransferArtifactRole.RecoveryRecord).Content);
        WindowsOverwriteCleanupTests.RequirePartial(attempt, 0, ArtifactLinkObservation.Retained);
        Assert.AreEqual("source", files.Read(files.Pair.Source.Path));
        Assert.AreEqual("source", files.Read(files.Pair.Target.Path));
    }
}
