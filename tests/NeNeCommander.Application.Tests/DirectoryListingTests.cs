using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves deterministic ordering, boundaries, and ownership of directory listings.</summary>
[TestClass]
public sealed class DirectoryListingTests
{
    /// <summary>Proves ordering ignores enumeration order and places directories first.</summary>
    [TestMethod]
    public void CreateWhenEntriesArriveUnorderedOrdersDirectoriesFirstThenNameIgnoringCaseThenOrdinal()
    {
        FileSystemPath location = ParsePath("\\\\wsl.localhost\\Ubuntu\\home\\xi");
        DirectoryEntry[] unordered = [
            Entry(location, "b", DirectoryEntryKind.File),
            Entry(location, "Zeta", DirectoryEntryKind.Directory),
            Entry(location, "a", DirectoryEntryKind.File),
            Entry(location, "A", DirectoryEntryKind.File),
            Entry(location, "alpha", DirectoryEntryKind.Directory),
        ];

        DirectoryListing listing = CreateListing(location, unordered, DirectoryListingCompleteness.Complete, 0);

        string[] names = [.. listing.Entries.Select(entry => entry.Name)];
        string[] expected = ["alpha", "Zeta", "A", "a", "b"];
        CollectionAssert.AreEqual(expected, names);
        Assert.AreSame(location, listing.Location);
        Assert.AreSame(DirectoryListingCompleteness.Complete, listing.Completeness);
        Assert.AreEqual(0, listing.UnrepresentableEntryCount);
    }

    /// <summary>Proves the first ordered entry is the deterministic pane focus identity.</summary>
    [TestMethod]
    public void CreateWhenProjectedToPaneStateFocusesFirstOrderedEntry()
    {
        FileSystemPath location = ParsePath("C:\\projects");
        DirectoryEntry[] unordered = [
            Entry(location, "notes.txt", DirectoryEntryKind.File),
            Entry(location, "src", DirectoryEntryKind.Directory),
        ];
        DirectoryListing listing = CreateListing(location, unordered, DirectoryListingCompleteness.Complete, 0);
        VisiblePageCapacity capacity = Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(
            VisiblePageCapacity.Create(8)).Capacity;

        PaneState state = Assert.IsInstanceOfType<PaneStateAccepted>(PaneState.Create(
            listing.Location,
            listing.Entries,
            capacity,
            HiddenItemVisibility.Hidden)).State;

        Assert.AreSame(listing.Entries[0].Path, state.FocusItem);
        Assert.AreEqual("src", listing.Entries[0].Name);
    }

    /// <summary>Proves two entries with one provider identity are rejected.</summary>
    [TestMethod]
    public void CreateWhenEntriesShareIdentityDuplicateEntryRejection()
    {
        FileSystemPath location = ParsePath("C:\\same");
        DirectoryEntry[] entries = [
            Entry(location, "Same", DirectoryEntryKind.File),
            DirectoryEntry.Create(
                ParsePath("c:\\same\\same"),
                "same",
                DirectoryEntryKind.File,
                EntryVisibility.Normal),
        ];

        DirectoryListingCreation outcome = DirectoryListing.Create(
            location,
            entries,
            DirectoryListingCompleteness.Complete,
            0);

        Assert.AreSame(
            DirectoryListingFailureKind.DuplicateEntry,
            Assert.IsInstanceOfType<DirectoryListingRejected>(outcome).Kind);
    }

    /// <summary>Proves a null entry is rejected before ordering.</summary>
    [TestMethod]
    public void CreateWhenEntriesContainNullNullEntryRejection()
    {
        DirectoryEntry[] entries = new DirectoryEntry[1];

        DirectoryListingCreation outcome = DirectoryListing.Create(
            ParsePath("C:\\x"),
            entries,
            DirectoryListingCompleteness.Complete,
            0);

        Assert.AreSame(
            DirectoryListingFailureKind.NullEntry,
            Assert.IsInstanceOfType<DirectoryListingRejected>(outcome).Kind);
    }

    /// <summary>Proves the listing boundary rejects hostile entry counts.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-011")]
    public void CreateWhenEntryCountExceedsBoundaryTooManyEntriesRejection()
    {
        FileSystemPath location = ParsePath("C:\\many");

        DirectoryListingCreation outcome = DirectoryListing.Create(
            location,
            CreateEntries(location, DirectoryListing.EntryBoundaryLimit + 1),
            DirectoryListingCompleteness.Complete,
            0);

        Assert.AreSame(
            DirectoryListingFailureKind.TooManyEntries,
            Assert.IsInstanceOfType<DirectoryListingRejected>(outcome).Kind);
    }

