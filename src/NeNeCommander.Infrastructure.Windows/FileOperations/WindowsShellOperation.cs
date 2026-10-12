using System;
using System.Runtime.InteropServices.Marshalling;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Owns the native operation and keeps its callback object alive until native release.</summary>
internal sealed class WindowsShellOperation : IWindowsShellOperation, IDisposable
{

    private NativeWindowsRecycleProgress? _progress;

    internal WindowsShellOperation(INativeWindowsShellOperation native)
    {
        ArgumentNullException.ThrowIfNull(native);
        Native = native;
    }

    internal INativeWindowsShellOperation Native { get; }

    public int SetOperationFlags(uint flags)
    {
        return Native.SetOperationFlags(flags);
    }

    public int DeleteItem(IWindowsShellEntry entry, IWindowsRecycleSink sink)
    {
        _progress = new NativeWindowsRecycleProgress(sink);
        return Native.DeleteItem(((WindowsShellEntry)entry).Native, _progress);
    }

    public int PerformOperations()
    {
        return Native.PerformOperations();
    }

    public int GetAnyOperationsAborted(out int aborted)
    {
        return Native.GetAnyOperationsAborted(out aborted);
    }

    public void Dispose()
    {
        ((ComObject)(object)Native).FinalRelease();
        _progress?.Dispose();
    }
}
