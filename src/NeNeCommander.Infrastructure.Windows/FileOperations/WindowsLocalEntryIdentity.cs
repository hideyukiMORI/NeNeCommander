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
    private const string ExtendedLengthPrefix = @"\\?\";

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
        return Compose(entry, WindowsFileIdentifier.Describe(ExtendedLength(entry.FullName)));
    }

    /// <summary>Captures full and stable identities using the same native object observation.</summary>
    internal static VerifiedEntrySnapshot Capture(WindowsLocalPath path, FileSystemInfo entry)
    {
        entry.Refresh();
        string objectIdentity = WindowsFileIdentifier.Describe(ExtendedLength(path.CanonicalText));
        return VerifiedEntrySnapshot.Create(path, Compose(entry, objectIdentity), FileObjectIdentity.Create(objectIdentity));
    }

    /// <summary>
    /// Returns the extended-length form of a full local path so the native open does not depend on
    /// the host's long-path setting. A path that already starts with two separators, which is the
    /// extended-length form or a network path, is returned unchanged.
    /// </summary>
    private static string ExtendedLength(string fullPath)
    {
        return fullPath.StartsWith(@"\\", StringComparison.Ordinal)
            ? fullPath
            : ExtendedLengthPrefix + fullPath;
    }

    private static FileIdentity Compose(FileSystemInfo entry, string objectIdentity)
    {
        IdentityMetadata metadata = new(entry is DirectoryInfo ? "directory" : "file",
            entry is FileInfo file ? file.Length : 0, entry.CreationTimeUtc.Ticks, entry.LastWriteTimeUtc.Ticks);
        return Compose(objectIdentity, metadata);
    }

    /// <summary>Associates a caller-qualified path with the exact held object's existing windows-v2 identity; it does not prove the current pathname.</summary>
    internal static VerifiedEntrySnapshot CaptureHeld(WindowsLocalPath path, WindowsLocalHandleFacts facts)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(facts);
        bool directoryAttribute = (facts.Basic.Attributes & (uint)FileAttributes.Directory) != 0;
        if (facts.Standard.EndOfFile < 0 || facts.Standard.Directory > 1 || facts.Standard.DeletePending > 1 ||
            directoryAttribute != (facts.Standard.Directory != 0))
        {
            throw new IOException("The held entry metadata is inconsistent.");
        }
        try
        {
            IdentityMetadata metadata = new(facts.Standard.Directory != 0 ? "directory" : "file",
                facts.Standard.Directory != 0 ? 0 : facts.Standard.EndOfFile,
                DateTime.FromFileTimeUtc(facts.Basic.CreationFileTimeUtc).Ticks,
                DateTime.FromFileTimeUtc(facts.Basic.LastWriteFileTimeUtc).Ticks);
            return VerifiedEntrySnapshot.Create(path, Compose(facts.ObjectIdentity.Value, metadata), facts.ObjectIdentity);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new IOException("The held entry timestamps cannot be represented.", exception);
        }
    }

    private static FileIdentity Compose(string objectIdentity, IdentityMetadata metadata)
    {
        string value = string.Join(
            '|',
            "windows-v2",
            objectIdentity,
            metadata.Kind,
            metadata.Length.ToString(CultureInfo.InvariantCulture),
            metadata.CreationTicks.ToString(CultureInfo.InvariantCulture),
            metadata.LastWriteTicks.ToString(CultureInfo.InvariantCulture));
        return FileIdentity.Parse(value) is FileIdentityAccepted accepted
            ? accepted.Identity
            : throw new InvalidOperationException("The metadata identity exceeds the identity boundary.");
    }

    private sealed record IdentityMetadata
    {
        internal IdentityMetadata(string kind, long length, long creationTicks, long lastWriteTicks)
        {
            Kind = kind;
            Length = length;
            CreationTicks = creationTicks;
            LastWriteTicks = lastWriteTicks;
        }

        internal string Kind { get; }
        internal long Length { get; }
        internal long CreationTicks { get; }
        internal long LastWriteTicks { get; }
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
