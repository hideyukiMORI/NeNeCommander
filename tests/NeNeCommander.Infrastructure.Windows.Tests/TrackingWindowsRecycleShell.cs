using System;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

internal sealed class TrackingWindowsRecycleShell : IWindowsRecycleShell, IDisposable
{
    private readonly TrackedEntry _entry = new();
    private readonly TrackedOperation _operation = new();

    internal Exception? OperationFailure { get; init; }
    internal int EntryReleaseCount => _entry.ReleaseCount;
    internal int OperationReleaseCount => _operation.ReleaseCount;

    public IWindowsShellEntry OpenEntry(WindowsLocalPath path)
    {
        return _entry;
    }

    public IWindowsShellOperation OpenOperation()
    {
        return OperationFailure is null ? _operation : throw OperationFailure;
    }

    public int QueryRecycleBin(WindowsLocalPath path)
    {
        return 0;
    }

    public void Dispose()
    {
        _entry.Dispose();
        _operation.Dispose();
    }

    private sealed class TrackedEntry : IWindowsShellEntry, IDisposable
    {
        internal int ReleaseCount { get; private set; }

        public int GetDisplayName(uint form, out string name)
        {
            name = "unused tracked entry";
            return 0;
        }

        public int GetAttributes(uint mask, out uint attributes)
        {
            attributes = 0x40000020;
            return 0;
        }

        public void Dispose()
        {
            ReleaseCount++;
        }
    }

    private sealed class TrackedOperation : IWindowsShellOperation, IDisposable
    {
        internal int ReleaseCount { get; private set; }

        public int SetOperationFlags(uint flags)
        {
            return 0;
        }

        public int DeleteItem(IWindowsShellEntry entry, IWindowsRecycleSink sink)
        {
            return 0;
        }

        public int PerformOperations()
        {
            return 0;
        }

        public int GetAnyOperationsAborted(out int aborted)
        {
            aborted = 0;
            return 0;
        }

        public void Dispose()
        {
            ReleaseCount++;
        }
    }
}
