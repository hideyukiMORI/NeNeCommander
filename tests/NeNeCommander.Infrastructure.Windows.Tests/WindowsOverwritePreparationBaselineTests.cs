using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves final checks retain the initial content and recovery baselines instead of accepting two equally changed observations.</summary>
[TestClass]
public sealed class WindowsOverwritePreparationBaselineTests
{
    /// <summary>Proves equal changed bytes on both sides of final comparison cannot replace the first verified digest.</summary>
    [TestMethod]
    [DataRow("original")]
    [DataRow("desired")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task PrepareAsyncWhenBothFinalReadsChangeEquallyRefusesRefreshedBaselineAsync(string baseline)
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        TransferArtifactRole role = baseline == "original" ? TransferArtifactRole.OriginalBackup : TransferArtifactRole.PreparedContent;
        WindowsOverwriteAttempt? observed = null;
        bool changed = false;
        int changedReads = 0;
        WindowsTransferContentIo io = new(async (handle, bytes, offset, cancellationToken) =>
        {
            int count = await RandomAccess.ReadAsync(handle, bytes, offset, cancellationToken);
            if (changed && count > 0 && (ReferenceEquals(handle, baseline == "original" ? fixture.Pair.Target.Handle : fixture.Pair.Source.Handle) ||
                WindowsOverwritePreparationFixture.IsArtifact(handle, observed, role)))
            {
                bytes.Span[..count].Fill(42);
                changedReads++;
            }
            return count;
        }, RandomAccess.WriteAsync, RandomAccess.SetLength, handle =>
        {
            RandomAccess.FlushToDisk(handle);
            changed = true;
        });
        using WindowsOverwriteAttempt attempt = fixture.Attempt(new DeterministicIdentifierSource(), io, WindowsTransferArtifactQueries.Native);
        observed = attempt;

        IOException failure = await Assert.ThrowsAsync<IOException>(() => attempt.PrepareAsync(CancellationToken.None));
        Assert.IsTrue(failure.Message.Contains("baseline", StringComparison.Ordinal));
        Assert.AreEqual(2, changedReads);
        _ = Assert.IsInstanceOfType<VerifiedArtifactContent>(WindowsOverwritePreparationFixture.Artifact(attempt, role).Content);
        RecoveryMappingContent mapping = Assert.IsInstanceOfType<RecoveryMappingContent>(WindowsOverwritePreparationFixture.Artifact(attempt, TransferArtifactRole.RecoveryRecord).Content);
        Assert.AreEqual(RecoveryFlushObservation.Confirmed, mapping.InitialFrame.Flush);
        fixture.RequireUntouched(attempt, "source", "target");
    }

    /// <summary>Proves final read failure or cancellation keeps the first content/frame proof without claiming successful preparation.</summary>
    [TestMethod]
    [DataRow("original", "throw")]
    [DataRow("original", "cancel")]
    [DataRow("desired", "throw")]
    [DataRow("desired", "cancel")]
    [DataRow("recovery", "throw")]
    [DataRow("recovery", "cancel")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task PrepareAsyncWhenFinalReadFailsRetainsHistoricalEvidenceAsync(string baseline, string failure)
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        using CancellationTokenSource cancellation = new();
        WindowsOverwriteAttempt? observed = null;
        ArtifactContentObservation? before = null;
        bool armed = false;
        TransferArtifactRole role = FinalReadRole(baseline);
        WindowsTransferContentIo io = new(async (handle, bytes, offset, token) =>
        {
            int count = await RandomAccess.ReadAsync(handle, bytes, offset, token);
            if (armed && count > 0 && WindowsOverwritePreparationFixture.IsArtifact(handle, observed, role))
            {
                if (failure == "throw")
                {
                    throw new IOException("Injected final read failure.");
                }
                cancellation.Cancel();
            }
            return count;
        }, RandomAccess.WriteAsync, RandomAccess.SetLength, handle =>
        {
            RandomAccess.FlushToDisk(handle);
            before = WindowsOverwritePreparationFixture.Artifact(observed ?? throw new AssertFailedException(), role).Content;
            armed = true;
        });
        using WindowsOverwriteAttempt attempt = fixture.Attempt(new DeterministicIdentifierSource(), io, WindowsTransferArtifactQueries.Native);
        observed = attempt;

        if (failure == "throw")
        {
            _ = await Assert.ThrowsAsync<IOException>(() => attempt.PrepareAsync(cancellation.Token));
        }
        else
        {
            _ = await Assert.ThrowsAsync<OperationCanceledException>(() => attempt.PrepareAsync(cancellation.Token));
        }
        RequireHistoricalEvidence(WindowsOverwritePreparationFixture.Artifact(attempt, role), before);
        fixture.RequireUntouched(attempt, "source", "target");
    }

    private static TransferArtifactRole FinalReadRole(string baseline)
    {
        return baseline switch
        {
            "original" => TransferArtifactRole.OriginalBackup,
            "desired" => TransferArtifactRole.PreparedContent,
            "recovery" => TransferArtifactRole.RecoveryRecord,
            _ => throw new ArgumentOutOfRangeException(nameof(baseline)),
        };
    }

    private static void RequireHistoricalEvidence(IdentifiedTransferArtifact artifact, ArtifactContentObservation? before)
    {
        Assert.IsNotNull(before);
        if (before is RecoveryMappingContent mapping)
        {
            RecoveryMappingContent after = Assert.IsInstanceOfType<RecoveryMappingContent>(artifact.Content);
            Assert.AreEqual(mapping.InitialFrame.Frame, after.InitialFrame.Frame);
            Assert.AreEqual(RecoveryFlushObservation.Confirmed, after.InitialFrame.Flush);
        }
        else
        {
            Assert.AreEqual(before, artifact.Content);
        }
        Assert.AreEqual(TransferPathObservation.LastKnownPath, artifact.Artifact.PathObservation);
    }

    /// <summary>Proves the final preparation cannot accept a truncated, extended or same-length-corrupt recovery mapping after flush.</summary>
    [TestMethod]
    [DataRow("truncate")]
    [DataRow("extend")]
    [DataRow("corrupt")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task PrepareAsyncWhenRecoveryChangesAfterFlushRefusesFinalPreparationAsync(string mutation)
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsTransferContentIo io = new(RandomAccess.ReadAsync, RandomAccess.WriteAsync, RandomAccess.SetLength, handle =>
        {
            RandomAccess.FlushToDisk(handle);
            switch (mutation)
            {
                case "truncate":
                    RandomAccess.SetLength(handle, 1);
                    break;
                case "extend":
                    RandomAccess.Write(handle, new byte[1], RandomAccess.GetLength(handle));
                    break;
                case "corrupt":
                    RandomAccess.Write(handle, new byte[1], 0);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mutation));
            }
        });
        using WindowsOverwriteAttempt attempt = fixture.Attempt(new DeterministicIdentifierSource(), io, WindowsTransferArtifactQueries.Native);

        _ = await Assert.ThrowsAsync<IOException>(() => attempt.PrepareAsync(CancellationToken.None));
        Assert.HasCount(3, attempt.Receipt.Artifacts);
        RecoveryMappingContent saved = Assert.IsInstanceOfType<RecoveryMappingContent>(WindowsOverwritePreparationFixture.Artifact(attempt, TransferArtifactRole.RecoveryRecord).Content);
        Assert.AreEqual(RecoveryFlushObservation.Confirmed, saved.InitialFrame.Flush);
        Assert.IsTrue(saved.InitialFrame.Frame.Size.Bytes > 1);
        fixture.RequireUntouched(attempt, "source", "target");
    }
}
