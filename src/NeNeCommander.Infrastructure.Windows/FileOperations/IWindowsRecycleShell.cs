using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Isolates native Shell creation and the volume query for deterministic boundary proof.</summary>
internal interface IWindowsRecycleShell
{
    public IWindowsShellEntry OpenEntry(WindowsLocalPath path);
    public IWindowsShellOperation OpenOperation();
    public int QueryRecycleBin(WindowsLocalPath path);
}
