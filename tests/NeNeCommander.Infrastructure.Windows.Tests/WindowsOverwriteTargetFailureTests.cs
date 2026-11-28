using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the target receipt crosses into uncertainty before native effects and never infers no effect from an exception.</summary>
[TestClass]
public sealed class WindowsOverwriteTargetFailureTests
{
    /// <summary>Proves actual first-copy read failure or cancellation leaves the target untouched after complete final admission.</summary>
    [TestMethod]
    [DataRow("throw")]
    [DataRow("cancel")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task OverwritePreparedAsyncWhenFirstCopyReadStopsRetainsUntouchedAsync(string failure)
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        using CancellationTokenSource cancellation = new();
        WindowsOverwriteAttempt? observed = null;
        bool armed = false;
        bool copyRead = false;
        WindowsTransferContentIo io = new(async (handle, bytes, offset, token) =>
        {
            int count = await RandomAccess.ReadAsync(handle, bytes, offset, token);
            if (armed && count == 0 && WindowsOverwritePreparationFixture.IsArtifact(handle, observed, TransferArtifactRole.RecoveryRecord))
            {
                copyRead = true;
            }
            if (copyRead && WindowsOverwritePreparationFixture.IsArtifact(handle, observed, TransferArtifactRole.PreparedContent))
            {
                if (failure == "throw")
                {
                    throw new IOException("Injected first target-copy read failure.");
                }
                cancellation.Cancel();
            }
            return count;
        }, RandomAccess.WriteAsync, RandomAccess.SetLength, RandomAccess.FlushToDisk);
        using WindowsOverwriteAttempt attempt = fixture.Attempt(new DeterministicIdentifierSource(), io, WindowsTransferArtifactQueries.Native);
        observed = attempt;
        _ = await attempt.PrepareAsync(CancellationToken.None);
        armed = true;

        await RequireStopAsync(attempt, failure, cancellation.Token);
        Assert.IsTrue(copyRead);
        fixture.RequireUntouched(attempt, "source", "target");
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.OverwritePreparedAsync(CancellationToken.None));
    }

    /// <summary>Proves uncertainty is recorded before no-write, partial-write, full-write or truncate exceptions, with actual bytes retained.</summary>
    [TestMethod]
    [DataRow("before")]
    [DataRow("partial")]
    [DataRow("full")]
    [DataRow("truncate")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task OverwritePreparedAsyncWhenNativeTargetCallThrowsRetainsMayHaveChangedAsync(string failure)
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target-old");
        WindowsOverwriteAttempt? observed = null;
        WindowsOverwriteTargetFailureInjection injection = new(fixture, failure, () => observed);
        using WindowsOverwriteAttempt attempt = fixture.Attempt(new DeterministicIdentifierSource(), injection.Content, WindowsTransferArtifactQueries.Native);
        observed = attempt;
        _ = await attempt.PrepareAsync(CancellationToken.None);

        _ = await Assert.ThrowsAsync<IOException>(() => attempt.OverwritePreparedAsync(CancellationToken.None));
        RequireRetainedUncertainty(attempt, fixture, ExpectedTarget(failure));
    }

    /// <summary>Proves empty desired content enters uncertainty before destructive truncate and preserves cancellation after it succeeds.</summary>
    [TestMethod]
    [DataRow("throw")]
    [DataRow("cancel")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task OverwritePreparedAsyncWhenEmptyTruncateStopsRetainsActualEmptyTargetAsync(string failure)
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create(string.Empty, "target");
        using CancellationTokenSource cancellation = new();
        WindowsOverwriteAttempt? observed = null;
        WindowsTransferContentIo io = new(RandomAccess.ReadAsync, RandomAccess.WriteAsync, (handle, length) =>
        {
            if (ReferenceEquals(handle, fixture.Pair.Target.Handle))
            {
                _ = Assert.IsInstanceOfType<TargetMayHaveChanged>(observed?.Receipt.Target);
                RandomAccess.SetLength(handle, length);
                if (failure == "throw")
                {
                    throw new IOException("Injected exception after empty truncate.");
                }
                cancellation.Cancel();
            }
            else
            {
                RandomAccess.SetLength(handle, length);
            }
        }, RandomAccess.FlushToDisk);
        using WindowsOverwriteAttempt attempt = fixture.Attempt(new DeterministicIdentifierSource(), io, WindowsTransferArtifactQueries.Native);
        observed = attempt;
        _ = await attempt.PrepareAsync(CancellationToken.None);

        await RequireStopAsync(attempt, failure, cancellation.Token);
        _ = Assert.IsInstanceOfType<TargetMayHaveChanged>(attempt.Receipt.Target);
        Assert.AreEqual(string.Empty, fixture.Read(fixture.Pair.Target.Path));
        Assert.AreEqual(string.Empty, fixture.Read(fixture.Pair.Source.Path));
        Assert.HasCount(3, attempt.Receipt.Artifacts);
        Assert.HasCount(0, attempt.Receipt.Effects);
    }

    private static async Task RequireStopAsync(WindowsOverwriteAttempt attempt, string failure, CancellationToken cancellationToken)
    {
        _ = failure == "throw"
            ? (Exception)await Assert.ThrowsAsync<IOException>(() => attempt.OverwritePreparedAsync(cancellationToken))
            : await Assert.ThrowsAsync<OperationCanceledException>(() => attempt.OverwritePreparedAsync(cancellationToken));
    }

    private static string ExpectedTarget(string failure)
    {
        return failure switch
        {
            "before" => "target-old",
            "partial" => "sarget-old",
            "full" => "source-old",
            "truncate" => "source",
            _ => throw new ArgumentOutOfRangeException(nameof(failure)),
        };
    }

    private static void RequireRetainedUncertainty(WindowsOverwriteAttempt attempt, WindowsOverwritePreparationFixture fixture, string expected)
    {
        _ = Assert.IsInstanceOfType<TargetMayHaveChanged>(attempt.Receipt.Target);
        Assert.HasCount(0, attempt.Receipt.Effects);
        Assert.HasCount(3, attempt.Receipt.Artifacts);
        Assert.AreEqual(expected, fixture.Read(fixture.Pair.Target.Path));
        Assert.AreEqual("source", fixture.Read(fixture.Pair.Source.Path));
    }
}
