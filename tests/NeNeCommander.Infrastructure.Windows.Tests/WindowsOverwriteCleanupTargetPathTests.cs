using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves later cleanup failure cannot preserve or resurrect a current target path after its shared boundary becomes unadmitted.</summary>
[TestClass]
public sealed class WindowsOverwriteCleanupTargetPathTests
{
    /// <summary>Proves a shared drive refusal after desired verification drops only current path qualification and retains desired content history.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task CleanupAsyncWhenArtifactAdmissionLosesSharedDriveInvalidatesTargetPathAsync()
    {
        DriveType current = DriveType.Fixed;
        WindowsDriveBoundary drive = new(_ => current);
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target", driveBoundary: drive);
        bool armed = false;
        WindowsTransferArtifactQueries queries = new(handle =>
        {
            if (armed)
            {
                current = DriveType.Network;
            }
            return WindowsTransferArtifactQueries.Native.ReadIdentity(handle);
        }, WindowsTransferArtifactQueries.Native.ReadFacts, WindowsTransferArtifactQueries.Native.RequirePath);
        using WindowsOverwriteAttempt attempt = files.Attempt(new DeterministicIdentifierSource(), WindowsTransferContentIo.Native, queries);
        WindowsTransferRecoveryMapping mapping = await WindowsOverwriteCleanupFixture.CompleteAsync(attempt);
        armed = true;

        _ = await Assert.ThrowsAsync<IOException>(() => attempt.CleanupAsync(CancellationToken.None));
        DesiredTargetContentVerified target = Assert.IsInstanceOfType<DesiredTargetContentVerified>(attempt.Receipt.Target);
        Assert.AreEqual(mapping.Desired, target.Content);
        Assert.AreEqual(TransferPathObservation.LastKnownPath, target.Target.PathObservation);
        WindowsOverwriteCleanupTests.RequirePartial(attempt, 0, ArtifactLinkObservation.Retained);
        Assert.AreEqual("source", files.Read(files.Pair.Source.Path));
    }

    /// <summary>Proves disposal before final desired observation cannot resurrect a current target path during failed cleanup.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-014")]
    public async Task CleanupAsyncWhenDisposedDuringTargetReadKeepsTargetPathLastKnownAsync()
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsOverwriteCleanupFixture calls = new();
        using WindowsOverwriteAttempt attempt = files.Attempt(new DeterministicIdentifierSource(), calls.Io, WindowsTransferArtifactQueries.Native);
        calls.Attempt = attempt;
        WindowsTransferRecoveryMapping mapping = await WindowsOverwriteCleanupFixture.CompleteAsync(attempt);
        calls.Read = async (handle, bytes, offset, token) =>
        {
            int count = await RandomAccess.ReadAsync(handle, bytes, offset, token);
            if (count == 0 && ReferenceEquals(handle, files.Pair.Target.Handle))
            {
                attempt.Dispose();
            }
            return count;
        };

        _ = await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => attempt.CleanupAsync(CancellationToken.None));
        DesiredTargetContentVerified target = Assert.IsInstanceOfType<DesiredTargetContentVerified>(attempt.Receipt.Target);
        Assert.AreEqual(mapping.Desired, target.Content);
        Assert.AreEqual(TransferPathObservation.LastKnownPath, target.Target.PathObservation);
        WindowsOverwriteCleanupTests.RequirePartial(attempt, 0, ArtifactLinkObservation.Retained);
        _ = await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => attempt.CleanupAsync(CancellationToken.None));
    }

    /// <summary>Proves successful cleanup preserves the current target observation until actual disposal invalidates it.</summary>
    [TestMethod]
    public async Task CleanupAsyncWhenSuccessfulRetainsTargetPathUntilDisposedAsync()
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        using WindowsOverwriteAttempt attempt = files.Attempt(new DeterministicIdentifierSource(), WindowsTransferContentIo.Native, WindowsTransferArtifactQueries.Native);
        _ = await WindowsOverwriteCleanupFixture.CompleteAsync(attempt);

        await attempt.CleanupAsync(CancellationToken.None);
        Assert.AreEqual(TransferPathObservation.CurrentPathVerified, Assert.IsInstanceOfType<DesiredTargetContentVerified>(attempt.Receipt.Target).Target.PathObservation);
        attempt.Dispose();
        Assert.AreEqual(TransferPathObservation.LastKnownPath, Assert.IsInstanceOfType<DesiredTargetContentVerified>(attempt.Receipt.Target).Target.PathObservation);
    }
}
