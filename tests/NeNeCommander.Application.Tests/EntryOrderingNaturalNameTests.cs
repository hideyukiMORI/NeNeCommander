using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Panes;
using NeNeCommander.Domain.Paths;
using CultureInfo = System.Globalization.CultureInfo;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves the numeric name comparison and its deterministic text boundaries (ADR-0074).</summary>
[TestClass]
public sealed class EntryOrderingNaturalNameTests
{
    /// <summary>Proves numeric runs, padding, text and case in both directions and input orders.</summary>
    /// <param name="first">Name that must precede the other ascending.</param>
    /// <param name="second">Name that must follow the other ascending.</param>
    [TestMethod]
    [DataRow("file2", "file10")]
    [DataRow("file9", "file10")]
    [DataRow("file123", "file124")]
    [DataRow("2-file", "10-file")]
    [DataRow("file000", "file001")]
    [DataRow("file0", "file00")]
    [DataRow("file00", "file000")]
    [DataRow("file2", "file02")]
    [DataRow("file02", "file002")]
    [DataRow("file2z", "file02a")]
    [DataRow("file02z", "file10a")]
    [DataRow("file2a", "File2b")]
    [DataRow("File2", "file2")]
    [DataRow("part2-ch9", "part2-ch10")]
    [DataRow("v1.2", "v1.10")]
    [DataRow("v1.2", "v1.02")]
    [DataRow("file-2", "file-10")]
    [DataRow("file 2", "file 10")]
    [DataRow("file", "file0")]
    [DataRow("a!", "a1")]
    [DataRow("a1", "aA")]
    [DataRow("a1", "a[")]
    [DataRow("file１０", "file２")]
    [DataRow("file١٠", "file٢")]
    [DataRow("file2", "file２")]
    [DataRow("Å2", "å10")]
    [DataRow("\U000104002", "\U0001042810")]
    [DataRow("\U000104002", "\U000104282")]
    [DataRow("a\u030a2", "å2")]
    public void ApplyWhenNamesContainRunsUsesNaturalOrder(string first, string second)
    {
        DirectoryEntry lower = Entry(first, 1);
        DirectoryEntry higher = Entry(second, 2);

        AssertPair(lower, higher);
    }

    /// <summary>Proves arbitrarily long values and zero padding do not depend on bounded parsing.</summary>
    [TestMethod]
    public void ApplyWhenDigitRunsExceedIntegerRangeComparesAllDigits()
    {
        AssertPair(Entry("n" + new string('9', 400), 1), Entry("n1" + new string('0', 400), 2));
        AssertPair(Entry("n" + new string('9', 400) + "1", 1), Entry("n" + new string('9', 400) + "2", 2));
        AssertPair(Entry("n2z", 1), Entry("n" + new string('0', 400) + "2a", 2));
        AssertPair(Entry("n" + new string('0', 400), 1), Entry("n" + new string('0', 401), 2));
    }

    /// <summary>Proves the same display comparison for each supported provider without changing paths.</summary>
    /// <param name="root">Provider path prefix.</param>
    [TestMethod]
    [DataRow("C:/root/")]
    [DataRow("//server/share/root/")]
    [DataRow("//wsl.localhost/Ubuntu/root/")]
    public void ApplyWhenProviderChangesKeepsNaturalDisplayOrder(string root)
    {
        DirectoryEntry lower = DirectoryEntry.Create(Path(root + "File2"), "File2", DirectoryEntryKind.File, Metadata());
        DirectoryEntry higher = DirectoryEntry.Create(Path(root + "file10"), "file10", DirectoryEntryKind.File, Metadata());

        AssertPair(lower, higher);
        Assert.AreEqual(Path(root + "File2"), lower.Path);
        Assert.AreEqual(Path(root + "file10"), higher.Path);
    }

