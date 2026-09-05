using System.Collections.Generic;
using System.IO;
using NeNeCommander.Application.Directories;

namespace NeNeCommander.Infrastructure.Windows.Directories;

/// <summary>Reads direct entry facts from a Windows-supported namespace.</summary>
internal sealed class WindowsDirectoryEnumerator : IWindowsDirectoryEnumerator
{
    public IEnumerable<WindowsDirectoryEntrySnapshot> Enumerate(string canonicalLocation)
    {
        DirectoryInfo directory = new(canonicalLocation);
        foreach (FileSystemInfo info in directory.EnumerateFileSystemInfos("*", CreateDirectEntryOptions()))
        {
            yield return new WindowsDirectoryEntrySnapshot(
                info.Name,
                info is DirectoryInfo ? DirectoryEntryKind.Directory : DirectoryEntryKind.File,
                info.Attributes);
        }
    }

    private static EnumerationOptions CreateDirectEntryOptions()
    {
        return new EnumerationOptions
        {
            AttributesToSkip = FileAttributes.None,
            IgnoreInaccessible = false,
            RecurseSubdirectories = false,
            ReturnSpecialDirectories = false,
        };
    }
}
