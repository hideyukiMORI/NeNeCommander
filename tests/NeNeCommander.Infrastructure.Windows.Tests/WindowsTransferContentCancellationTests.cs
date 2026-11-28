using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the held-content boundary owns cancellation even when an injected native delegate ignores the token.</summary>
[TestClass]
public sealed class WindowsTransferContentCancellationTests
{
    /// <summary>Already cancelled requests never reach an uncooperative native read or write.</summary>
    [TestMethod]
    [DataRow("read")]
    [DataRow("write")]
    [DataRow("emptyRead")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task IoAsyncWhenAlreadyCancelledDoesNotEnterNativeDelegateAsync(string operation)
    {
        using WindowsTransferContentFixture files = WindowsTransferContentFixture.Create("source", "target", 6);
        using CancellationTokenSource cancellation = new();
        int calls = 0;
        WindowsTransferContentIo io = new((handle, buffer, offset, _) =>
        {
            calls++;
            return ValueTask.FromResult(RandomAccess.Read(handle, buffer.Span, offset));
        }, (handle, buffer, offset, _) =>
        {
            calls++;
            RandomAccess.Write(handle, buffer.Span, offset);
            return ValueTask.CompletedTask;
        }, RandomAccess.SetLength, RandomAccess.FlushToDisk);
        cancellation.Cancel();

        _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => InvokeAsync(io, files, operation, cancellation.Token));

        Assert.AreEqual(0, calls);
        Assert.AreEqual("source", files.Read("source"));
        Assert.AreEqual("target", files.Read("target"));
    }

    /// <summary>A cancellation arriving with the native acknowledgement is reported without denying the write's real effect.</summary>
    [TestMethod]
    [DataRow("read")]
    [DataRow("write")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task IoAsyncWhenDelegateCancelsBeforeAcknowledgementReportsCancellationAsync(string operation)
    {
        using WindowsTransferContentFixture files = WindowsTransferContentFixture.Create("source", "target", 6);
        using CancellationTokenSource cancellation = new();
        int calls = 0;
        WindowsTransferContentIo io = new((handle, buffer, offset, _) =>
        {
            calls++;
            int read = RandomAccess.Read(handle, buffer.Span, offset);
            cancellation.Cancel();
            return ValueTask.FromResult(read);
        }, (handle, buffer, offset, _) =>
        {
            calls++;
            RandomAccess.Write(handle, buffer.Span, offset);
            cancellation.Cancel();
            return ValueTask.CompletedTask;
        }, RandomAccess.SetLength, RandomAccess.FlushToDisk);

        _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => InvokeAsync(io, files, operation, cancellation.Token));

        Assert.AreEqual(1, calls);
        Assert.AreEqual("source", files.Read("source"));
        Assert.AreEqual(operation == "write" ? "Xarget" : "target", files.Read("target"));
    }

    /// <summary>The independently destructive truncation acknowledgement cannot turn a cancelled copy into success.</summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(6)]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task CopyAsyncWhenTruncationCancelsRetainsRealEffectAndReportsCancellationAsync(int length)
    {
        string source = length == 0 ? string.Empty : "source";
        using WindowsTransferContentFixture files = WindowsTransferContentFixture.Create(source, "old-target", length);
        using CancellationTokenSource cancellation = new();
        int truncations = 0;
        WindowsTransferContentIo io = new(RandomAccess.ReadAsync, RandomAccess.WriteAsync, (handle, size) =>
        {
            truncations++;
            RandomAccess.SetLength(handle, size);
            cancellation.Cancel();
        }, RandomAccess.FlushToDisk);

        _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => WindowsTransferContentCopy.CopyAsync(files.Pair, io, cancellation.Token));

        Assert.AreEqual(1, truncations);
        Assert.AreEqual(source, files.Read("source"));
        Assert.AreEqual(source, files.Read("target"));
    }

    private static Task InvokeAsync(WindowsTransferContentIo io, WindowsTransferContentFixture files, string operation, CancellationToken token)
    {
        return operation switch
        {
            "read" => io.ReadAsync(files.Pair.Source, new byte[1], 0, token).AsTask(),
            "write" => io.WriteAsync(files.Pair.Target, "X"u8.ToArray(), 0, token).AsTask(),
            "emptyRead" => io.ReadExactlyAsync(files.Pair.Source, Memory<byte>.Empty, 0, token).AsTask(),
            _ => throw new ArgumentOutOfRangeException(nameof(operation)),
        };
    }
}
