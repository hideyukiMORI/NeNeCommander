using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Directories;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves validated provider identity selects exactly one directory reader.</summary>
[TestClass]
public sealed class ProviderDirectoryReadPortTests
{
    /// <summary>Proves Windows local and WSL requests reach only their corresponding adapter.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenProviderIsSupportedDelegatesToItsOnlyReader()
    {
        RecordingDirectoryReadPort windowsLocal = new();
        RecordingDirectoryReadPort wsl = new();
        ProviderDirectoryReadPort router = new(windowsLocal, wsl);

        DirectoryReadOutcome localOutcome = await router.ReadAsync(
            Request("C:\\"),
            CancellationToken.None);
        Assert.AreEqual(1, windowsLocal.InvocationCount);
        Assert.AreEqual(0, wsl.InvocationCount);

        DirectoryReadOutcome wslOutcome = await router.ReadAsync(
            Request("\\\\wsl.localhost\\Ubuntu\\home"),
            CancellationToken.None);

        _ = Assert.IsInstanceOfType<DirectoryReadSucceeded>(localOutcome);
        _ = Assert.IsInstanceOfType<DirectoryReadSucceeded>(wslOutcome);
        Assert.AreEqual(1, windowsLocal.InvocationCount);
        Assert.AreEqual(1, wsl.InvocationCount);
    }

    /// <summary>Proves unsupported providers fail closed without invoking another adapter.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenProviderIsUnsupportedReturnsProviderUnavailable()
    {
        RecordingDirectoryReadPort windowsLocal = new();
        RecordingDirectoryReadPort wsl = new();
        ProviderDirectoryReadPort router = new(windowsLocal, wsl);

        DirectoryReadOutcome outcome = await router.ReadAsync(
            Request("\\\\server\\share\\root"),
            CancellationToken.None);

        Assert.AreSame(
            FileOperationFailureKind.ProviderUnavailable,
            Assert.IsInstanceOfType<DirectoryReadFailed>(outcome).Failure);
        Assert.AreEqual(0, windowsLocal.InvocationCount);
        Assert.AreEqual(0, wsl.InvocationCount);
    }

    /// <summary>Proves every required router argument is rejected at its boundary.</summary>
    [TestMethod]
    public void ConstructorsAndReadAsyncWhenArgumentIsNullRejectDefect()
    {
        RecordingDirectoryReadPort port = new();

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ProviderDirectoryReadPort(null!, port));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ProviderDirectoryReadPort(port, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ProviderDirectoryReadPort(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => new ProviderDirectoryReadPort(port, port).ReadAsync(null!, CancellationToken.None));
    }

    private static DirectoryReadRequest Request(string text)
    {
        FileSystemPath path = Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path;
        DirectoryReadRequestCreation creation = DirectoryReadRequest.Create(path, 8);
        return Assert.IsInstanceOfType<DirectoryReadRequestAccepted>(creation).Request;
    }

    private sealed class RecordingDirectoryReadPort : IDirectoryReadPort
    {
        internal int InvocationCount { get; private set; }

        public Task<DirectoryReadOutcome> ReadAsync(
            DirectoryReadRequest request,
            CancellationToken cancellationToken)
        {
            InvocationCount++;
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
