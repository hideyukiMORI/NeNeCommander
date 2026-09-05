using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Wsl;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Wsl;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves WSL distribution discovery is validated as one closed snapshot.</summary>
[TestClass]
public sealed class WslDistributionCatalogTests
{
    /// <summary>Proves valid process lines become distinct, case-preserving WSL roots.</summary>
    [TestMethod]
    public async Task DiscoverAsyncWhenOutputIsValidReturnsDistinctCasePreservingRoots()
    {
        ScriptedWslDistributionProcess process = Process(0, "Ubuntu\r\n\r\nDebian\r\nubuntu\r\n");
        WslDistributionCatalog catalog = new(process);

        WslDistributionCatalogOutcome outcome = await catalog.DiscoverAsync(CancellationToken.None);

        WslDistributionCatalogSucceeded success = Assert.IsInstanceOfType<WslDistributionCatalogSucceeded>(outcome);
        Assert.HasCount(2, success.Roots);
        Assert.AreEqual("Ubuntu", success.Roots[0].DistributionName);
        Assert.AreEqual("\\\\wsl.localhost\\Ubuntu\\", success.Roots[0].CanonicalText);
        Assert.AreEqual("Debian", success.Roots[1].DistributionName);
    }

    /// <summary>Proves empty discovery succeeds and successful outcomes own their snapshot.</summary>
    [TestMethod]
    public async Task DiscoverAsyncWhenOutputIsEmptyReturnsEmptyOwnedSnapshot()
    {
        List<WslPath> source = [];
        WslDistributionCatalogOutcome direct = WslDistributionCatalogOutcome.Succeeded(source);
        source.Add(RequireWslRoot("Later"));

        WslDistributionCatalogSucceeded owned = Assert.IsInstanceOfType<WslDistributionCatalogSucceeded>(direct);
        Assert.IsEmpty(owned.Roots);

        WslDistributionCatalog catalog = new(Process(0, "\r\n"));
        WslDistributionCatalogSucceeded discovered = Assert.IsInstanceOfType<WslDistributionCatalogSucceeded>(
            await catalog.DiscoverAsync(CancellationToken.None));
        Assert.IsEmpty(discovered.Roots);
    }

    /// <summary>Proves one unsafe line or an excessive response rejects the whole snapshot.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-009")]
    public async Task DiscoverAsyncWhenOutputContainsUnsafeOrExcessiveContentFailsClosed()
    {
        string excessiveLines = string.Join("\n", Enumerable.Range(0, WslDistributionCatalog.DistributionBoundary + 1)
            .Select(index => "D" + index));
        string boundaryLines = string.Join("\n", Enumerable.Range(0, WslDistributionCatalog.DistributionBoundary)
            .Select(index => "D" + index));

        WslDistributionCatalogFailed unsafeName = Assert.IsInstanceOfType<WslDistributionCatalogFailed>(
            await new WslDistributionCatalog(Process(0, "Ubuntu\r\nUbuntu;shutdown\r\n"))
                .DiscoverAsync(CancellationToken.None));
        WslDistributionCatalogFailed childPath = Assert.IsInstanceOfType<WslDistributionCatalogFailed>(
            await new WslDistributionCatalog(Process(0, "Ubuntu\\home\r\n"))
                .DiscoverAsync(CancellationToken.None));
        WslDistributionCatalogFailed excessiveLineCount = Assert.IsInstanceOfType<WslDistributionCatalogFailed>(
            await new WslDistributionCatalog(Process(0, excessiveLines)).DiscoverAsync(CancellationToken.None));
        WslDistributionCatalogSucceeded acceptedBoundary = Assert.IsInstanceOfType<WslDistributionCatalogSucceeded>(
            await new WslDistributionCatalog(Process(0, boundaryLines)).DiscoverAsync(CancellationToken.None));

        Assert.AreSame(WslDistributionCatalogFailureKind.MalformedOutput, unsafeName.Failure);
        Assert.AreSame(WslDistributionCatalogFailureKind.MalformedOutput, childPath.Failure);
        Assert.AreSame(WslDistributionCatalogFailureKind.MalformedOutput, excessiveLineCount.Failure);
        Assert.HasCount(WslDistributionCatalog.DistributionBoundary, acceptedBoundary.Roots);
    }

