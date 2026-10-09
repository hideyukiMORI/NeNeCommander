using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Panes;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves the sole projection from listed entries and a sort order to the shown sequence.</summary>
[TestClass]
public sealed class EntryOrderingTests
{
    /// <summary>Proves the default order is the canonical listing order.</summary>
    [TestMethod]
    public void ApplyWhenOrderIsDefaultMatchesCanonicalListingOrder()
    {
        DirectoryEntry[] entries =
        [
            FileNamed("b.txt"),
            DirectoryNamed("Zeta"),
            FileNamed("A.md"),
            DirectoryNamed("alpha"),
            FileNamed("c"),
        ];
        DirectoryListing listing = Assert.IsInstanceOfType<DirectoryListingAccepted>(DirectoryListing.Create(
            ParsePath("C:\\root"),
            entries,
            DirectoryListingCompleteness.Complete,
            0)).Listing;

        IReadOnlyList<DirectoryEntry> ordered = EntryOrdering.Apply(entries, PaneSortOrder.Default);

        CollectionAssert.AreEqual(listing.Entries.ToArray(), ordered.ToArray());
        AssertNames(["alpha", "Zeta", "A.md", "b.txt", "c"], ordered);
    }

    /// <summary>Proves descending names reverse each group while directories stay first.</summary>
    [TestMethod]
    public void ApplyWhenNameDescendingReversesEachGroupAndKeepsDirectoriesFirst()
    {
        DirectoryEntry[] entries = [FileNamed("a.txt"), DirectoryNamed("b"), FileNamed("C.txt"), DirectoryNamed("a")];

        IReadOnlyList<DirectoryEntry> ordered = EntryOrdering.Apply(entries, NameDescending());

        AssertNames(["b", "a", "C.txt", "a.txt"], ordered);
    }

    /// <summary>Proves names that differ only by case are ordered ordinally after the case-blind comparison.</summary>
    [TestMethod]
    public void ApplyWhenNamesDifferOnlyByCaseOrdersOrdinallyInBothDirections()
    {
        DirectoryEntry lower = Entry("C:\\root\\1", "read", DirectoryEntryKind.File);
        DirectoryEntry upper = Entry("C:\\root\\2", "Read", DirectoryEntryKind.File);
        DirectoryEntry other = Entry("C:\\root\\3", "q", DirectoryEntryKind.File);

        IReadOnlyList<DirectoryEntry> ascending = EntryOrdering.Apply([lower, other, upper], PaneSortOrder.Default);
        IReadOnlyList<DirectoryEntry> descending = EntryOrdering.Apply([lower, other, upper], NameDescending());

        CollectionAssert.AreEqual(new[] { other, upper, lower }, ascending.ToArray());
        CollectionAssert.AreEqual(new[] { lower, upper, other }, descending.ToArray());
    }

    /// <summary>Proves the extension is the text after the last dot and a missing one sorts first.</summary>
    [TestMethod]
    public void ApplyWhenExtensionAscendingPlacesNamesWithoutExtensionFirst()
    {
        DirectoryEntry[] entries =
        [
            FileNamed("z.txt"),
            FileNamed("archive.tar.gz"),
            FileNamed(".gitignore"),
            FileNamed("README"),
            FileNamed("notes.md"),
            Entry("C:\\root\\trailing-dot", "trailing.", DirectoryEntryKind.File),
        ];

        IReadOnlyList<DirectoryEntry> ordered = EntryOrdering.Apply(entries, ExtensionAscending());

        AssertNames([".gitignore", "README", "trailing.", "archive.tar.gz", "notes.md", "z.txt"], ordered);
    }

    /// <summary>Proves extensions compare ignoring case and equal extensions fall back to the name order.</summary>
    [TestMethod]
    public void ApplyWhenExtensionsTieOrdersByAscendingNameInBothDirections()
    {
        DirectoryEntry[] entries = [FileNamed("b.TXT"), FileNamed("a.md"), FileNamed("C.txt"), FileNamed("a.txt")];

        IReadOnlyList<DirectoryEntry> ascending = EntryOrdering.Apply(entries, ExtensionAscending());
        IReadOnlyList<DirectoryEntry> descending = EntryOrdering.Apply(entries, ExtensionDescending());

        AssertNames(["a.md", "a.txt", "b.TXT", "C.txt"], ascending);
        AssertNames(["a.txt", "b.TXT", "C.txt", "a.md"], descending);
    }

    /// <summary>Proves descending extensions reverse only the key and keep directories first.</summary>
    [TestMethod]
    public void ApplyWhenExtensionDescendingKeepsDirectoriesFirstAndPutsMissingExtensionLast()
    {
        DirectoryEntry[] entries =
        [
            FileNamed("plain"),
            DirectoryNamed("src.old"),
            FileNamed("b.cs"),
            DirectoryNamed("bin"),
            FileNamed("a.zip"),
        ];

        IReadOnlyList<DirectoryEntry> ordered = EntryOrdering.Apply(entries, ExtensionDescending());

        AssertNames(["src.old", "bin", "a.zip", "b.cs", "plain"], ordered);
    }

