using System.Collections.Generic;

namespace NeNeCommander.Infrastructure.Windows.Settings;

/// <summary>An admitted ancestor chain, nearest parent first, ending at a local root.</summary>
internal sealed record SafeLocation : SettingsLocationSnapshot
{
    internal SafeLocation(IReadOnlyList<DirectoryBaseline> directories)
    {
        Directories = directories;
    }

    internal IReadOnlyList<DirectoryBaseline> Directories { get; }
}
