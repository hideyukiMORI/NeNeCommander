namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Defines guarded recycle policy without duplicating native callback signatures.</summary>
internal interface IWindowsRecycleSink
{
    public int RejectUnrequestedMutation();
    public int StartOperations();
    public int FinishOperations(int result);
    public int PreDeleteItem(uint flags, IWindowsShellEntry entry);
    public int PostDeleteItem(uint flags, IWindowsShellEntry entry, int result,
        IWindowsShellEntry? created);
    public int UpdateProgress(uint total, uint completed);
    public int ResetTimer();
    public int PauseTimer();
    public int ResumeTimer();
}
