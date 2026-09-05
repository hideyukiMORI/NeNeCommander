using System;
using System.Globalization;
using System.IO;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>
/// Captures and revalidates one Windows local entry from its Win32 volume/file identifier plus
/// kind, byte length, creation time, and last write time. A replaced or rewritten entry changes it.
/// </summary>
public static class WindowsLocalEntryIdentity
{
    /// <summary>Finds the current file or directory at a location.</summary>
    /// <param name="path">Validated Windows local path.</param>
    /// <returns>The entry, or absence when nothing exists there.</returns>
    public static FileSystemInfo? Find(WindowsLocalPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        FileInfo file = new(path.CanonicalText);
        if (file.Exists)
        {
            return file;
        }
        DirectoryInfo directory = new(path.CanonicalText);
        return directory.Exists ? directory : null;
    }

    /// <summary>Describes an existing entry as an opaque provider identity.</summary>
    /// <param name="entry">Existing file or directory.</param>
    /// <returns>The identity token derived from file identifier, kind, length, and timestamps.</returns>
    public static FileIdentity Describe(FileSystemInfo entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        string kind = entry is DirectoryInfo ? "directory" : "file";
        long length = entry is FileInfo file ? file.Length : 0;
        string value = string.Join(
            '|',
            "windows-v2",
            WindowsFileIdentifier.Describe(entry.FullName),
            kind,
            length.ToString(CultureInfo.InvariantCulture),
            entry.CreationTimeUtc.Ticks.ToString(CultureInfo.InvariantCulture),
            entry.LastWriteTimeUtc.Ticks.ToString(CultureInfo.InvariantCulture));
        return FileIdentity.Parse(value) is FileIdentityAccepted accepted
            ? accepted.Identity
            : throw new InvalidOperationException("The metadata identity exceeds the identity boundary.");
    }

    /// <summary>
    /// Checks a snapshot against the entry as it exists now, so a mutation never acts on an entry
    /// that was replaced or rewritten after preflight.
    /// </summary>
    /// <param name="snapshot">Snapshot captured by inspection.</param>
    /// <returns>The current matching entry or a closed rejection.</returns>
    public static RevalidationOutcome Revalidate(FileEntrySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.Path is not WindowsLocalPath local)
        {
            return new EntryRejected(FileOperationFailureKind.ProviderUnavailable);
        }
        FileSystemInfo? entry = Find(local);
        return entry is null
            ? new EntryRejected(FileOperationFailureKind.NotFound)
            : Describe(entry) == snapshot.Identity
                ? new EntryMatched(entry)
                : new EntryRejected(FileOperationFailureKind.IdentityChanged);
    }
}
