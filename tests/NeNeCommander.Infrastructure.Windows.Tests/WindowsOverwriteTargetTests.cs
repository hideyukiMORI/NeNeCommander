using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves actual exact-target content changes retain canonical evidence without claiming cleanup, copy completion or source deletion.</summary>
[TestClass]
public sealed class WindowsOverwriteTargetTests
{
    /// <summary>Proves empty, shorter, longer and multibuffer desired contents retain the original target object and original backup.</summary>
    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(0, 9)]
    [DataRow(3, 9)]
    [DataRow(131073, 2)]
    public async Task OverwritePreparedAsyncWhenContentVerifiesRetainsExactObjectAndPreparationsAsync(int sourceLength, int targetLength)
    {
        string source = new('s', sourceLength);
        string target = new('t', targetLength);
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create(source, target);
        using WindowsOverwriteAttempt attempt = fixture.Attempt(new DeterministicIdentifierSource(), WindowsTransferContentIo.Native, WindowsTransferArtifactQueries.Native);
        WindowsTransferRecoveryMapping prepared = await attempt.PrepareAsync(CancellationToken.None);
        WindowsLocalHandleFacts before = WindowsFileIdentifier.ReadLocalFacts(fixture.Pair.Target.Handle);

        await attempt.OverwritePreparedAsync(CancellationToken.None);
        DesiredTargetContentVerified result = Assert.IsInstanceOfType<DesiredTargetContentVerified>(attempt.Receipt.Target);
        WindowsLocalHandleFacts after = WindowsFileIdentifier.ReadLocalFacts(fixture.Pair.Target.Handle);
        Assert.AreEqual(source, fixture.Read(fixture.Pair.Target.Path));
        Assert.AreEqual(source, fixture.Read(fixture.Pair.Source.Path));
        Assert.AreEqual(target, fixture.Read(prepared.Names.OriginalBackup));
        Assert.AreEqual(source, fixture.Read(prepared.Names.PreparedContent));
        Assert.AreEqual(before.ObjectIdentity, after.ObjectIdentity);
        Assert.AreEqual(before.Basic.CreationFileTimeUtc, after.Basic.CreationFileTimeUtc);
        Assert.AreEqual(prepared.Desired, result.Content);
        Assert.AreEqual(WindowsLocalEntryIdentity.CaptureHeld(fixture.Pair.Target.Path, after), result.Target.Entry);
        Assert.HasCount(0, attempt.Receipt.Effects);
        Assert.HasCount(3, attempt.Receipt.Artifacts);
        Assert.IsTrue(attempt.Receipt.Artifacts.Cast<IdentifiedTransferArtifact>().All(artifact => artifact.Link == ArtifactLinkObservation.Retained));
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.OverwritePreparedAsync(CancellationToken.None));
        attempt.Dispose();
        DesiredTargetContentVerified retained = Assert.IsInstanceOfType<DesiredTargetContentVerified>(attempt.Receipt.Target);
        Assert.AreEqual(result.Content, retained.Content);
        Assert.AreEqual(TransferPathObservation.LastKnownPath, retained.Target.PathObservation);
        fixture.Pair.Target.RevalidateObject();
        _ = await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => attempt.OverwritePreparedAsync(CancellationToken.None));
    }

    /// <summary>Proves an unprepared attempt cannot begin target mutation.</summary>
    [TestMethod]
    public async Task OverwritePreparedAsyncWhenPreparationIsAbsentRefusesWithoutEffectsAsync()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        using WindowsOverwriteAttempt attempt = fixture.Attempt(new DeterministicIdentifierSource(), WindowsTransferContentIo.Native, WindowsTransferArtifactQueries.Native);

        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.OverwritePreparedAsync(CancellationToken.None));
        Assert.HasCount(0, attempt.Receipt.Artifacts);
        fixture.RequireUntouched(attempt, "source", "target");
    }

    /// <summary>Proves cancellation consumes the prepared transition before any await or target call and cannot later replay it.</summary>
    [TestMethod]
    public async Task OverwritePreparedAsyncWhenAlreadyCancelledConsumesPreparationAndRetainsUntouchedAsync()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        using WindowsOverwriteAttempt attempt = fixture.Attempt(new DeterministicIdentifierSource(), WindowsTransferContentIo.Native, WindowsTransferArtifactQueries.Native);
        _ = await attempt.PrepareAsync(CancellationToken.None);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        _ = await Assert.ThrowsAsync<OperationCanceledException>(() => attempt.OverwritePreparedAsync(cancellation.Token));
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.OverwritePreparedAsync(CancellationToken.None));
        Assert.HasCount(3, attempt.Receipt.Artifacts);
        fixture.RequireUntouched(attempt, "source", "target");
    }
}