    /// <summary>Proves the exact boundary is still accepted.</summary>
    [TestMethod]
    public void CreateWhenEntryCountEqualsBoundaryAccepted()
    {
        FileSystemPath location = ParsePath("C:\\many");

        DirectoryListingCreation outcome = DirectoryListing.Create(
            location,
            CreateEntries(location, DirectoryListing.EntryBoundaryLimit),
            DirectoryListingCompleteness.Bounded,
            0);

        DirectoryListing listing = Assert.IsInstanceOfType<DirectoryListingAccepted>(outcome).Listing;
        Assert.HasCount(DirectoryListing.EntryBoundaryLimit, listing.Entries);
        Assert.AreSame(DirectoryListingCompleteness.Bounded, listing.Completeness);
    }

    /// <summary>Proves an adapter defect in the omitted count is not a typed outcome.</summary>
    [TestMethod]
    public void CreateWhenUnrepresentableCountIsNegativeThrowsArgumentOutOfRangeException()
    {
        FileSystemPath location = ParsePath("C:\\x");

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => DirectoryListing.Create(
            location,
            [],
            DirectoryListingCompleteness.Complete,
            -1));
    }

    /// <summary>Proves the unrepresentable count is preserved verbatim.</summary>
    [TestMethod]
    public void CreateWhenEntriesWereOmittedPreservesCount()
    {
        DirectoryListing listing = CreateListing(ParsePath("C:\\x"), [], DirectoryListingCompleteness.Complete, 3);

        Assert.AreEqual(3, listing.UnrepresentableEntryCount);
        Assert.IsEmpty(listing.Entries);
    }

    /// <summary>Proves listings own their entry snapshot.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-011")]
    public void CreateWhenCallerChangesInputListingRetainsFrozenEntries()
    {
        FileSystemPath location = ParsePath("C:\\x");
        DirectoryEntry original = Entry(location, "original", DirectoryEntryKind.File);
        List<DirectoryEntry> entries = [original];

        DirectoryListing listing = CreateListing(location, entries, DirectoryListingCompleteness.Complete, 0);
        entries.Clear();

        Assert.HasCount(1, listing.Entries);
        Assert.AreSame(original, listing.Entries[0]);
    }

    /// <summary>Proves an empty or whitespace entry name is an adapter defect.</summary>
    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void CreateWhenEntryNameIsBlankThrowsArgumentException(string name)
    {
        FileSystemPath path = ParsePath("C:\\x\\entry");

        _ = Assert.ThrowsExactly<ArgumentException>(() => DirectoryEntry.Create(
            path,
            name,
            DirectoryEntryKind.File,
            EntryVisibility.Normal));
    }

    /// <summary>Proves closed read outcomes carry their exact payload.</summary>
    [TestMethod]
    public void OutcomeFactoriesWhenInvokedCarryExactPayload()
    {
        DirectoryListing listing = CreateListing(ParsePath("C:\\x"), [], DirectoryListingCompleteness.Complete, 0);

        DirectoryReadOutcome succeeded = DirectoryReadOutcome.Succeeded(listing);
        DirectoryReadOutcome cancelled = DirectoryReadOutcome.Cancelled();
        DirectoryReadOutcome failed = DirectoryReadOutcome.Failed(FileOperationFailureKind.NotFound);

        Assert.AreSame(listing, Assert.IsInstanceOfType<DirectoryReadSucceeded>(succeeded).Listing);
        _ = Assert.IsInstanceOfType<DirectoryReadCancelled>(cancelled);
        Assert.AreSame(
            FileOperationFailureKind.NotFound,
            Assert.IsInstanceOfType<DirectoryReadFailed>(failed).Failure);
    }

    private static DirectoryEntry[] CreateEntries(FileSystemPath location, int count)
    {
        DirectoryEntry[] entries = new DirectoryEntry[count];
        for (int index = 0; index < count; index++)
        {
            string name = "entry-" + index.ToString(CultureInfo.InvariantCulture);
            entries[index] = Entry(location, name, DirectoryEntryKind.File);
        }
        return entries;
    }

    private static DirectoryListing CreateListing(
        FileSystemPath location,
        IReadOnlyList<DirectoryEntry> entries,
        DirectoryListingCompleteness completeness,
        int unrepresentableEntryCount)
    {
        DirectoryListingCreation outcome = DirectoryListing.Create(
            location,
            entries,
            completeness,
            unrepresentableEntryCount);
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(outcome).Listing;
    }

    private static DirectoryEntry Entry(FileSystemPath location, string name, DirectoryEntryKind kind)
    {
        return DirectoryEntry.Create(
            ParsePath(location.CanonicalText + "\\" + name),
            name,
            kind,
            EntryVisibility.Normal);
    }

    private static FileSystemPath ParsePath(string input)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input)).Path;
    }
}
