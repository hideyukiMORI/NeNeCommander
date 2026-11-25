using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves completion is a one-shot continuation only after a successful target stage, including cancellation and asynchronous reentry.</summary>
[TestClass]
public sealed class WindowsOverwriteCompletionProtocolTests
{
    /// <summary>Proves preparation alone or a new attempt cannot claim desired-content completion.</summary>
    [TestMethod]
    [DataRow("new")]
    [DataRow("prepared")]
    public async Task AppendCompletionAsyncWhenTargetStageHasNotSucceededRefusesWithoutChangingTargetAsync(string phase)
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        using WindowsOverwriteAttempt attempt = fixture.Attempt(new DeterministicIdentifierSource(), WindowsTransferContentIo.Native, WindowsTransferArtifactQueries.Native);
        if (phase == "prepared")
        {
            _ = await attempt.PrepareAsync(CancellationToken.None);
        }

        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.AppendCompletionAsync(CancellationToken.None));
        fixture.RequireUntouched(attempt, "source", "target");
    }

    /// <summary>Proves a pre-cancelled completion consumes the continuation without attempting the append or allowing replay.</summary>
    [TestMethod]
    public async Task AppendCompletionAsyncWhenAlreadyCancelledRetainsDesiredAndUnattemptedAppendAsync()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        using WindowsOverwriteAttempt attempt = fixture.Attempt(new DeterministicIdentifierSource(), WindowsTransferContentIo.Native, WindowsTransferArtifactQueries.Native);
        _ = await attempt.PrepareAsync(CancellationToken.None);
        await attempt.OverwritePreparedAsync(CancellationToken.None);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        _ = await Assert.ThrowsAsync<OperationCanceledException>(() => attempt.AppendCompletionAsync(cancellation.Token));
        RecoveryMappingContent saved = Assert.IsInstanceOfType<RecoveryMappingContent>(WindowsOverwritePreparationFixture.Artifact(attempt, TransferArtifactRole.RecoveryRecord).Content);
        Assert.AreEqual(RecoveryAppendObservation.NotAttempted, saved.Append);
        _ = Assert.IsInstanceOfType<DesiredTargetContentVerified>(attempt.Receipt.Target);
        Assert.HasCount(3, attempt.Receipt.Artifacts);
        Assert.HasCount(0, attempt.Receipt.Effects);
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.AppendCompletionAsync(CancellationToken.None));
    }

    /// <summary>Proves a second caller cannot enter while initial readmission is awaiting.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-014")]
    public async Task AppendCompletionAsyncWhenInitialReadIsPendingRefusesConcurrentEntryAsync()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
        bool armed = false;
        bool suspended = false;
        WindowsTransferContentIo io = new(async (handle, bytes, offset, token) =>
        {
            if (armed && !suspended)
            {
                suspended = true;
                entered.SetResult();
                await resume.Task;
            }
            return await RandomAccess.ReadAsync(handle, bytes, offset, token);
        }, RandomAccess.WriteAsync, RandomAccess.SetLength, RandomAccess.FlushToDisk);
        using WindowsOverwriteAttempt attempt = fixture.Attempt(new DeterministicIdentifierSource(), io, WindowsTransferArtifactQueries.Native);
        _ = await attempt.PrepareAsync(CancellationToken.None);
        await attempt.OverwritePreparedAsync(CancellationToken.None);
        armed = true;
        Task first = attempt.AppendCompletionAsync(CancellationToken.None);
        await entered.Task;
        try
        {
            _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.AppendCompletionAsync(CancellationToken.None));
        }
        finally
        {
            resume.SetResult();
            await first;
        }
        RecoveryMappingContent saved = Assert.IsInstanceOfType<RecoveryMappingContent>(WindowsOverwritePreparationFixture.Artifact(attempt, TransferArtifactRole.RecoveryRecord).Content);
        Assert.AreEqual(RecoveryFlushObservation.Confirmed, Assert.IsInstanceOfType<VerifiedRecoveryAppend>(saved.Append).Frame.Flush);
    }

    /// <summary>Proves native flush acknowledgement survives cancellation without opening a retry or cleanup path.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task AppendCompletionAsyncWhenFlushAcknowledgesThenCancelsRetainsConfirmedFrameAsync()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        using CancellationTokenSource cancellation = new();
        bool armed = false;
        WindowsTransferContentIo io = new(RandomAccess.ReadAsync, RandomAccess.WriteAsync, RandomAccess.SetLength, handle =>
        {
            RandomAccess.FlushToDisk(handle);
            if (armed)
            {
                cancellation.Cancel();
            }
        });
        using WindowsOverwriteAttempt attempt = fixture.Attempt(new DeterministicIdentifierSource(), io, WindowsTransferArtifactQueries.Native);
        _ = await attempt.PrepareAsync(CancellationToken.None);
        await attempt.OverwritePreparedAsync(CancellationToken.None);
        armed = true;

        _ = await Assert.ThrowsAsync<OperationCanceledException>(() => attempt.AppendCompletionAsync(cancellation.Token));
        RecoveryMappingContent saved = Assert.IsInstanceOfType<RecoveryMappingContent>(WindowsOverwritePreparationFixture.Artifact(attempt, TransferArtifactRole.RecoveryRecord).Content);
        Assert.AreEqual(RecoveryFlushObservation.Confirmed, saved.InitialFrame.Flush);
        Assert.AreEqual(RecoveryFlushObservation.Confirmed, Assert.IsInstanceOfType<VerifiedRecoveryAppend>(saved.Append).Frame.Flush);
        Assert.HasCount(0, attempt.Receipt.Effects);
        Assert.HasCount(3, attempt.Receipt.Artifacts);
        _ = Assert.IsInstanceOfType<DesiredTargetContentVerified>(attempt.Receipt.Target);
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.AppendCompletionAsync(CancellationToken.None));
    }
}