    /// <summary>Proves every pair agrees with one explicit total order, making cycles impossible in this corpus.</summary>
    [TestMethod]
    public void ApplyWhenNamesAreMixedHasConsistentTotalOrderAcrossPermutations()
    {
        string[] names = ["a", "a!", "a0", "a00", "a1", "A2", "a2", "a2a", "a2z", "a02", "a02a", "a9", "a10", "a10b2", "a10b10", "aA", "a[", "a١٠", "a٢", "a１０", "a２"];
        DirectoryEntry[] expected = [.. names.Select(Entry)];
        for (int first = 0; first < expected.Length; first++)
        {
            for (int second = first + 1; second < expected.Length; second++)
            {
                AssertPair(expected[first], expected[second]);
            }
            DirectoryEntry[] rotated = [.. expected.Skip(first), .. expected.Take(first)];
            CollectionAssert.AreEqual(expected, EntryOrdering.Apply(rotated, PaneSortOrder.Default).ToArray());
            CollectionAssert.AreEqual(expected.Reverse().ToArray(), EntryOrdering.Apply(rotated, Descending()).ToArray());
        }
    }

    /// <summary>Proves the maximum listing projects every entry while leaving its lexical snapshot intact.</summary>
    [TestMethod]
    public void ApplyWhenListingReachesBoundaryKeepsEveryEntryInNumericOrder()
    {
        DirectoryEntry[] expected = [.. Enumerable.Range(1, DirectoryListing.EntryBoundaryLimit)
            .Select(index => Entry("file" + index.ToString(CultureInfo.InvariantCulture), index))];
        DirectoryListing listing = Assert.IsInstanceOfType<DirectoryListingAccepted>(DirectoryListing.Create(
            Path("C:/root"), expected.Reverse().ToArray(), DirectoryListingCompleteness.Complete, 0)).Listing;
        DirectoryEntry[] canonical = [.. listing.Entries];

        IReadOnlyList<DirectoryEntry> ordered = EntryOrdering.Apply(listing.Entries, PaneSortOrder.Default);

        CollectionAssert.AreEqual(expected, ordered.ToArray());
        CollectionAssert.AreEqual(canonical, listing.Entries.ToArray());
        Assert.AreEqual("file10", canonical[1].Name);
        Assert.AreEqual("file2", ordered[1].Name);
    }

    /// <summary>Proves equal names preserve both distinct entries and their stable source order.</summary>
    [TestMethod]
    public void ApplyWhenCompleteNamesTieKeepsBothEntriesInSourceOrder()
    {
        DirectoryEntry[] entries = [Entry("file2", 1), Entry("file2", 2)];

        CollectionAssert.AreEqual(entries, EntryOrdering.Apply(entries, PaneSortOrder.Default).ToArray());
        CollectionAssert.AreEqual(entries, EntryOrdering.Apply(entries, Descending()).ToArray());
    }

    private static void AssertPair(DirectoryEntry lower, DirectoryEntry higher)
    {
        DirectoryEntry[] expected = [lower, higher];
        DirectoryEntry[] reversed = [higher, lower];
        CollectionAssert.AreEqual(expected, EntryOrdering.Apply(reversed, PaneSortOrder.Default).ToArray());
        CollectionAssert.AreEqual(expected, EntryOrdering.Apply(expected, PaneSortOrder.Default).ToArray());
        CollectionAssert.AreEqual(reversed, EntryOrdering.Apply(expected, Descending()).ToArray());
        CollectionAssert.AreEqual(reversed, EntryOrdering.Apply(reversed, Descending()).ToArray());
    }

    private static PaneSortOrder Descending()
    {
        return PaneSortOrder.Default.Toggle(SortKey.Name);
    }

    private static DirectoryEntry Entry(string name, int index)
    {
        return DirectoryEntry.Create(Path("C:/root/" + index.ToString(CultureInfo.InvariantCulture)), name, DirectoryEntryKind.File, Metadata());
    }

    private static EntryMetadata Metadata()
    {
        return EntryMetadata.Unmeasured(EntryVisibility.Normal);
    }

    private static FileSystemPath Path(string text)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path;
    }
}