    /// <summary>
    /// Proves ascending size places directories first in name order, known sizes smallest first,
    /// and unknown sizes last.
    /// </summary>
    [TestMethod]
    public void ApplyWhenSizeAscendingPlacesUnknownSizesLastAndKeepsDirectoriesFirst()
    {
        IReadOnlyList<DirectoryEntry> ordered = EntryOrdering.Apply(MetadataEntries(), SizeAscending());

        AssertNames(["alpha", "Zeta", "small", "middle", "large", "unread"], ordered);
    }

    /// <summary>
    /// Proves descending size reverses only the key: directories stay first in name order and unknown
    /// sizes come before every known size.
    /// </summary>
    [TestMethod]
    public void ApplyWhenSizeDescendingPlacesUnknownSizesFirstAndKeepsDirectoriesFirst()
    {
        IReadOnlyList<DirectoryEntry> ordered = EntryOrdering.Apply(MetadataEntries(), SizeDescending());

        AssertNames(["alpha", "Zeta", "unread", "large", "middle", "small"], ordered);
    }

    /// <summary>Proves ascending modification time places older first and unknown times last.</summary>
    [TestMethod]
    public void ApplyWhenModifiedAscendingPlacesUnknownTimesLastAndKeepsDirectoriesFirst()
    {
        IReadOnlyList<DirectoryEntry> ordered = EntryOrdering.Apply(MetadataEntries(), ModifiedAscending());

        AssertNames(["Zeta", "alpha", "large", "small", "middle", "unread"], ordered);
    }

    /// <summary>Proves descending modification time places unknown times first and newer before older.</summary>
    [TestMethod]
    public void ApplyWhenModifiedDescendingPlacesUnknownTimesFirstAndKeepsDirectoriesFirst()
    {
        IReadOnlyList<DirectoryEntry> ordered = EntryOrdering.Apply(MetadataEntries(), ModifiedDescending());

        AssertNames(["alpha", "Zeta", "unread", "middle", "small", "large"], ordered);
    }

    /// <summary>Proves equal sizes, including two unknown sizes, fall back to the ascending name order.</summary>
    [TestMethod]
    public void ApplyWhenSizesTieOrdersByAscendingNameInBothDirections()
    {
        DirectoryEntry[] entries = [Sized("b", 5), Unsized("D"), Sized("A", 5), Unsized("c")];

        IReadOnlyList<DirectoryEntry> ascending = EntryOrdering.Apply(entries, SizeAscending());
        IReadOnlyList<DirectoryEntry> descending = EntryOrdering.Apply(entries, SizeDescending());

        AssertNames(["A", "b", "c", "D"], ascending);
        AssertNames(["c", "D", "A", "b"], descending);
    }

    /// <summary>Proves equal times, including two unknown times, fall back to the ascending name order.</summary>
    [TestMethod]
    public void ApplyWhenTimesTieOrdersByAscendingNameInBothDirections()
    {
        DirectoryEntry[] entries = [Dated("b", 1), Undated("D"), Dated("A", 1), Undated("c")];

        IReadOnlyList<DirectoryEntry> ascending = EntryOrdering.Apply(entries, ModifiedAscending());
        IReadOnlyList<DirectoryEntry> descending = EntryOrdering.Apply(entries, ModifiedDescending());

        AssertNames(["A", "b", "c", "D"], ascending);
        AssertNames(["c", "D", "A", "b"], descending);
    }

    /// <summary>Proves the size key compares byte counts numerically up to the largest count.</summary>
    [TestMethod]
    public void ApplyWhenSizesSpanTheRangeComparesNumerically()
    {
        DirectoryEntry[] entries = [Sized("max", long.MaxValue), Sized("ten", 10), Sized("zero", 0), Sized("nine", 9)];

        IReadOnlyList<DirectoryEntry> ordered = EntryOrdering.Apply(entries, SizeAscending());

        AssertNames(["zero", "nine", "ten", "max"], ordered);
    }

    /// <summary>Proves the projection owns a new sequence and never reorders its input.</summary>
    [TestMethod]
    public void ApplyWhenOrderingLeavesInputUnchangedAndReturnsEveryEntry()
    {
        List<DirectoryEntry> entries = [FileNamed("b"), FileNamed("a")];

        IReadOnlyList<DirectoryEntry> ordered = EntryOrdering.Apply(entries, PaneSortOrder.Default);
        entries.Clear();

        AssertNames(["a", "b"], ordered);
    }

