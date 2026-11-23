using System;
using System.Collections.Generic;
using System.Linq;
using NeNeCommander.Application.Directories;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Counts the two provider-classified kinds in one already-owned entry snapshot.</summary>
internal sealed record PaneEntryCounts
{
    private PaneEntryCounts(int files, int directories)
    {
        Files = files;
        Directories = directories;
    }

    /// <summary>Gets the number of entries classified as files, without reading their contents.</summary>
    public int Files { get; }

    /// <summary>Gets the number of entries classified as directories, without recursive enumeration.</summary>
    public int Directories { get; }

    /// <summary>Gets the number of classified entries in this snapshot.</summary>
    public int Total => Files + Directories;

    internal static PaneEntryCounts Create(IReadOnlyList<DirectoryEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        int directories = entries.Count(entry => entry.Kind == DirectoryEntryKind.Directory);
        return new PaneEntryCounts(entries.Count - directories, directories);
    }
}
