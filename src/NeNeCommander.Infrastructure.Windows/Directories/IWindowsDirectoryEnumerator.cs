using System.Collections.Generic;

namespace NeNeCommander.Infrastructure.Windows.Directories;

/// <summary>Enumerates direct Windows namespace entries for the shared read operation.</summary>
internal interface IWindowsDirectoryEnumerator
{
    internal IEnumerable<WindowsDirectoryEntrySnapshot> Enumerate(string canonicalLocation);
}
