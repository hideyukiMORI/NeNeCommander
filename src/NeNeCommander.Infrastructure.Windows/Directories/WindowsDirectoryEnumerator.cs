using System;
using System.Collections.Generic;
using System.IO;
using NeNeCommander.Application.Directories;

namespace NeNeCommander.Infrastructure.Windows.Directories;

/// <summary>Reads direct entry facts from a Windows-supported namespace.</summary>
internal sealed class WindowsDirectoryEnumerator : IWindowsDirectoryEnumerator
{
    /// <summary>
    /// The FILETIME origin <see cref="FileSystemInfo.LastWriteTimeUtc"/> reports when the platform
    /// has no time for the entry; it is the platform's absence marker, not a modification time.
    /// </summary>
    private static readonly DateTime AbsentFileTimeUtc = DateTime.FromFileTimeUtc(0);

    public IEnumerable<WindowsDirectoryEntrySnapshot> Enumerate(string canonicalLocation)
    {
        DirectoryInfo directory = new(canonicalLocation);
        foreach (FileSystemInfo info in directory.EnumerateFileSystemInfos("*", CreateDirectEntryOptions()))
        {
            yield return new WindowsDirectoryEntrySnapshot(
                info.Name,
                info is DirectoryInfo ? DirectoryEntryKind.Directory : DirectoryEntryKind.File,
                info.Attributes,
                EntryMetadata.Create(
                    info is FileInfo file ? ReadSize(() => file.Length) : EntrySize.Unknown,
                    ReadModified(() => info.LastWriteTimeUtc)));
        }
    }

    /// <summary>
    /// Reads one entry's length. The enumeration already holds the value, but a refresh can still
    /// fail for that one entry, for example when it disappears or its access changes during the
    /// read. Only those two expected per-entry failures become an unknown size so the entry stays
    /// listed (ADR-0056); every other exception keeps travelling to the shared read operation.
    /// </summary>
    internal static EntrySize ReadSize(Func<long> readLength)
    {
        ArgumentNullException.ThrowIfNull(readLength);
        try
        {
            return EntrySize.Create(readLength());
        }
        catch (IOException)
        {
            return EntrySize.Unknown;
        }
        catch (UnauthorizedAccessException)
        {
            return EntrySize.Unknown;
        }
    }

    /// <summary>
    /// Reads one entry's last-write time in UTC under the same per-entry failure rule as
    /// <see cref="ReadSize"/>: only <see cref="IOException"/> and
    /// <see cref="UnauthorizedAccessException"/> become an unknown time, and the platform's FILETIME
    /// origin, which it reports for an absent time, is unknown rather than a 1601 timestamp.
    /// </summary>
    internal static EntryTimestamp ReadModified(Func<DateTime> readLastWriteUtc)
    {
        ArgumentNullException.ThrowIfNull(readLastWriteUtc);
        try
        {
            DateTime lastWriteUtc = readLastWriteUtc();
            return lastWriteUtc == AbsentFileTimeUtc
                ? EntryTimestamp.Unknown
                : EntryTimestamp.Create(new DateTimeOffset(lastWriteUtc, TimeSpan.Zero));
        }
        catch (IOException)
        {
            return EntryTimestamp.Unknown;
        }
        catch (UnauthorizedAccessException)
        {
            return EntryTimestamp.Unknown;
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
