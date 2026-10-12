namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Exposes only the Shell facts required by recycle policy.</summary>
internal interface IWindowsShellEntry
{
    public int GetDisplayName(uint form, out string name);
    public int GetAttributes(uint mask, out uint attributes);
}