    /// <summary>Proves process failure and cancellation become closed outcomes without partial data.</summary>
    [TestMethod]
    public async Task DiscoverAsyncWhenProviderFailsOrCancellationIsRequestedReturnsClosedOutcome()
    {
        ScriptedWslDistributionProcess exitFailure = Process(1, "Ubuntu\r\n");
        UnavailableWslDistributionProcess startFailure = new();
        UnreadableWslDistributionProcess streamFailure = new();
        MalformedWslDistributionProcess malformedProcess = new();
        ScriptedWslDistributionProcess cancelledProcess = Process(0, "Ubuntu\r\n");
        using CancellationTokenSource preCancellation = new();
        using CancellationTokenSource activeCancellation = new();
        preCancellation.Cancel();
        CancellingWslDistributionProcess cancellingProcess = new(activeCancellation);

        WslDistributionCatalogFailed nonzero = Assert.IsInstanceOfType<WslDistributionCatalogFailed>(
            await new WslDistributionCatalog(exitFailure).DiscoverAsync(CancellationToken.None));
        WslDistributionCatalogFailed unavailable = Assert.IsInstanceOfType<WslDistributionCatalogFailed>(
            await new WslDistributionCatalog(startFailure).DiscoverAsync(CancellationToken.None));
        WslDistributionCatalogFailed unreadable = Assert.IsInstanceOfType<WslDistributionCatalogFailed>(
            await new WslDistributionCatalog(streamFailure).DiscoverAsync(CancellationToken.None));
        WslDistributionCatalogFailed malformed = Assert.IsInstanceOfType<WslDistributionCatalogFailed>(
            await new WslDistributionCatalog(malformedProcess).DiscoverAsync(CancellationToken.None));
        _ = Assert.IsInstanceOfType<WslDistributionCatalogCancelled>(
            await new WslDistributionCatalog(cancelledProcess).DiscoverAsync(preCancellation.Token));
        _ = Assert.IsInstanceOfType<WslDistributionCatalogCancelled>(
            await new WslDistributionCatalog(cancellingProcess).DiscoverAsync(activeCancellation.Token));

        Assert.AreSame(WslDistributionCatalogFailureKind.ProviderUnavailable, nonzero.Failure);
        Assert.AreSame(WslDistributionCatalogFailureKind.ProviderUnavailable, unavailable.Failure);
        Assert.AreSame(WslDistributionCatalogFailureKind.ProviderUnavailable, unreadable.Failure);
        Assert.AreSame(WslDistributionCatalogFailureKind.MalformedOutput, malformed.Failure);
        Assert.AreEqual(0, cancelledProcess.InvocationCount);
        Assert.AreEqual(1, cancellingProcess.InvocationCount);
    }

    private static ScriptedWslDistributionProcess Process(int exitCode, string output)
    {
        return new ScriptedWslDistributionProcess(new WslDistributionProcessResult(exitCode, output));
    }

    /// <summary>Proves catalog and process-result null boundaries reject adapter defects.</summary>
    [TestMethod]
    public void ConstructorsAndParserWhenDependenciesAreNullRejectDefect()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WslDistributionCatalog(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WslDistributionProcessResult(0, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => WslDistributionCatalog.ParseOutput(null!));
    }

    private static WslPath RequireWslRoot(string distributionName)
    {
        PathParseSuccess parsed = Assert.IsInstanceOfType<PathParseSuccess>(
            FileSystemPath.Parse("\\\\wsl.localhost\\" + distributionName));
        return Assert.IsInstanceOfType<WslPath>(parsed.Path);
    }

    private sealed class CancellingWslDistributionProcess : IWslDistributionProcess
    {
        private readonly CancellationTokenSource _cancellation;

        internal CancellingWslDistributionProcess(CancellationTokenSource cancellation)
        {
            _cancellation = cancellation;
        }

        internal int InvocationCount { get; private set; }

        public Task<WslDistributionProcessResult> ListAsync(CancellationToken cancellationToken)
        {
            InvocationCount++;
            _cancellation.Cancel();
            return Task.FromCanceled<WslDistributionProcessResult>(cancellationToken);
        }
    }

    private sealed class UnavailableWslDistributionProcess : IWslDistributionProcess
    {
        public Task<WslDistributionProcessResult> ListAsync(CancellationToken cancellationToken)
        {
            throw new Win32Exception(2);
        }
    }

    private sealed class MalformedWslDistributionProcess : IWslDistributionProcess
    {
        public Task<WslDistributionProcessResult> ListAsync(CancellationToken cancellationToken)
        {
            throw new InvalidDataException("Synthetic oversized output.");
        }
    }

    private sealed class UnreadableWslDistributionProcess : IWslDistributionProcess
    {
        public Task<WslDistributionProcessResult> ListAsync(CancellationToken cancellationToken)
        {
            throw new IOException("Synthetic standard-stream failure.");
        }
    }
}