    /// <summary>Proves an empty input projects to an empty sequence.</summary>
    [TestMethod]
    public void ApplyWhenEntriesAreEmptyReturnsEmptySequence()
    {
        IReadOnlyList<DirectoryEntry> ordered = EntryOrdering.Apply([], ExtensionAscending());

        Assert.IsEmpty(ordered);
    }

    /// <summary>Proves each absent argument is rejected by its own name before any projection.</summary>
    [TestMethod]
    public void ApplyWhenArgumentIsNullNamesTheParameter()
    {
        ArgumentNullException entries = Assert.ThrowsExactly<ArgumentNullException>(
            () => EntryOrdering.Apply(null!, PaneSortOrder.Default));
        ArgumentNullException order = Assert.ThrowsExactly<ArgumentNullException>(
            () => EntryOrdering.Apply([], null!));

        Assert.AreEqual("entries", entries.ParamName);
        Assert.AreEqual("order", order.ParamName);
    }

    private static PaneSortOrder NameDescending()
    {
        return PaneSortOrder.Default.Toggle(SortKey.Name);
    }

    private static PaneSortOrder ExtensionAscending()
    {
        return PaneSortOrder.Default.Toggle(SortKey.Extension);
    }

    private static PaneSortOrder ExtensionDescending()
    {
        return ExtensionAscending().Toggle(SortKey.Extension);
    }

    private static PaneSortOrder SizeAscending()
    {
        return PaneSortOrder.Default.Toggle(SortKey.Size);
    }

    private static PaneSortOrder SizeDescending()
    {
        return SizeAscending().Toggle(SortKey.Size);
    }

    private static PaneSortOrder ModifiedAscending()
    {
        return PaneSortOrder.Default.Toggle(SortKey.Modified);
    }

    private static PaneSortOrder ModifiedDescending()
    {
        return ModifiedAscending().Toggle(SortKey.Modified);
    }

    /// <summary>
    /// Builds entries whose size order and time order differ, so a test proves which key decided.
    /// Directories report no size; one file reports neither fact.
    /// </summary>
    private static DirectoryEntry[] MetadataEntries()
    {
        return
        [
            WithMetadata("large", DirectoryEntryKind.File, EntrySize.Create(300), Day(1)),
            WithMetadata("Zeta", DirectoryEntryKind.Directory, EntrySize.Unknown, Day(0)),
            WithMetadata("unread", DirectoryEntryKind.File, EntrySize.Unknown, EntryTimestamp.Unknown),
            WithMetadata("small", DirectoryEntryKind.File, EntrySize.Create(10), Day(2)),
            WithMetadata("alpha", DirectoryEntryKind.Directory, EntrySize.Unknown, EntryTimestamp.Unknown),
            WithMetadata("middle", DirectoryEntryKind.File, EntrySize.Create(20), Day(3)),
        ];
    }

    private static DirectoryEntry Sized(string name, long bytes)
    {
        return WithMetadata(name, DirectoryEntryKind.File, EntrySize.Create(bytes), EntryTimestamp.Unknown);
    }

    private static DirectoryEntry Unsized(string name)
    {
        return WithMetadata(name, DirectoryEntryKind.File, EntrySize.Unknown, EntryTimestamp.Unknown);
    }

    private static DirectoryEntry Dated(string name, int day)
    {
        return WithMetadata(name, DirectoryEntryKind.File, EntrySize.Unknown, Day(day));
    }

    private static DirectoryEntry Undated(string name)
    {
        return WithMetadata(name, DirectoryEntryKind.File, EntrySize.Create(1), EntryTimestamp.Unknown);
    }

    private static EntryTimestamp Day(int day)
    {
        return EntryTimestamp.Create(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(day));
    }

    private static DirectoryEntry WithMetadata(
        string name,
        DirectoryEntryKind kind,
        EntrySize size,
        EntryTimestamp modified)
    {
        return DirectoryEntry.Create(
            ParsePath("C:\\root\\" + name),
            name,
            kind,
            EntryMetadata.Create(EntryVisibility.Normal, size, modified));
    }

    private static void AssertNames(string[] expected, IReadOnlyList<DirectoryEntry> actual)
    {
        CollectionAssert.AreEqual(expected, actual.Select(entry => entry.Name).ToArray());
    }

    private static DirectoryEntry FileNamed(string name)
    {
        return Entry("C:\\root\\" + name, name, DirectoryEntryKind.File);
    }

    private static DirectoryEntry DirectoryNamed(string name)
    {
        return Entry("C:\\root\\" + name, name, DirectoryEntryKind.Directory);
    }

    private static DirectoryEntry Entry(string path, string name, DirectoryEntryKind kind)
    {
        return DirectoryEntry.Create(ParsePath(path), name, kind, EntryMetadata.Unmeasured(EntryVisibility.Normal));
    }

    private static FileSystemPath ParsePath(string input)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input)).Path;
    }
}
