using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Directories;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves synchronous provider work crosses the single owned scheduling boundary.</summary>
[TestClass]
public sealed class WindowsLocalIoExecutionBoundaryTests
{
    /// <summary>Proves reads and mutations both return before their queued filesystem work runs.</summary>
    [TestMethod]
    public async Task ProviderCallsWhenExecutionIsQueuedDoNotCompleteOnCallingThread()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileSystemPath file = ParsePath(root.WriteFile("item.txt", "content"));
        ManualIoScheduler scheduler = new();
        WindowsLocalIoExecutionBoundary boundary = new(scheduler);
        WindowsLocalDirectoryReader reader = new(boundary);
        WindowsLocalFileOperationAdapter adapter = new(boundary);

        Task<DirectoryReadOutcome> read = reader.ReadAsync(
            Request(root.Path),
            CancellationToken.None);
        Task<FileInspectionOutcome> inspection = adapter.InspectAsync(file, CancellationToken.None);

        Assert.IsFalse(read.IsCompleted);
        Assert.IsFalse(inspection.IsCompleted);
        Assert.AreEqual(2, scheduler.PendingCount);
        scheduler.ExecuteAll();
        _ = Assert.IsInstanceOfType<DirectoryReadSucceeded>(await read);
        _ = Assert.IsInstanceOfType<FileInspectionSucceeded>(await inspection);
    }

    private static DirectoryReadRequest Request(FileSystemPath location)
    {
        DirectoryReadRequestCreation creation = DirectoryReadRequest.Create(location, 8);
        return Assert.IsInstanceOfType<DirectoryReadRequestAccepted>(creation).Request;
    }

    private static FileSystemPath ParsePath(string text)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path;
    }

    private sealed class ManualIoScheduler : IWindowsLocalIoScheduler
    {
        private readonly Queue<Action> _pending = new();

        internal int PendingCount => _pending.Count;

        internal void ExecuteAll()
        {
            while (_pending.Count > 0)
            {
                _pending.Dequeue()();
            }
        }

        public Task<TResult> ScheduleAsync<TResult>(Func<TResult> operation)
        {
            TaskCompletionSource<TResult> completion = new();
            _pending.Enqueue(() => completion.SetResult(operation()));
            return completion.Task;
        }
    }
}
