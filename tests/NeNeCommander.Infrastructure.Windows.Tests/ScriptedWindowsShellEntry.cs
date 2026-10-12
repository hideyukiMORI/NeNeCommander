using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

internal sealed class ScriptedWindowsShellEntry : IWindowsShellEntry
{
    internal ScriptedWindowsShellEntry(string path)
    {
        Path = path;
    }

    internal string Path { get; }
    internal uint Attributes { get; init; } = 0x40000020;
    internal int AttributesResult { get; init; }
    internal int NameResult { get; init; }

    public int GetDisplayName(uint form, out string name)
    {
        name = Path;
        return NameResult;
    }

    public int GetAttributes(uint mask, out uint attributes)
    {
        attributes = Attributes;
        return AttributesResult;
    }
}
