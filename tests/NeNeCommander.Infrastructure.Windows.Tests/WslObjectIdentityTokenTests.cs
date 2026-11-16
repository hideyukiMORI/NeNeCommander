using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Infrastructure.Windows.FileOperations;
using NeNeCommander.Infrastructure.Windows.Wsl;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the exact WSL object token text and the cancelled distribution listing boundary.</summary>
[TestClass]
public sealed class WslObjectIdentityTokenTests
{
    /// <summary>Proves the object token is versioned, distribution-case-folded and keeps the hexadecimal tag.</summary>
    [TestMethod]
    public void ComposeWslObjectTokenWhenFactsAreLinkComposesExactToken()
    {
        WslHandleFacts facts = new(41, 0x400, 0xA000001D, new WslRewriteFacts(7, 2, 101, 103));

        string token = WindowsFileIdentifier.ComposeWslObjectToken("Ubuntu", facts);

        Assert.AreEqual("wsl-object-v1|UBUNTU|41|link|A000001D", token);
    }

    /// <summary>Proves a cancelled listing never starts the WSL process.</summary>
    [TestMethod]
    public async Task ListAsyncWhenCancelledBeforeStartThrowsWithoutStartingAsync()
    {
        int starts = 0;
        WslDistributionProcess process = new(_ =>
        {
            starts++;
            return null;
        });
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => process.ListAsync(cancellation.Token));

        Assert.AreEqual(0, starts);
    }
}
