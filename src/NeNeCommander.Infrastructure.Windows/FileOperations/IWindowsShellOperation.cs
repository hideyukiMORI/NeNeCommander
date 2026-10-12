namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Defines the four native steps needed for a single guarded recycle operation.</summary>
internal interface IWindowsShellOperation
{
    public int SetOperationFlags(uint flags);
    public int DeleteItem(IWindowsShellEntry entry, IWindowsRecycleSink sink);
    public int PerformOperations();
    public int GetAnyOperationsAborted(out int aborted);
}
