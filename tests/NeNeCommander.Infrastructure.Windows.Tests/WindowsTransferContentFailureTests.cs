using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves cancellation and partial native effects never become successful content evidence.</summary>
[TestClass]
public sealed class WindowsTransferContentFailureTests
{
    /// <summary>Proves cancellation before entry invokes no native read, write or truncate.</summary>
    [TestMethod]
    public async Task CopyAsyncWhenAlreadyCancelledHasNoIoEffectsAsync()
    {
        using WindowsTransferContentFixture fixture = WindowsTransferContentFixture.Create("abc", "original", 3);
        using CancellationTokenSource cancellation = new();
        int calls = 0;
        WindowsTransferContentIo io = new((handle, buffer, offset, token) =>
        {
            calls++;
            return RandomAccess.ReadAsync(handle, buffer, offset, token);
        }, (handle, buffer, offset, token) =>
        {
            calls++;
            return RandomAccess.WriteAsync(handle, buffer, offset, token);
        }, (handle, length) =>
        {
            calls++;
            RandomAccess.SetLength(handle, length);
        }, RandomAccess.FlushToDisk);
        cancellation.Cancel();

        _ = await Assert.ThrowsAsync<OperationCanceledException>(() => WindowsTransferContentCopy.CopyAsync(fixture.Pair, io, cancellation.Token));
        Assert.AreEqual(0, calls);
        Assert.AreEqual("original", fixture.Read("target"));
    }

    /// <summary>Proves cancellation after a completed read prevents its following write.</summary>
    [TestMethod]
    public async Task CopyAsyncWhenCancellationArrivesAfterReadDoesNotWriteAsync()
    {
        using WindowsTransferContentFixture fixture = WindowsTransferContentFixture.Create("abc", "original", 3);
        using CancellationTokenSource cancellation = new();
        int writes = 0;
        WindowsTransferContentIo io = new(async (handle, buffer, offset, token) =>
        {
            int count = await RandomAccess.ReadAsync(handle, buffer, offset, token);
            cancellation.Cancel();
            return count;
        }, (handle, buffer, offset, token) =>
        {
            writes++;
            return RandomAccess.WriteAsync(handle, buffer, offset, token);
        }, RandomAccess.SetLength, RandomAccess.FlushToDisk);

        _ = await Assert.ThrowsAsync<OperationCanceledException>(() => WindowsTransferContentCopy.CopyAsync(fixture.Pair, io, cancellation.Token));
        Assert.AreEqual(0, writes);
        Assert.AreEqual("original", fixture.Read("target"));
    }

    /// <summary>Proves a thrown write can leave actual bytes and prevents later truncate or successful completion.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task CopyAsyncWhenWriteThrowsAfterPartialEffectRetainsActualPartialBytesAsync()
    {
        using WindowsTransferContentFixture fixture = WindowsTransferContentFixture.Create("0123456789", string.Empty, 10);
        int truncates = 0;
        WindowsTransferContentIo io = new(RandomAccess.ReadAsync, async (handle, buffer, offset, token) =>
        {
            await RandomAccess.WriteAsync(handle, buffer[..3], offset, token);
            throw new IOException("Injected failure after partial native write.");
        }, (handle, length) =>
        {
            truncates++;
            RandomAccess.SetLength(handle, length);
        }, RandomAccess.FlushToDisk);

        _ = await Assert.ThrowsExactlyAsync<IOException>(() => WindowsTransferContentCopy.CopyAsync(fixture.Pair, io, CancellationToken.None));
        Assert.AreEqual("012", fixture.Read("target"));
        Assert.AreEqual("0123456789", File.ReadAllText(fixture.SourcePath));
        Assert.AreEqual(0, truncates);
    }

    /// <summary>Proves cancellation after the first complete chunk reports interruption with the already written bytes retained.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task CopyAsyncWhenCancellationArrivesAfterWriteRetainsWrittenChunkAsync()
    {
        using WindowsTransferContentFixture fixture = WindowsTransferContentFixture.Create(new string('a', 65537), string.Empty, 65537);
        using CancellationTokenSource cancellation = new();
        int writes = 0;
        int truncates = 0;
        WindowsTransferContentIo io = new(RandomAccess.ReadAsync, async (handle, buffer, offset, token) =>
        {
            writes++;
            await RandomAccess.WriteAsync(handle, buffer, offset, token);
            cancellation.Cancel();
        }, (handle, length) =>
        {
            truncates++;
            RandomAccess.SetLength(handle, length);
        }, RandomAccess.FlushToDisk);

        _ = await Assert.ThrowsAsync<OperationCanceledException>(() => WindowsTransferContentCopy.CopyAsync(fixture.Pair, io, cancellation.Token));
        Assert.AreEqual(1, writes);
        Assert.AreEqual(0, truncates);
        Assert.AreEqual(65536L, RandomAccess.GetLength(fixture.Pair.Target));
        Assert.AreEqual(65537L, RandomAccess.GetLength(fixture.Pair.Source));
    }

