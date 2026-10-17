using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Drives;
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

        DirectoryReadOutcome outcome = new WindowsDirectoryReadOperation(
            () =>
            {
                invocationCount++;
                throw new IOException("The cancelled provider must remain untouched.");
            },
            WindowsDirectoryReadOperation.ClassifyByAttributes,
            () => null).Read(Request(), cancellation.Token);

        _ = Assert.IsInstanceOfType<DirectoryReadCancelled>(outcome);
        Assert.AreEqual(0, invocationCount);
    }

    /// <summary>A cancelled drive observation never begins directory enumeration.</summary>
    [TestMethod]
    public void ReadWhenCancelledDuringDriveObservationSkipsEnumeration()
    {
        using CancellationTokenSource cancellation = new();
        int enumerations = 0;
        WindowsDirectoryReadOperation operation = new(
            () =>
            {
                enumerations++;
                return [];
            },
            WindowsDirectoryReadOperation.ClassifyByAttributes,
            () =>
            {
                cancellation.Cancel();
                return DriveKind.Network;
            });

        DirectoryReadOutcome outcome = operation.Read(Request(), cancellation.Token);

        _ = Assert.IsInstanceOfType<DirectoryReadCancelled>(outcome);
        Assert.AreEqual(0, enumerations);
    }

    /// <summary>An empty enumeration that cancels still returns cancellation.</summary>
    [TestMethod]
    public void ReadWhenEmptyEnumerationCancelsReturnsCancelled()
    {
        using CancellationTokenSource cancellation = new();
        WindowsDirectoryReadOperation operation = new(
            () =>
            {
                cancellation.Cancel();
                return [];
            },
            WindowsDirectoryReadOperation.ClassifyByAttributes,
            () => null);

        DirectoryReadOutcome outcome = operation.Read(Request(), cancellation.Token);

        _ = Assert.IsInstanceOfType<DirectoryReadCancelled>(outcome);
    }

    /// <summary>Proves a provider that throws while creating its enumerable is normalized.</summary>
    [TestMethod]
    public void ReadWhenProviderCallbackThrowsNormalizesFailure()
    {
        DirectoryReadOutcome denied = new WindowsDirectoryReadOperation(
            () => throw new UnauthorizedAccessException(),
            WindowsDirectoryReadOperation.ClassifyByAttributes,
            () => null).Read(Request(), CancellationToken.None);
        DirectoryReadOutcome missing = new WindowsDirectoryReadOperation(
            () => throw new DirectoryNotFoundException(),
            WindowsDirectoryReadOperation.ClassifyByAttributes,
            () => null).Read(Request(), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.AccessDenied, Assert.IsInstanceOfType<DirectoryReadFailed>(denied).Failure);
        Assert.AreSame(FileOperationFailureKind.NotFound, Assert.IsInstanceOfType<DirectoryReadFailed>(missing).Failure);
    }

    /// <summary>Proves a lazy iterator failure after an entry discards the partial listing.</summary>
    [TestMethod]
    public void ReadWhenIteratorThrowsAfterEntryNormalizesFailureWithoutPartialListing()
    {
        DirectoryReadOutcome denied = new WindowsDirectoryReadOperation(
            () => EntriesThenFailure(new UnauthorizedAccessException()),
            WindowsDirectoryReadOperation.ClassifyByAttributes,
            () => null).Read(Request(), CancellationToken.None);
        DirectoryReadOutcome unavailable = new WindowsDirectoryReadOperation(
            () => EntriesThenFailure(new IOException("Synthetic provider loss.")),
            WindowsDirectoryReadOperation.ClassifyByAttributes,
            () => null).Read(Request(), CancellationToken.None);

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
