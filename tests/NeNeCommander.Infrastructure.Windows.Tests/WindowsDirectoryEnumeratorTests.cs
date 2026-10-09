using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Infrastructure.Windows.Directories;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>
/// Proves the per-entry metadata reads keep an entry listed when one fact fails, and map only the
/// expected per-entry failures to an unknown value.
/// </summary>
[TestClass]
public sealed class WindowsDirectoryEnumeratorTests
{
    /// <summary>Proves a readable length becomes a known size with the same byte count.</summary>
    [TestMethod]
    public void ReadSizeWhenLengthIsReadableReturnsKnownSize()
    {
        EntrySize size = WindowsDirectoryEnumerator.ReadSize(() => 12345L);

        Assert.AreEqual(12345L, Assert.IsInstanceOfType<KnownEntrySize>(size).Bytes);
    }

    /// <summary>Proves the two expected per-entry failures become an unknown size.</summary>
    [TestMethod]
    public void ReadSizeWhenEntryFailsExpectedlyReturnsUnknown()
    {
        EntrySize vanished = WindowsDirectoryEnumerator.ReadSize(() => throw new FileNotFoundException());
        EntrySize denied = WindowsDirectoryEnumerator.ReadSize(() => throw new UnauthorizedAccessException());

        Assert.AreSame(EntrySize.Unknown, vanished);
        Assert.AreSame(EntrySize.Unknown, denied);
    }

    /// <summary>Proves an unexpected failure is not swallowed as an unknown size.</summary>
    [TestMethod]
    public void ReadSizeWhenFailureIsUnexpectedPropagatesIt()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(
            () => WindowsDirectoryEnumerator.ReadSize(() => throw new InvalidOperationException()));
    }

    /// <summary>Proves a readable last-write time becomes a known UTC timestamp with the same instant.</summary>
    [TestMethod]
    public void ReadModifiedWhenTimeIsReadableReturnsKnownUtcTimestamp()
    {
        DateTime lastWrite = new(2025, 12, 31, 23, 59, 58, DateTimeKind.Utc);

        EntryTimestamp modified = WindowsDirectoryEnumerator.ReadModified(() => lastWrite);

        DateTimeOffset utc = Assert.IsInstanceOfType<KnownEntryTimestamp>(modified).Utc;
        Assert.AreEqual(lastWrite, utc.UtcDateTime);
        Assert.AreEqual(TimeSpan.Zero, utc.Offset);
    }

    /// <summary>Proves the platform's FILETIME origin, its marker for an absent time, is unknown.</summary>
    [TestMethod]
    public void ReadModifiedWhenTimeIsFileTimeOriginReturnsUnknown()
    {
        EntryTimestamp modified = WindowsDirectoryEnumerator.ReadModified(() => DateTime.FromFileTimeUtc(0));

        Assert.AreSame(EntryTimestamp.Unknown, modified);
    }

    /// <summary>Proves the instant after the FILETIME origin is still a known time.</summary>
    [TestMethod]
    public void ReadModifiedWhenTimeFollowsFileTimeOriginReturnsKnown()
    {
        EntryTimestamp modified = WindowsDirectoryEnumerator.ReadModified(() => DateTime.FromFileTimeUtc(1));

        _ = Assert.IsInstanceOfType<KnownEntryTimestamp>(modified);
    }

    /// <summary>Proves the two expected per-entry failures become an unknown time.</summary>
    [TestMethod]
    public void ReadModifiedWhenEntryFailsExpectedlyReturnsUnknown()
    {
        EntryTimestamp vanished = WindowsDirectoryEnumerator.ReadModified(() => throw new IOException());
        EntryTimestamp denied = WindowsDirectoryEnumerator.ReadModified(() => throw new UnauthorizedAccessException());

        Assert.AreSame(EntryTimestamp.Unknown, vanished);
        Assert.AreSame(EntryTimestamp.Unknown, denied);
    }

    /// <summary>Proves an unexpected failure is not swallowed as an unknown time.</summary>
    [TestMethod]
    public void ReadModifiedWhenFailureIsUnexpectedPropagatesIt()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(
            () => WindowsDirectoryEnumerator.ReadModified(() => throw new InvalidOperationException()));
    }

    /// <summary>Proves each read rejects an absent reader as an adapter defect.</summary>
    [TestMethod]
    public void ReadWhenReaderIsNullThrowsArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => WindowsDirectoryEnumerator.ReadSize(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => WindowsDirectoryEnumerator.ReadModified(null!));
    }
}
