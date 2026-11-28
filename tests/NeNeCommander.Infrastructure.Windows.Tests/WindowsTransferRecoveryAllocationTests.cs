using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves oversized recovery input is refused before allocating its snapshot copy.</summary>
[TestClass]
public sealed class WindowsTransferRecoveryAllocationTests
{
    /// <summary>A missing admission guard allocates the entire oversized snapshot even when its later header parse rejects.</summary>
    [TestMethod]
    [DataRow("inspect")]
    [DataRow("completion")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public void DecodeWhenRecordIsOversizedRefusesBeforeAllocatingSnapshot(string operation)
    {
        ReadOnlyMemory<byte> record = new byte[WindowsTransferRecoveryFrame.MaximumRecordLength + 1];
        Action decode = operation == "inspect"
            ? () => WindowsTransferRecoveryCodec.Inspect(record)
            : () => WindowsTransferRecoveryCodec.EncodeCompletion(record);
        _ = Assert.ThrowsExactly<InvalidDataException>(decode);

        long before = GC.GetAllocatedBytesForCurrentThread();
        _ = Assert.ThrowsExactly<InvalidDataException>(decode);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.IsLessThan(record.Length, allocated);
    }
}