    /// <summary>Proves an exception after truncation cannot be described as no effect.</summary>
    [TestMethod]
    public async Task CopyAsyncWhenTruncateThrowsAfterEffectLeavesChangedArtifactAsync()
    {
        using WindowsTransferContentFixture fixture = WindowsTransferContentFixture.Create(string.Empty, "original", 0);
        WindowsTransferContentIo io = new(RandomAccess.ReadAsync, RandomAccess.WriteAsync, (handle, length) =>
        {
            RandomAccess.SetLength(handle, length);
            throw new IOException("Injected failure after native truncate.");
        }, RandomAccess.FlushToDisk);

        _ = await Assert.ThrowsExactlyAsync<IOException>(() => WindowsTransferContentCopy.CopyAsync(fixture.Pair, io, CancellationToken.None));
        Assert.AreEqual(0L, RandomAccess.GetLength(fixture.Pair.Target));
    }

    /// <summary>Proves malformed read counts cannot overrun the buffer or loop as successful content.</summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(65537)]
    public async Task CopyAsyncWhenReadReturnsInvalidCountRefusesItAsync(int count)
    {
        using WindowsTransferContentFixture fixture = WindowsTransferContentFixture.Create("abc", "original", 3);
        WindowsTransferContentIo io = new((handle, buffer, offset, token) => ValueTask.FromResult(count),
            RandomAccess.WriteAsync, RandomAccess.SetLength, RandomAccess.FlushToDisk);

        _ = await Assert.ThrowsExactlyAsync<IOException>(() => WindowsTransferContentCopy.CopyAsync(fixture.Pair, io, CancellationToken.None));
        Assert.AreEqual("original", fixture.Read("target"));
    }

    /// <summary>Proves cancellation at the last EOF observation still prevents a successful verification result.</summary>
    [TestMethod]
    public async Task CompareHeldAsyncWhenCancellationArrivesAtFinalEofReturnsNoEvidenceAsync()
    {
        using WindowsTransferContentFixture fixture = WindowsTransferContentFixture.Create("abc", "abc", 3);
        using CancellationTokenSource cancellation = new();
        WindowsTransferContentIo io = new(async (handle, buffer, offset, token) =>
        {
            int count = await RandomAccess.ReadAsync(handle, buffer, offset, token);
            if (ReferenceEquals(handle, fixture.Pair.Target) && offset == 3)
            {
                cancellation.Cancel();
            }
            return count;
        }, RandomAccess.WriteAsync, RandomAccess.SetLength, RandomAccess.FlushToDisk);

        _ = await Assert.ThrowsAsync<OperationCanceledException>(() => VerifiedFileContent.CompareHeldAsync(fixture.Pair, io, cancellation.Token));
        Assert.AreEqual("abc", File.ReadAllText(fixture.SourcePath));
        Assert.AreEqual("abc", fixture.Read("target"));
    }

    /// <summary>Proves an oversized request is refused before invoking the native delegate.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenBufferExceedsBoundRefusesBeforeNativeIoAsync()
    {
        using WindowsTransferContentFixture fixture = WindowsTransferContentFixture.Create("abc", "abc", 3);
        int calls = 0;
        WindowsTransferContentIo io = new((handle, buffer, offset, token) =>
        {
            calls++;
            return RandomAccess.ReadAsync(handle, buffer, offset, token);
        }, (handle, buffer, offset, token) =>
        {
            calls++;
            return RandomAccess.WriteAsync(handle, buffer, offset, token);
        }, RandomAccess.SetLength, RandomAccess.FlushToDisk);
        byte[] oversized = new byte[65537];

        _ = await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(() => io.ReadAsync(fixture.Pair.Source, oversized, 0, CancellationToken.None).AsTask());
        _ = await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(() => io.WriteAsync(fixture.Pair.Target, oversized, 0, CancellationToken.None).AsTask());
        Assert.AreEqual(0, calls);
    }
}
