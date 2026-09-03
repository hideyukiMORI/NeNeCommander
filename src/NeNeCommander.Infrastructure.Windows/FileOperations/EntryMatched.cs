using System;
using System.IO;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Represents an entry whose current metadata identity still matches its snapshot.</summary>
public sealed record EntryMatched : RevalidationOutcome
{
    internal EntryMatched(FileSystemInfo entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        Entry = entry;
    }

    /// <summary>Gets the current entry.</summary>
    public FileSystemInfo Entry { get; }
}
