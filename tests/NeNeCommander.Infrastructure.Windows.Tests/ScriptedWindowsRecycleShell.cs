using System;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

internal sealed class ScriptedWindowsRecycleShell : IWindowsRecycleShell
{
    internal ScriptedWindowsShellOperation Operation { get; init; } = new();
    internal int BinQueryResult { get; init; }
    internal int AttributesResult { get; init; }
    internal uint Attributes { get; init; } = 0x40000020;
    internal Exception? OpenFailure { get; init; }
    internal Exception? OperationFailure { get; init; }

    public IWindowsShellEntry OpenEntry(WindowsLocalPath path)
    {
        return OpenFailure is not null ? throw OpenFailure : new ScriptedWindowsShellEntry(path.CanonicalText)
        {
            Attributes = Attributes,
            AttributesResult = AttributesResult,
        };
    }

    public IWindowsShellOperation OpenOperation()
    {
        return OperationFailure is null ? Operation : throw OperationFailure;
    }

    public int QueryRecycleBin(WindowsLocalPath path)
    {
        return BinQueryResult;
    }
}
