using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves the scripted preflight cannot reinterpret provider cancellation as successful admission.</summary>
[TestClass]
public sealed class ScriptedFileOperationPortTests
{
    /// <summary>Proves an unsupported fixture completion is rejected before constructing a transfer plan.</summary>
    [TestMethod]
    public async Task PreflightTransferAsyncWhenQueuedStepIsCancelledRefusesTheConversion()
    {
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueuePreflight(ProviderStepOutcome.Create(ProviderStepCompletion.Cancelled, ProviderStepReceipt.Empty));
        FileSystemPath destination = Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse("D:\\destination")).Path;

        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
        {
            _ = await port.PreflightTransferAsync([], destination, CancellationToken.None);
        });
    }
}
