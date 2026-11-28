using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the prepared destructive stage is consumed before a suspended first read can permit reentry.</summary>
[TestClass]
public sealed class WindowsOverwriteTargetProtocolTests
{
    /// <summary>Proves a second call cannot enter while the first call is suspended in final readmission, and the first owner still completes normally.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-014")]
    public async Task OverwritePreparedAsyncWhenFirstReadIsPendingRefusesConcurrentEntryAsync()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
        bool armed = false;
        bool suspended = false;
        WindowsTransferContentIo io = new(async (handle, bytes, offset, token) =>
        {
            if (armed && !suspended && ReferenceEquals(handle, fixture.Pair.Target.Handle))
            {
                suspended = true;
                entered.SetResult();
                await resume.Task;
            }
            return await RandomAccess.ReadAsync(handle, bytes, offset, token);
        }, RandomAccess.WriteAsync, RandomAccess.SetLength, RandomAccess.FlushToDisk);
        using WindowsOverwriteAttempt attempt = fixture.Attempt(new DeterministicIdentifierSource(), io, WindowsTransferArtifactQueries.Native);
        _ = await attempt.PrepareAsync(CancellationToken.None);
        armed = true;
        Task first = attempt.OverwritePreparedAsync(CancellationToken.None);
        await entered.Task;
        try
        {
            _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.OverwritePreparedAsync(CancellationToken.None));
            _ = Assert.IsInstanceOfType<UntouchedTransferTarget>(attempt.Receipt.Target);
        }
        finally
        {
            resume.SetResult();
            await first;
        }
        _ = Assert.IsInstanceOfType<DesiredTargetContentVerified>(attempt.Receipt.Target);
        Assert.AreEqual("source", fixture.Read(fixture.Pair.Target.Path));
    }
}
