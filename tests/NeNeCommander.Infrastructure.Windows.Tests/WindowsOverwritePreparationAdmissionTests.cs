using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves pre-effect admission and cancellation do not invent successful preparations or discard completed creation knowledge.</summary>
[TestClass]
public sealed class WindowsOverwritePreparationAdmissionTests
{
    /// <summary>Proves cancellation before admission never consumes a token or creates an artifact.</summary>
    [TestMethod]
    public async Task PrepareAsyncWhenAlreadyCancelledRetainsUntouchedReceiptWithoutTokenAsync()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        DeterministicIdentifierSource identifiers = new();
        using WindowsOverwriteAttempt attempt = fixture.Attempt(identifiers, WindowsTransferContentIo.Native, WindowsTransferArtifactQueries.Native);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        _ = await Assert.ThrowsAsync<OperationCanceledException>(() => attempt.PrepareAsync(cancellation.Token));
        Assert.AreEqual(0, identifiers.IssuedCount);
        Assert.HasCount(0, attempt.Receipt.Artifacts);
        fixture.RequireUntouched(attempt, "source", "target");
    }

    /// <summary>Proves a changed admitted original prevents token acquisition and all preparation effects.</summary>
    [TestMethod]
    public async Task PrepareAsyncWhenOriginalShapeChangedRefusesBeforeTokenAsync()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        DeterministicIdentifierSource identifiers = new();
        using WindowsOverwriteAttempt attempt = fixture.Attempt(identifiers, WindowsTransferContentIo.Native, WindowsTransferArtifactQueries.Native);
        File.SetAttributes(fixture.Pair.Source.Path.CanonicalText, FileAttributes.Hidden);

        _ = await Assert.ThrowsAsync<IOException>(() => attempt.PrepareAsync(CancellationToken.None));
        Assert.AreEqual(0, identifiers.IssuedCount);
        Assert.HasCount(0, attempt.Receipt.Artifacts);
        _ = Assert.IsInstanceOfType<UntouchedTransferTarget>(attempt.Receipt.Target);
        Assert.AreEqual("target", fixture.Read(fixture.Pair.Target.Path));
    }

    /// <summary>Proves cancellation during a native artifact observation retains that file and prevents the next creation.</summary>
    [TestMethod]
    [DataRow("recovery.txt", 1)]
    [DataRow("original.bin", 2)]
    [DataRow("prepared.bin", 3)]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task PrepareAsyncWhenCreationObservationCancelsRetainsOnlyAttemptedOwnersAsync(string suffix, int count)
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        using CancellationTokenSource cancellation = new();
        WindowsTransferArtifactQueries queries = new(WindowsTransferArtifactQueries.Native.ReadIdentity, WindowsFileIdentifier.ReadLocalFacts, (handle, path) =>
        {
            WindowsTransferPathObservation.Require(handle, path);
            if (path.CanonicalText.EndsWith(suffix, StringComparison.Ordinal))
            {
                cancellation.Cancel();
            }
        });
        using WindowsOverwriteAttempt attempt = fixture.Attempt(new DeterministicIdentifierSource(), WindowsTransferContentIo.Native, queries);

        _ = await Assert.ThrowsAsync<OperationCanceledException>(() => attempt.PrepareAsync(cancellation.Token));
        Assert.HasCount(count, attempt.Receipt.Artifacts);
        IdentifiedTransferArtifact last = Assert.IsInstanceOfType<IdentifiedTransferArtifact>(attempt.Receipt.Artifacts[^1]);
        Assert.IsTrue(File.Exists(last.Artifact.Path.CanonicalText));
        Assert.AreEqual(ArtifactContentObservation.Unknown, last.Content);
        fixture.RequireUntouched(attempt, "source", "target");
    }

    /// <summary>Proves byte verification is retained before a subsequent fallible metadata/path query.</summary>
    [TestMethod]
    [DataRow("backup")]
    [DataRow("prepared")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task PrepareAsyncWhenPostVerificationObservationFailsKeepsVerifiedContentAsync(string artifact)
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsOverwriteAttempt? observed = null;
        TransferArtifactRole role = artifact == "backup" ? TransferArtifactRole.OriginalBackup : TransferArtifactRole.PreparedContent;
        WindowsTransferArtifactQueries queries = new(WindowsTransferArtifactQueries.Native.ReadIdentity, WindowsFileIdentifier.ReadLocalFacts, (handle, path) =>
        {
            WindowsTransferPathObservation.Require(handle, path);
            if (observed is not null && WindowsOverwritePreparationFixture.IsArtifact(handle, observed, role) &&
                WindowsOverwritePreparationFixture.Artifact(observed, role).Content is VerifiedArtifactContent)
            {
                throw new IOException("Injected observation failure after byte verification.");
            }
        });
        using WindowsOverwriteAttempt attempt = fixture.Attempt(new DeterministicIdentifierSource(), WindowsTransferContentIo.Native, queries);
        observed = attempt;

        _ = await Assert.ThrowsAsync<IOException>(() => attempt.PrepareAsync(CancellationToken.None));
        IdentifiedTransferArtifact known = WindowsOverwritePreparationFixture.Artifact(attempt, role);
        _ = Assert.IsInstanceOfType<VerifiedArtifactContent>(known.Content);
        Assert.AreEqual(TransferPathObservation.LastKnownPath, known.Artifact.PathObservation);
        Assert.HasCount(3, attempt.Receipt.Artifacts);
        fixture.RequireUntouched(attempt, "source", "target");
    }
    /// <summary>Proves fallible metadata after initial-frame verification or flush cannot erase either fact.</summary>
    [TestMethod]
    [DataRow("pending")]
    [DataRow("confirmed")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task PrepareAsyncWhenRecoveryObservationFailsRetainsFrameAndFlushAsync(string phase)
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsOverwriteAttempt? observed = null;
        RecoveryFlushObservation expected = phase == "pending" ? RecoveryFlushObservation.NotAttempted : RecoveryFlushObservation.Confirmed;
        WindowsTransferArtifactQueries queries = new(WindowsTransferArtifactQueries.Native.ReadIdentity, WindowsFileIdentifier.ReadLocalFacts, (handle, path) =>
        {
            WindowsTransferPathObservation.Require(handle, path);
            if (observed is not null && WindowsOverwritePreparationFixture.IsArtifact(handle, observed, TransferArtifactRole.RecoveryRecord) &&
                WindowsOverwritePreparationFixture.Artifact(observed, TransferArtifactRole.RecoveryRecord).Content is RecoveryMappingContent mapping && mapping.InitialFrame.Flush == expected)
            {
                throw new IOException("Injected observation failure after recovery evidence.");
            }
        });
        using WindowsOverwriteAttempt attempt = fixture.Attempt(new DeterministicIdentifierSource(), WindowsTransferContentIo.Native, queries);
        observed = attempt;

        _ = await Assert.ThrowsAsync<IOException>(() => attempt.PrepareAsync(CancellationToken.None));
        IdentifiedTransferArtifact known = WindowsOverwritePreparationFixture.Artifact(attempt, TransferArtifactRole.RecoveryRecord);
        RecoveryMappingContent saved = Assert.IsInstanceOfType<RecoveryMappingContent>(known.Content);
        Assert.AreEqual(expected, saved.InitialFrame.Flush);
        Assert.IsTrue(saved.InitialFrame.Frame.Size.Bytes > 0);
        Assert.AreEqual(TransferPathObservation.LastKnownPath, known.Artifact.PathObservation);
        fixture.RequireUntouched(attempt, "source", "target");
    }
}
