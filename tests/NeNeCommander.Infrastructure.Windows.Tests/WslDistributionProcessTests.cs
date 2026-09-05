using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Infrastructure.Windows.Wsl;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves WSL discovery can start only the fixed non-shell invocation.</summary>
[TestClass]
public sealed class WslDistributionProcessTests
{
    /// <summary>Proves process name, argument tokens, redirection, encoding, and window policy.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-009")]
    public void CreateStartInfoReturnsOnlyFixedQuietListInvocation()
    {
        ProcessStartInfo startInfo = WslDistributionProcess.CreateStartInfo();

        Assert.HasCount(2, startInfo.ArgumentList);
        Assert.AreEqual("--list", startInfo.ArgumentList[0]);
        Assert.AreEqual("--quiet", startInfo.ArgumentList[1]);
        Assert.AreEqual("wsl.exe", startInfo.FileName);
        Assert.IsTrue(startInfo.CreateNoWindow);
        Assert.IsTrue(startInfo.RedirectStandardError);
        Assert.IsTrue(startInfo.RedirectStandardOutput);
        Assert.AreSame(Encoding.Unicode, startInfo.StandardErrorEncoding);
        Assert.AreSame(Encoding.Unicode, startInfo.StandardOutputEncoding);
        Assert.IsFalse(startInfo.UseShellExecute);
    }

    /// <summary>Proves process text is accepted only through the fixed character boundary.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    public async Task ReadBoundedAsyncAtAndBeyondBoundaryAcceptsOnlyBoundedText()
    {
        string boundary = new('a', WslDistributionCatalog.OutputCharacterBoundary);
        bool terminated = false;
        using StringReader boundaryReader = new(boundary);
        using StringReader oversizedReader = new(boundary + "a");

        string accepted = await WslDistributionProcess.ReadBoundedAsync(
            boundaryReader,
            () => terminated = true,
            CancellationToken.None);
        _ = await Assert.ThrowsExactlyAsync<InvalidDataException>(
            () => WslDistributionProcess.ReadBoundedAsync(
                oversizedReader,
                () => terminated = true,
                CancellationToken.None));

        Assert.AreEqual(boundary, accepted);
        Assert.IsTrue(terminated);
    }

    /// <summary>Proves successful child completion drains both streams and reports its exit code.</summary>
    [TestMethod]
    public async Task ListAsyncWhenTestOwnedProcessCompletesReturnsItsOutput()
    {
        WslDistributionProcess process = new(_ => StartCommand("echo catalog", Encoding.Default));

        WslDistributionProcessResult result = await process.ListAsync(CancellationToken.None);

        Assert.AreEqual(0, result.ExitCode);
        StringAssert.Contains(result.StandardOutput, "catalog");
    }

    /// <summary>Proves active cancellation terminates and observes the process owned by the invocation.</summary>
    [TestMethod]
    public async Task ListAsyncWhenCancelledAfterStartThrowsOperationCancelledException()
    {
        WslDistributionProcess process = new(_ => StartPing());
        using CancellationTokenSource cancellation = new();

        Task<WslDistributionProcessResult> pending = process.ListAsync(cancellation.Token);
        cancellation.Cancel();

        _ = await Assert.ThrowsAsync<OperationCanceledException>(() => pending);
    }

    /// <summary>Proves oversized child output terminates the owned process and preserves the boundary error.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-009")]
    public async Task ListAsyncWhenTestOwnedProcessExceedsOutputBoundaryThrowsInvalidDataException()
    {
        const string command = "for /L %i in (1,1,7000) do @echo 0123456789";
        WslDistributionProcess process = new(_ => StartCommand(command, Encoding.Default));

        InvalidDataException exception = await Assert.ThrowsExactlyAsync<InvalidDataException>(
            () => process.ListAsync(CancellationToken.None));

        StringAssert.Contains(exception.Message, "output exceeded its boundary");
    }

    /// <summary>Proves null factories and failed starts are rejected at the process boundary.</summary>
    [TestMethod]
    public async Task ConstructorAndListAsyncWhenStartFactoryIsInvalidFailClosed()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WslDistributionProcess(null!));
        WslDistributionProcess process = new(_ => null);

        Win32Exception exception = await Assert.ThrowsExactlyAsync<Win32Exception>(
            () => process.ListAsync(CancellationToken.None));

        StringAssert.Contains(exception.Message, "did not start");
    }

    private static Process StartCommand(string command, Encoding encoding)
    {
        ProcessStartInfo startInfo = CreateTestStartInfo("cmd.exe", encoding);
        startInfo.ArgumentList.Add("/d");
        startInfo.ArgumentList.Add("/c");
        startInfo.ArgumentList.Add(command);
        return Process.Start(startInfo) ?? throw new InvalidOperationException("Test-owned command did not start.");
    }

    private static Process StartPing()
    {
        ProcessStartInfo startInfo = CreateTestStartInfo("ping.exe", Encoding.Default);
        startInfo.ArgumentList.Add("-n");
        startInfo.ArgumentList.Add("30");
        startInfo.ArgumentList.Add("127.0.0.1");
        return Process.Start(startInfo) ?? throw new InvalidOperationException("Test-owned ping did not start.");
    }

    private static ProcessStartInfo CreateTestStartInfo(string fileName, Encoding encoding)
    {
        return new ProcessStartInfo
        {
            FileName = fileName,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            StandardErrorEncoding = encoding,
            StandardOutputEncoding = encoding,
            UseShellExecute = false,
        };
    }
}
