using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Checks partial-read offset arithmetic before another injected transport callback can observe a wrapped position.</summary>
[TestClass]
public sealed class WindowsTransferOffsetTests
{
    /// <summary>A partial acknowledgement at the largest position cannot wrap the next read to a negative offset.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task ReadExactlyAsyncWhenPartialReadWouldOverflowRejectsBeforeNextCallbackAsync()
    {
        using WindowsTransferContentFixture files = WindowsTransferContentFixture.Create("source", "target", 6);
        int reads = 0;
        WindowsTransferContentIo io = new((_, buffer, offset, _) =>
        {
            reads++;
            Assert.AreEqual(long.MaxValue, offset);
            buffer.Span[0] = (byte)'x';
            return ValueTask.FromResult(1);
        }, RandomAccess.WriteAsync, RandomAccess.SetLength, RandomAccess.FlushToDisk);

        _ = await Assert.ThrowsExactlyAsync<OverflowException>(() =>
            io.ReadExactlyAsync(files.Pair.Source, new byte[2], long.MaxValue, CancellationToken.None).AsTask());

        Assert.AreEqual(1, reads);
        Assert.AreEqual("source", files.Read("source"));
        Assert.AreEqual("target", files.Read("target"));
    }
}
