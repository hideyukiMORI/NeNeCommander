using System;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

internal sealed class ScriptedWindowsShellOperation : IWindowsShellOperation
{
    private IWindowsRecycleSink? _sink;
    private IWindowsShellEntry? _entry;

    internal int FlagsResult { get; init; }
    internal int QueueResult { get; init; }
    internal int AbortQueryResult { get; init; }
    internal int Aborted { get; init; }
    internal uint Flags { get; private set; }
    internal int PerformCount { get; private set; }
    internal Func<IWindowsRecycleSink, IWindowsShellEntry, int> Perform { get; init; } = CompleteRecycle;

    public int SetOperationFlags(uint flags)
    {
        Flags = flags;
        return FlagsResult;
    }

    public int DeleteItem(IWindowsShellEntry entry, IWindowsRecycleSink sink)
    {
        _entry = entry;
        _sink = sink;
        return QueueResult;
    }

    public int PerformOperations()
    {
        PerformCount++;
        return Perform(_sink ?? throw new InvalidOperationException("No queued sink."),
            _entry ?? throw new InvalidOperationException("No queued entry."));
    }

    public int GetAnyOperationsAborted(out int aborted)
    {
        aborted = Aborted;
        return AbortQueryResult;
    }

    internal static int CompleteRecycle(IWindowsRecycleSink sink, IWindowsShellEntry entry)
    {
        int verdict = sink.PreDeleteItem(WindowsRecycleProgress.RecycleFlag, entry);
        return verdict == 0
            ? sink.PostDeleteItem(WindowsRecycleProgress.RecycleFlag, entry, 0, new ScriptedWindowsShellEntry("owned receipt"))
            : verdict;
    }
}
