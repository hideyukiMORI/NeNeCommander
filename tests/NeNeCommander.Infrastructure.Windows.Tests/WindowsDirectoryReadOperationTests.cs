using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Directories;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the shared read owns cancellation and both deferred-enumeration failure boundaries.</summary>
[TestClass]
public sealed class WindowsDirectoryReadOperationTests
{
    /// <summary>Proves an already cancelled read never invokes the provider callback.</summary>
    [TestMethod]
    public void ReadWhenCancelledBeforeEnumerationDoesNotInvokeProvider()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        int invocationCount = 0;

        DirectoryReadOutcome outcome = WindowsDirectoryReadOperation.Read(
            Request(),
            () =>
            {
                invocationCount++;
                throw new IOException("The cancelled provider must remain untouched.");
            },
            WindowsDirectoryReadOperation.ClassifyByAttributes,
            cancellation.Token);

        _ = Assert.IsInstanceOfType<DirectoryReadCancelled>(outcome);
        Assert.AreEqual(0, invocationCount);
    }

    /// <summary>Proves a provider that throws while creating its enumerable is normalized.</summary>
    [TestMethod]
    public void ReadWhenProviderCallbackThrowsNormalizesFailure()
    {
        DirectoryReadOutcome denied = WindowsDirectoryReadOperation.Read(
            Request(),
            () => throw new UnauthorizedAccessException(),
            WindowsDirectoryReadOperation.ClassifyByAttributes,
            CancellationToken.None);
        DirectoryReadOutcome missing = WindowsDirectoryReadOperation.Read(
            Request(),
            () => throw new DirectoryNotFoundException(),
            WindowsDirectoryReadOperation.ClassifyByAttributes,
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.AccessDenied, Assert.IsInstanceOfType<DirectoryReadFailed>(denied).Failure);
        Assert.AreSame(FileOperationFailureKind.NotFound, Assert.IsInstanceOfType<DirectoryReadFailed>(missing).Failure);
    }

    /// <summary>Proves a lazy iterator failure after an entry discards the partial listing.</summary>
    [TestMethod]
    public void ReadWhenIteratorThrowsAfterEntryNormalizesFailureWithoutPartialListing()
    {
        DirectoryReadOutcome denied = WindowsDirectoryReadOperation.Read(
            Request(),
            () => EntriesThenFailure(new UnauthorizedAccessException()),
            WindowsDirectoryReadOperation.ClassifyByAttributes,
            CancellationToken.None);
        DirectoryReadOutcome unavailable = WindowsDirectoryReadOperation.Read(
            Request(),
            () => EntriesThenFailure(new IOException("Synthetic provider loss.")),
            WindowsDirectoryReadOperation.ClassifyByAttributes,
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.AccessDenied, Assert.IsInstanceOfType<DirectoryReadFailed>(denied).Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, Assert.IsInstanceOfType<DirectoryReadFailed>(unavailable).Failure);
    }

    private static IEnumerable<WindowsDirectoryEntrySnapshot> EntriesThenFailure(Exception failure)
    {
        yield return new WindowsDirectoryEntrySnapshot("one", FileAttributes.None, EntrySize.Unknown, EntryTimestamp.Unknown);
        throw failure;
    }

    private static DirectoryReadRequest Request()
    {
        FileSystemPath path = Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse("C:\\")).Path;
        return Assert.IsInstanceOfType<DirectoryReadRequestAccepted>(DirectoryReadRequest.Create(path, 8)).Request;
    }
}
