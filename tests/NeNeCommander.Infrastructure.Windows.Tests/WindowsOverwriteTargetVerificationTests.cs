using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves final target verification binds first desired bytes and complete native observations, retaining known completion before later cancellation.</summary>
[TestClass]
public sealed class WindowsOverwriteTargetVerificationTests
{
    /// <summary>Proves equal changed comparison bytes cannot replace the initial desired digest.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task OverwritePreparedAsyncWhenBothFinalReadsChangeEquallyRejectsNewDigestAsync()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        bool written = false;
        int altered = 0;
        WindowsTransferContentIo io = new(async (handle, bytes, offset, token) =>
        {
            int count = await RandomAccess.ReadAsync(handle, bytes, offset, token);
            if (written && count > 0 && (ReferenceEquals(handle, fixture.Pair.Source.Handle) || ReferenceEquals(handle, fixture.Pair.Target.Handle)))
            {
                bytes.Span[..count].Fill(42);
                altered++;
            }
            return count;
        }, RandomAccess.WriteAsync, (handle, length) =>
        {
            RandomAccess.SetLength(handle, length);
            written |= ReferenceEquals(handle, fixture.Pair.Target.Handle);
        }, RandomAccess.FlushToDisk);
        using WindowsOverwriteAttempt attempt = fixture.Attempt(new DeterministicIdentifierSource(), io, WindowsTransferArtifactQueries.Native);
        _ = await attempt.PrepareAsync(CancellationToken.None);

        IOException failure = await Assert.ThrowsAsync<IOException>(() => attempt.OverwritePreparedAsync(CancellationToken.None));
        Assert.IsTrue(failure.Message.Contains("first desired baseline", StringComparison.Ordinal));
        Assert.AreEqual(2, altered);
        _ = Assert.IsInstanceOfType<TargetMayHaveChanged>(attempt.Receipt.Target);
        Assert.AreEqual("source", fixture.Read(fixture.Pair.Source.Path));
        Assert.AreEqual("source", fixture.Read(fixture.Pair.Target.Path));
    }

    /// <summary>Proves rewritten source or target metadata during complete byte comparison prevents final verification.</summary>
    [TestMethod]
    [DataRow("source")]
    [DataRow("target")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task OverwritePreparedAsyncWhenObservedIdentityChangesDuringComparisonRetainsUncertaintyAsync(string changed)
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        bool written = false;
        bool changedMetadata = false;
        WindowsTransferContentIo io = new(async (handle, bytes, offset, token) =>
        {
            int count = await RandomAccess.ReadAsync(handle, bytes, offset, token);
            if (written && !changedMetadata && count > 0 && ReferenceEquals(handle, fixture.Pair.Target.Handle))
            {
                fixture.Root.SetLastWriteTimeUtc(changed, new DateTime(2000, 1, 2, 3, 4, 5, DateTimeKind.Utc));
                changedMetadata = true;
            }
            return count;
        }, RandomAccess.WriteAsync, (handle, length) =>
        {
            RandomAccess.SetLength(handle, length);
            written |= ReferenceEquals(handle, fixture.Pair.Target.Handle);
        }, RandomAccess.FlushToDisk);
        using WindowsOverwriteAttempt attempt = fixture.Attempt(new DeterministicIdentifierSource(), io, WindowsTransferArtifactQueries.Native);
        _ = await attempt.PrepareAsync(CancellationToken.None);

        _ = await Assert.ThrowsAsync<IOException>(() => attempt.OverwritePreparedAsync(CancellationToken.None));
        Assert.IsTrue(changedMetadata);
        _ = Assert.IsInstanceOfType<TargetMayHaveChanged>(attempt.Receipt.Target);
        Assert.AreEqual("source", fixture.Read(fixture.Pair.Source.Path));
        Assert.HasCount(3, attempt.Receipt.Artifacts);
    }

    /// <summary>Proves fully established desired content survives cancellation during the last native observation, without completing provider effects.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task OverwritePreparedAsyncWhenFinalNativeObservationCancelsRetainsVerifiedContentAsync()
    {
        using CancellationTokenSource cancellation = new();
        bool finalFacts = false;
        int targetVisits = 0;
        WindowsDriveBoundary boundary = new(path =>
        {
            if (finalFacts && path.CanonicalText.EndsWith("\\target", StringComparison.Ordinal) && ++targetVisits == 2)
            {
                cancellation.Cancel();
            }
            return DriveType.Fixed;
        });
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target", driveBoundary: boundary);
        bool written = false;
        WindowsTransferContentIo io = new(async (handle, bytes, offset, token) =>
        {
            int count = await RandomAccess.ReadAsync(handle, bytes, offset, token);
            if (written && count == 0 && ReferenceEquals(handle, fixture.Pair.Target.Handle))
            {
                finalFacts = true;
            }
            return count;
        }, RandomAccess.WriteAsync, (handle, length) =>
        {
            RandomAccess.SetLength(handle, length);
            written |= ReferenceEquals(handle, fixture.Pair.Target.Handle);
        }, RandomAccess.FlushToDisk);
        using WindowsOverwriteAttempt attempt = fixture.Attempt(new DeterministicIdentifierSource(), io, WindowsTransferArtifactQueries.Native);
        WindowsTransferRecoveryMapping prepared = await attempt.PrepareAsync(CancellationToken.None);

        _ = await Assert.ThrowsAsync<OperationCanceledException>(() => attempt.OverwritePreparedAsync(cancellation.Token));
        Assert.AreEqual(2, targetVisits);
        DesiredTargetContentVerified verified = Assert.IsInstanceOfType<DesiredTargetContentVerified>(attempt.Receipt.Target);
        Assert.AreEqual(prepared.Desired, verified.Content);
        Assert.HasCount(0, attempt.Receipt.Effects);
        Assert.HasCount(3, attempt.Receipt.Artifacts);
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.OverwritePreparedAsync(CancellationToken.None));
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.AppendCompletionAsync(CancellationToken.None));
        attempt.Dispose();
        Assert.AreEqual(verified.Content, Assert.IsInstanceOfType<DesiredTargetContentVerified>(attempt.Receipt.Target).Content);
    }
}
