using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves the closed provider metadata values and their single construction paths.</summary>
[TestClass]
public sealed class EntryMetadataTests
{
    /// <summary>Proves a negative byte count is rejected by its parameter name as an adapter defect.</summary>
    [TestMethod]
    public void CreateWhenSizeIsNegativeThrowsArgumentOutOfRange()
    {
        ArgumentOutOfRangeException failure = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => EntrySize.Create(-1));

        Assert.AreEqual("bytes", failure.ParamName);
    }

    /// <summary>Proves zero and the largest count are known sizes that keep their exact value.</summary>
    [TestMethod]
    public void CreateWhenSizeIsNonNegativeReturnsKnownSize()
    {
        KnownEntrySize empty = Assert.IsInstanceOfType<KnownEntrySize>(EntrySize.Create(0));
        KnownEntrySize largest = Assert.IsInstanceOfType<KnownEntrySize>(EntrySize.Create(long.MaxValue));

        Assert.AreEqual(0L, empty.Bytes);
        Assert.AreEqual(long.MaxValue, largest.Bytes);
        Assert.AreNotEqual(EntrySize.Unknown, empty);
    }

    /// <summary>Proves the unknown size is not a known size.</summary>
    [TestMethod]
    public void UnknownSizeWhenReadIsNotKnown()
    {
        Assert.IsNotInstanceOfType<KnownEntrySize>(EntrySize.Unknown);
    }

    /// <summary>Proves a time with a non-zero offset is rejected so every known time is UTC.</summary>
    [TestMethod]
    public void CreateWhenTimestampOffsetIsNotZeroThrowsArgumentException()
    {
        DateTimeOffset local = new(2026, 10, 9, 21, 0, 0, TimeSpan.FromHours(9));

        ArgumentException failure = Assert.ThrowsExactly<ArgumentException>(() => EntryTimestamp.Create(local));

        Assert.AreEqual("utc", failure.ParamName);
        Assert.StartsWith("The modification time must be carried in UTC.", failure.Message);
    }

    /// <summary>Proves a UTC time is a known timestamp that keeps its exact instant.</summary>
    [TestMethod]
    public void CreateWhenTimestampIsUtcReturnsKnownTimestamp()
    {
        DateTimeOffset utc = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);

        KnownEntryTimestamp known = Assert.IsInstanceOfType<KnownEntryTimestamp>(EntryTimestamp.Create(utc));

        Assert.AreEqual(utc, known.Utc);
        Assert.AreEqual(TimeSpan.Zero, known.Utc.Offset);
        Assert.IsNotInstanceOfType<KnownEntryTimestamp>(EntryTimestamp.Unknown);
    }

    /// <summary>Proves metadata keeps each fact it was created with, independently of the other.</summary>
    [TestMethod]
    public void CreateWhenFactsAreGivenKeepsBoth()
    {
        EntrySize size = EntrySize.Create(42);
        EntryTimestamp modified = EntryTimestamp.Create(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero));

        EntryMetadata sizeOnly = EntryMetadata.Create(size, EntryTimestamp.Unknown);
        EntryMetadata timeOnly = EntryMetadata.Create(EntrySize.Unknown, modified);

        Assert.AreSame(size, sizeOnly.Size);
        Assert.AreSame(EntryTimestamp.Unknown, sizeOnly.Modified);
        Assert.AreSame(EntrySize.Unknown, timeOnly.Size);
        Assert.AreSame(modified, timeOnly.Modified);
    }

    /// <summary>Proves the unknown metadata reports neither fact.</summary>
    [TestMethod]
    public void UnknownMetadataWhenReadHasUnknownSizeAndTime()
    {
        Assert.AreSame(EntrySize.Unknown, EntryMetadata.Unknown.Size);
        Assert.AreSame(EntryTimestamp.Unknown, EntryMetadata.Unknown.Modified);
    }

    /// <summary>Proves a directory entry carries exactly the metadata its adapter supplied.</summary>
    [TestMethod]
    public void DirectoryEntryCreateWhenMetadataIsGivenCarriesIt()
    {
        FileSystemPath path = Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse("C:\\root\\a.txt")).Path;
        EntryMetadata metadata = EntryMetadata.Create(EntrySize.Create(7), EntryTimestamp.Unknown);

        DirectoryEntry entry = DirectoryEntry.Create(
            path,
            "a.txt",
            DirectoryEntryKind.File,
            EntryVisibility.Normal,
            metadata);

        Assert.AreSame(metadata, entry.Metadata);
    }
}
