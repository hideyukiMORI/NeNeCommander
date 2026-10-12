using System;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Preserves the IFileOperationProgressSink callback ABI without exposing Shell to Application.</summary>
internal interface IWindowsRecycleSink
{
    public int StartOperations();
    public int FinishOperations(int result);
    public int PreRenameItem(uint flags, IntPtr entry, string name);
    public int PostRenameItem(uint flags, IntPtr entry, string name, int result, IntPtr created);
    public int PreMoveItem(uint flags, IntPtr entry, IntPtr destination, string name);
    public int PostMoveItem(uint flags, IntPtr entry, IntPtr destination, string name, int result, IntPtr created);
    public int PreCopyItem(uint flags, IntPtr entry, IntPtr destination, string name);
    public int PostCopyItem(uint flags, IntPtr entry, IntPtr destination, string name, int result, IntPtr created);
    public int PreDeleteItem(uint flags, IWindowsShellEntry entry);
    public int PostDeleteItem(uint flags, IWindowsShellEntry entry, int result,
        IWindowsShellEntry? created);
    public int PreNewItem(uint flags, IntPtr destination, string name);
    public int PostNewItem(uint flags, IntPtr destination, string name, string template, uint attributes, int result, IntPtr created);
    public int UpdateProgress(uint total, uint completed);
    public int ResetTimer();
    public int PauseTimer();
    public int ResumeTimer();
}
