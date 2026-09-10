namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>
/// Freezes the ADR-0049 identity facts of one entry as read from a single no-follow handle: the
/// file system inode, the attributes and reparse tag of the entry itself, the byte length, the hard
/// link count, and the two timestamps that an unprivileged caller cannot restore on a replacement.
/// Creation and last-access time are absent because 9P derives creation time from access time.
/// </summary>
internal sealed record WslHandleFacts
{
    /// <summary>Creates one complete facts snapshot from the values of one opened handle.</summary>
    /// <param name="inode">File system inode reported by <c>FileInternalInformation</c>.</param>
    /// <param name="attributes">Attributes of the entry itself, never of a reparse target.</param>
    /// <param name="reparseTag">Reparse tag of the entry, or zero when it is not a reparse point.</param>
    /// <param name="endOfFile">Byte length reported by <c>FileStandardInfo</c>.</param>
    /// <param name="numberOfLinks">Hard link count reported by <c>FileStandardInfo</c>.</param>
    /// <param name="lastWriteFileTimeUtc">Last-write time as 100-nanosecond intervals since 1601.</param>
    /// <param name="changeFileTimeUtc">Kernel-assigned change time in the same unit.</param>
    internal WslHandleFacts(
        long inode,
        uint attributes,
        uint reparseTag,
        long endOfFile,
        uint numberOfLinks,
        long lastWriteFileTimeUtc,
        long changeFileTimeUtc)
    {
        Inode = inode;
        Attributes = attributes;
        ReparseTag = reparseTag;
        EndOfFile = endOfFile;
        NumberOfLinks = numberOfLinks;
        LastWriteFileTimeUtc = lastWriteFileTimeUtc;
        ChangeFileTimeUtc = changeFileTimeUtc;
    }

    /// <summary>Gets the file system inode of the opened entry.</summary>
    internal long Inode { get; }

    /// <summary>Gets the Win32 attributes of the opened entry itself.</summary>
    internal uint Attributes { get; }

    /// <summary>Gets the reparse tag of the opened entry, or zero when it has none.</summary>
    internal uint ReparseTag { get; }

    /// <summary>Gets the byte length of the opened entry.</summary>
    internal long EndOfFile { get; }

    /// <summary>Gets the hard link count of the opened entry.</summary>
    internal uint NumberOfLinks { get; }

    /// <summary>Gets the last-write time in 100-nanosecond intervals since 1601-01-01 UTC.</summary>
    internal long LastWriteFileTimeUtc { get; }

    /// <summary>Gets the change time in 100-nanosecond intervals since 1601-01-01 UTC.</summary>
    internal long ChangeFileTimeUtc { get; }
}
