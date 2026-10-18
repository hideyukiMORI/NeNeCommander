using System;
using System.Runtime.InteropServices.Marshalling;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Translates native callbacks into the guarded policy and transfers unique entry ownership.</summary>
[GeneratedComClass]
internal sealed partial class NativeWindowsRecycleProgress : INativeWindowsRecycleSink, IDisposable
{
    private readonly IWindowsRecycleSink _sink;
    private WindowsShellEntry? _receipt;

    internal NativeWindowsRecycleProgress(IWindowsRecycleSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        _sink = sink;
    }

    public int PreDeleteItem(uint flags, INativeWindowsShellEntry entry)
    {
        using WindowsShellEntry source = new(entry);
        return _sink.PreDeleteItem(flags, source);
    }

    public int PostDeleteItem(uint flags, INativeWindowsShellEntry entry, int result, INativeWindowsShellEntry? created)
    {
        using WindowsShellEntry source = new(entry);
        if (created is null)
        {
            return _sink.PostDeleteItem(flags, source, result, null);
        }
        if (_receipt is not null)
        {
            using WindowsShellEntry additional = new(created);
            return _sink.PostDeleteItem(flags, source, result, additional);
        }
        _receipt = new WindowsShellEntry(created);
        return _sink.PostDeleteItem(flags, source, result, _receipt);
    }

    public int StartOperations()
    {
        return _sink.StartOperations();
    }

    public int FinishOperations(int result)
    {
        return _sink.FinishOperations(result);
    }

    public int PreRenameItem(uint flags, IntPtr entry, string name)
    {
        return _sink.RejectUnrequestedMutation();
    }

    public int PostRenameItem(uint flags, IntPtr entry, string name, int result, IntPtr created)
    {
        return _sink.RejectUnrequestedMutation();
    }

    public int PreMoveItem(uint flags, IntPtr entry, IntPtr destination, string name)
    {
        return _sink.RejectUnrequestedMutation();
    }

    public int PostMoveItem(uint flags, IntPtr entry, IntPtr destination, string name, int result, IntPtr created)
    {
        return _sink.RejectUnrequestedMutation();
    }

    public int PreCopyItem(uint flags, IntPtr entry, IntPtr destination, string name)
    {
        return _sink.RejectUnrequestedMutation();
    }

    public int PostCopyItem(uint flags, IntPtr entry, IntPtr destination, string name, int result, IntPtr created)
    {
        return _sink.RejectUnrequestedMutation();
    }

    public int PreNewItem(uint flags, IntPtr destination, string name)
    {
        return _sink.RejectUnrequestedMutation();
    }

    public int PostNewItem(uint flags, IntPtr destination, string name, string template, uint attributes, int result, IntPtr created)
    {
        return _sink.RejectUnrequestedMutation();
    }

    public int UpdateProgress(uint total, uint completed)
    {
        return _sink.UpdateProgress(total, completed);
    }

    public int ResetTimer()
    {
        return _sink.ResetTimer();
    }

    public int PauseTimer()
    {
        return _sink.PauseTimer();
    }

    public int ResumeTimer()
    {
        return _sink.ResumeTimer();
    }

    public void Dispose()
    {
        _receipt?.Dispose();
    }
}
