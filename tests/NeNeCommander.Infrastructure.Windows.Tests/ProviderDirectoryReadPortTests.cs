using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Directories;
using NeNeCommander.Infrastructure.Windows.Execution;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves validated provider identity selects exactly one directory reader.</summary>
[TestClass]
public sealed class ProviderDirectoryReadPortTests
{
    /// <summary>Proves Windows local, UNC, and WSL requests each reach only their corresponding adapter.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenProviderIsSupportedDelegatesToItsOnlyReader()
    {
        RecordingDirectoryReadPort windowsLocal = new();
        RecordingDirectoryReadPort windowsUnc = new();
        RecordingDirectoryReadPort wsl = new();
        ProviderDirectoryReadPort router = new(windowsLocal, windowsUnc, wsl);

        DirectoryReadOutcome localOutcome = await router.ReadAsync(
            Request("C:\\"),
            CancellationToken.None);
        AssertInvocations(windowsLocal, windowsUnc, wsl, 1, 0, 0);

        DirectoryReadOutcome uncOutcome = await router.ReadAsync(
            Request("\\\\server\\share\\root"),
            CancellationToken.None);
        AssertInvocations(windowsLocal, windowsUnc, wsl, 1, 1, 0);

        DirectoryReadOutcome wslOutcome = await router.ReadAsync(
            Request("\\\\wsl.localhost\\Ubuntu\\home"),
            CancellationToken.None);

        _ = Assert.IsInstanceOfType<DirectoryReadSucceeded>(localOutcome);
        _ = Assert.IsInstanceOfType<DirectoryReadSucceeded>(uncOutcome);
        _ = Assert.IsInstanceOfType<DirectoryReadSucceeded>(wslOutcome);
        AssertInvocations(windowsLocal, windowsUnc, wsl, 1, 1, 1);
    }

    /// <summary>
    /// Proves the WSL namespace, which is also UNC-shaped text, never reaches the UNC adapter, and
    /// that both legacy and current WSL roots stay with the WSL adapter.
    /// </summary>
    [TestMethod]
    public async Task ReadAsyncWhenWslNamespaceIsUncShapedRoutesOnlyToWsl()
    {
        RecordingDirectoryReadPort windowsLocal = new();
        RecordingDirectoryReadPort windowsUnc = new();
        RecordingDirectoryReadPort wsl = new();
        ProviderDirectoryReadPort router = new(windowsLocal, windowsUnc, wsl);

        _ = await router.ReadAsync(Request("\\\\wsl$\\Ubuntu\\home"), CancellationToken.None);
        _ = await router.ReadAsync(Request("\\\\WSL.LOCALHOST\\Ubuntu\\"), CancellationToken.None);

        AssertInvocations(windowsLocal, windowsUnc, wsl, 0, 0, 2);
    }

    /// <summary>
    /// Proves a failed UNC read is returned as the UNC adapter reported it, with no retry and no
    /// fallback to the Windows local or WSL adapter.
    /// </summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-021")]
    public async Task ReadAsyncWhenUncReadFailsReturnsThatFailureWithoutFallback()
    {
        RecordingDirectoryReadPort windowsLocal = new();
        RecordingDirectoryReadPort windowsUnc = new(FileOperationFailureKind.AccessDenied);
        RecordingDirectoryReadPort wsl = new();
        ProviderDirectoryReadPort router = new(windowsLocal, windowsUnc, wsl);

        DirectoryReadOutcome outcome = await router.ReadAsync(
            Request("\\\\server\\share\\root"),
            CancellationToken.None);

        Assert.AreSame(
            FileOperationFailureKind.AccessDenied,
            Assert.IsInstanceOfType<DirectoryReadFailed>(outcome).Failure);
        AssertInvocations(windowsLocal, windowsUnc, wsl, 0, 1, 0);
    }

    /// <summary>
    /// Proves the production composition routes a UNC location to a reader that accepts it: the
    /// request reaches the shared operation, which observes the prior cancellation before any
    /// enumeration, so no share is contacted and the outcome is not the router's refusal.
    /// </summary>
    [TestMethod]
    public async Task ReadAsyncWhenComposedRoutesUncToReaderThatAcceptsIt()
    {
        ProviderDirectoryReadPort router = new(new WindowsLocalIoExecutionBoundary());
        using CancellationTokenSource cancelled = new();
        cancelled.Cancel();

        DirectoryReadOutcome outcome = await router.ReadAsync(
            Request("\\\\server\\share\\root"),
            cancelled.Token);

        _ = Assert.IsInstanceOfType<DirectoryReadCancelled>(outcome);
    }

    /// <summary>Proves every required router argument is rejected at its boundary.</summary>
    [TestMethod]
    public void ConstructorsAndReadAsyncWhenArgumentIsNullRejectDefect()
    {
        RecordingDirectoryReadPort port = new();

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ProviderDirectoryReadPort(null!, port, port));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ProviderDirectoryReadPort(port, null!, port));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ProviderDirectoryReadPort(port, port, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ProviderDirectoryReadPort(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => new ProviderDirectoryReadPort(port, port, port).ReadAsync(null!, CancellationToken.None));
    }

    private static void AssertInvocations(
        RecordingDirectoryReadPort windowsLocal,
        RecordingDirectoryReadPort windowsUnc,
        RecordingDirectoryReadPort wsl,
        int expectedLocal,
        int expectedUnc,
        int expectedWsl)
    {
        Assert.AreEqual(expectedLocal, windowsLocal.InvocationCount);
        Assert.AreEqual(expectedUnc, windowsUnc.InvocationCount);
        Assert.AreEqual(expectedWsl, wsl.InvocationCount);
    }

    private static DirectoryReadRequest Request(string text)
    {
        FileSystemPath path = Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path;
        DirectoryReadRequestCreation creation = DirectoryReadRequest.Create(path, 8);
        return Assert.IsInstanceOfType<DirectoryReadRequestAccepted>(creation).Request;
    }

    private sealed class RecordingDirectoryReadPort : IDirectoryReadPort
    {
        private readonly FileOperationFailureKind? _failure;

        internal RecordingDirectoryReadPort()
        {
        }

        internal RecordingDirectoryReadPort(FileOperationFailureKind failure)
        {
            _failure = failure;
        }

        internal int InvocationCount { get; private set; }

        public Task<DirectoryReadOutcome> ReadAsync(
            DirectoryReadRequest request,
            CancellationToken cancellationToken)
        {
            InvocationCount++;
            if (_failure is not null)
            {
                return Task.FromResult(DirectoryReadOutcome.Failed(_failure));
            }
            DirectoryListingCreation creation = DirectoryListing.Create(
                request.Location,
                [],
                DirectoryListingCompleteness.Complete,
                0);
            DirectoryListing listing = Assert.IsInstanceOfType<DirectoryListingAccepted>(creation).Listing;
            return Task.FromResult(DirectoryReadOutcome.Succeeded(listing));
        }
    }
}
