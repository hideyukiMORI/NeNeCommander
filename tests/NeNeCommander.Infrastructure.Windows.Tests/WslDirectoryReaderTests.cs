using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Directories;
using NeNeCommander.Infrastructure.Windows.Execution;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves WSL direct enumeration preserves Linux identity and fails closed.</summary>
[TestClass]
public sealed class WslDirectoryReaderTests
{
    /// <summary>Proves case-sensitive siblings, kind, order, canonical children, and dot visibility.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenEntriesAreValidReturnsProviderFactsInOneListing()
    {
        ScriptedEnumerator enumerator = new([
            Entry("case", DirectoryEntryKind.File),
            Entry("Case", DirectoryEntryKind.File),
            Entry("plain", DirectoryEntryKind.File, FileAttributes.Hidden),
            Entry(".config", DirectoryEntryKind.Directory),
        ]);
        WslDirectoryReader reader = Reader(enumerator);

        DirectoryReadOutcome outcome = await reader.ReadAsync(
            Request("\\\\wsl.localhost\\Ubuntu\\home", 8),
            CancellationToken.None);

        DirectoryListing listing = Assert.IsInstanceOfType<DirectoryReadSucceeded>(outcome).Listing;
        string[] expectedNames = [".config", "Case", "case", "plain"];
        CollectionAssert.AreEqual(
            expectedNames,
            listing.Entries.Select(entry => entry.Name).ToArray());
        Assert.AreEqual("\\\\wsl.localhost\\Ubuntu\\home\\.config", listing.Entries[0].Path.CanonicalText);
        Assert.AreSame(EntryVisibility.Hidden, listing.Entries[0].Metadata.Visibility);
        Assert.AreSame(EntryVisibility.Normal, listing.Entries[1].Metadata.Visibility);
        Assert.AreSame(EntryVisibility.Normal, listing.Entries[2].Metadata.Visibility);
        Assert.AreSame(EntryVisibility.Normal, listing.Entries[3].Metadata.Visibility);
        Assert.AreSame(DirectoryEntryKind.Directory, listing.Entries[0].Kind);
        Assert.AreSame(DirectoryListingCompleteness.Complete, listing.Completeness);
    }

    /// <summary>
    /// Proves the WSL route carries the snapshot metadata into each entry unchanged, known or unknown,
    /// because it travels the same shared read operation as the Windows local route.
    /// </summary>
    [TestMethod]
    public async Task ReadAsyncWhenSnapshotsCarryMetadataReturnsItOnEachEntry()
    {
        EntrySize size = EntrySize.Create(4096);
        EntryTimestamp modified = EntryTimestamp.Create(new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.Zero));
        ScriptedEnumerator enumerator = new([
            new WindowsDirectoryEntrySnapshot("data.bin", FileAttributes.None, size, modified),
            Entry(".src", DirectoryEntryKind.Directory),
        ]);

        DirectoryListing listing = Assert.IsInstanceOfType<DirectoryReadSucceeded>(
            await Reader(enumerator).ReadAsync(
                Request("\\\\wsl.localhost\\Ubuntu\\home", 8),
                CancellationToken.None)).Listing;

        Assert.AreEqual(".src", listing.Entries[0].Name);
        Assert.AreEqual(EntryMetadata.Unmeasured(EntryVisibility.Hidden), listing.Entries[0].Metadata);
        Assert.AreEqual(EntryMetadata.Create(EntryVisibility.Normal, size, modified), listing.Entries[1].Metadata);
    }

    /// <summary>Proves exactly the requested count is complete while one more entry is bounded.</summary>
    [TestMethod]
    public async Task ReadAsyncAtAndBeyondEntryBoundaryReportsExactCompleteness()
    {
        WindowsDirectoryEntrySnapshot[] exactEntries = [
            Entry("one", DirectoryEntryKind.File),
            Entry("two", DirectoryEntryKind.File),
        ];
        WindowsDirectoryEntrySnapshot[] excessiveEntries = [
            .. exactEntries,
            Entry("three", DirectoryEntryKind.File),
        ];

        DirectoryListing exact = Assert.IsInstanceOfType<DirectoryReadSucceeded>(
            await Reader(new ScriptedEnumerator(exactEntries)).ReadAsync(
                Request("\\\\wsl.localhost\\Ubuntu\\", 2),
                CancellationToken.None)).Listing;
        DirectoryListing bounded = Assert.IsInstanceOfType<DirectoryReadSucceeded>(
            await Reader(new ScriptedEnumerator(excessiveEntries)).ReadAsync(
                Request("\\\\wsl.localhost\\Ubuntu\\", 2),
                CancellationToken.None)).Listing;

        Assert.AreSame(DirectoryListingCompleteness.Complete, exact.Completeness);
        Assert.AreSame(DirectoryListingCompleteness.Bounded, bounded.Completeness);
        Assert.HasCount(2, exact.Entries);
        Assert.HasCount(2, bounded.Entries);
    }

    /// <summary>Proves hostile names are counted and every provider entry consumes the bound.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-011")]
    [TestProperty("ThreatId", "ADV-015")]
    public async Task ReadAsyncWhenNamesAreInvalidOrExcessiveCountsAndBoundsThem()
    {
        ScriptedEnumerator enumerator = new([
            Entry("bad/name", DirectoryEntryKind.File),
            Entry("valid", DirectoryEntryKind.File),
            Entry("beyond", DirectoryEntryKind.File),
        ]);

        DirectoryReadOutcome outcome = await Reader(enumerator).ReadAsync(
            Request("\\\\wsl.localhost\\Ubuntu\\", 2),
            CancellationToken.None);

        DirectoryListing listing = Assert.IsInstanceOfType<DirectoryReadSucceeded>(outcome).Listing;
        Assert.HasCount(1, listing.Entries);
        Assert.AreEqual("valid", listing.Entries[0].Name);
        Assert.AreEqual(1, listing.UnrepresentableEntryCount);
        Assert.AreSame(DirectoryListingCompleteness.Bounded, listing.Completeness);
    }

    /// <summary>Proves cancellation before or during enumeration publishes no partial listing.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenCancelledBeforeOrDuringEnumerationReturnsCancelled()
    {
        using CancellationTokenSource before = new();
        before.Cancel();
        ScriptedEnumerator untouched = new([Entry("one", DirectoryEntryKind.File)]);
        _ = Assert.IsInstanceOfType<DirectoryReadCancelled>(
            await Reader(untouched).ReadAsync(Request("\\\\wsl.localhost\\Ubuntu\\", 8), before.Token));

        using CancellationTokenSource during = new();
        ScriptedEnumerator cancelling = new(
            [Entry("one", DirectoryEntryKind.File), Entry("two", DirectoryEntryKind.File)],
            index =>
            {
                if (index == 1)
                {
                    during.Cancel();
                }
            });
        _ = Assert.IsInstanceOfType<DirectoryReadCancelled>(
            await Reader(cancelling).ReadAsync(Request("\\\\wsl.localhost\\Ubuntu\\", 8), during.Token));

        Assert.AreEqual(0, untouched.InvocationCount);
        Assert.AreEqual(2, cancelling.YieldCount);
    }

    /// <summary>Proves provider and path failures normalize without becoming empty success.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-017")]
    public async Task ReadAsyncWhenEnumerationFailsReturnsCanonicalFailure()
    {
        DirectoryReadFailed denied = await FailureAsync(new UnauthorizedAccessException());
        DirectoryReadFailed missing = await FailureAsync(new DirectoryNotFoundException());
        DirectoryReadFailed unavailable = await FailureAsync(new IOException("Synthetic provider loss."));

        Assert.AreSame(FileOperationFailureKind.AccessDenied, denied.Failure);
        Assert.AreSame(FileOperationFailureKind.NotFound, missing.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, unavailable.Failure);
    }

    /// <summary>Proves the WSL adapter cannot reinterpret another provider or absent dependency.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenProviderOrDependencyIsInvalidFailsAtBoundary()
    {
        ScriptedEnumerator enumerator = new([]);
        WslDirectoryReader reader = Reader(enumerator);

        DirectoryReadOutcome unsupported = await reader.ReadAsync(Request("C:\\", 8), CancellationToken.None);

        Assert.AreSame(
            FileOperationFailureKind.ProviderUnavailable,
            Assert.IsInstanceOfType<DirectoryReadFailed>(unsupported).Failure);
        Assert.AreEqual(0, enumerator.InvocationCount);
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WslDirectoryReader(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => new WslDirectoryReader(new WindowsLocalIoExecutionBoundary(), null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => reader.ReadAsync(null!, CancellationToken.None));
    }

    /// <summary>Proves shared enumeration components reject incomplete adapter state.</summary>
    [TestMethod]
    public void SharedEnumerationWhenDependencyIsInvalidRejectsDefect()
    {
        DirectoryReadRequest request = Request("\\\\wsl.localhost\\Ubuntu\\", 8);
        ScriptedEnumerator enumerator = new([]);
        WindowsLocalIoExecutionBoundary boundary = new();

        static EntryVisibility Visibility(WindowsDirectoryEntrySnapshot _)
        {
            return EntryVisibility.Normal;
        }

        _ = Assert.ThrowsExactly<ArgumentException>(
            () => new WindowsDirectoryEntrySnapshot("", FileAttributes.None, EntrySize.Unknown, EntryTimestamp.Unknown));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => new WindowsDirectoryEntrySnapshot("item", FileAttributes.None, null!, EntryTimestamp.Unknown));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => new WindowsDirectoryEntrySnapshot("item", FileAttributes.None, EntrySize.Unknown, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => new WindowsDirectoryReadOperation(() => enumerator.Enumerate("location"), Visibility, () => null)
                .Read(null!, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => new WindowsDirectoryReadOperation(null!, Visibility, () => null).Read(request, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => new WindowsDirectoryReadOperation(() => enumerator.Enumerate("location"), null!, () => null)
                .Read(request, CancellationToken.None));
        Assert.AreEqual(0, enumerator.InvocationCount);
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => WindowsDirectoryReadOperation.TranslateListingCreation(null!, null));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => new WindowsLocalDirectoryReader(null!, enumerator));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => new WindowsLocalDirectoryReader(boundary, null!));
    }

    /// <summary>
    /// Proves the reader addresses the enumeration through the resolver's extended-length text,
    /// never through the ordinary UNC form, while the listing keeps the validated location.
    /// </summary>
    /// <returns>The running assertion.</returns>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-015")]
    public async Task ReadAsyncAddressesTheEnumerationThroughExtendedLengthText()
    {
        ScriptedEnumerator enumerator = new([Entry("foo.", DirectoryEntryKind.File)]);

        DirectoryListing listing = Assert.IsInstanceOfType<DirectoryReadSucceeded>(
            await Reader(enumerator).ReadAsync(
                Request("\\\\wsl.localhost\\Ubuntu\\home\\sub.", 8),
                CancellationToken.None)).Listing;

        string[] expectedLocations = ["\\\\?\\UNC\\wsl.localhost\\Ubuntu\\home\\sub."];
        CollectionAssert.AreEqual(expectedLocations, enumerator.Locations);
        Assert.AreEqual("\\\\wsl.localhost\\Ubuntu\\home\\sub.", listing.Location.CanonicalText);
        Assert.AreEqual("\\\\wsl.localhost\\Ubuntu\\home\\sub.\\foo.", listing.Entries[0].Path.CanonicalText);
    }

    /// <summary>
    /// Proves a real listing that contains trailing-dot names projects every entry onto its
    /// validated WSL path by its exact name, and no extended-length text reaches the listing.
    /// </summary>
    /// <returns>The running assertion.</returns>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-015")]
    public async Task ReadAsyncWhenNamesEndWithDotProjectsExactNamesOntoWslPaths()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.WriteVerbatimFile("foo.", "dot");
        _ = root.CreateVerbatimDirectory("sub.");
        _ = root.WriteVerbatimFile("plain", "plain");
        WslDirectoryReader reader = new(
            new WindowsLocalIoExecutionBoundary(),
            new OwnedEnumerator(root.ResolveVerbatim(string.Empty)));

        DirectoryListing listing = Assert.IsInstanceOfType<DirectoryReadSucceeded>(
            await reader.ReadAsync(Request("\\\\wsl.localhost\\Ubuntu\\owned", 8), CancellationToken.None)).Listing;

        string[] expectedNames = ["sub.", "foo.", "plain"];
        CollectionAssert.AreEqual(expectedNames, listing.Entries.Select(entry => entry.Name).ToArray());
        string[] expectedPaths =
        [
            "\\\\wsl.localhost\\Ubuntu\\owned\\sub.",
            "\\\\wsl.localhost\\Ubuntu\\owned\\foo.",
            "\\\\wsl.localhost\\Ubuntu\\owned\\plain",
        ];
        CollectionAssert.AreEqual(
            expectedPaths,
            listing.Entries.Select(entry => entry.Path.CanonicalText).ToArray());
        Assert.AreSame(DirectoryEntryKind.Directory, listing.Entries[0].Kind);
        Assert.AreSame(DirectoryEntryKind.File, listing.Entries[1].Kind);
        Assert.AreEqual(0, listing.UnrepresentableEntryCount);
        Assert.AreEqual(EntrySize.Create(3), listing.Entries[1].Metadata.Size);
    }

    private static async Task<DirectoryReadFailed> FailureAsync(Exception failure)
    {
        DirectoryReadOutcome outcome = await Reader(new ScriptedEnumerator(failure)).ReadAsync(
            Request("\\\\wsl.localhost\\Ubuntu\\missing", 8),
            CancellationToken.None);
        return Assert.IsInstanceOfType<DirectoryReadFailed>(outcome);
    }

    private static WslDirectoryReader Reader(ScriptedEnumerator enumerator)
    {
        return new WslDirectoryReader(new WindowsLocalIoExecutionBoundary(), enumerator);
    }

    private static WindowsDirectoryEntrySnapshot Entry(string name, DirectoryEntryKind kind)
    {
        return Entry(name, kind, FileAttributes.None);
    }

    private static WindowsDirectoryEntrySnapshot Entry(
        string name,
        DirectoryEntryKind kind,
        FileAttributes attributes)
    {
        FileAttributes reported = kind == DirectoryEntryKind.Directory ? attributes | FileAttributes.Directory : attributes;
        return new WindowsDirectoryEntrySnapshot(name, reported, EntrySize.Unknown, EntryTimestamp.Unknown);
    }

    private static DirectoryReadRequest Request(string text, int boundary)
    {
        FileSystemPath path = Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path;
        DirectoryReadRequestCreation creation = DirectoryReadRequest.Create(path, boundary);
        return Assert.IsInstanceOfType<DirectoryReadRequestAccepted>(creation).Request;
    }

    private sealed class ScriptedEnumerator : IWindowsDirectoryEnumerator
    {
        private readonly Action<int> _beforeYield;
        private readonly IReadOnlyList<WindowsDirectoryEntrySnapshot> _entries;
        private readonly Exception? _failure;

        internal ScriptedEnumerator(IReadOnlyList<WindowsDirectoryEntrySnapshot> entries)
            : this(entries, _ => { })
        {
        }

        internal ScriptedEnumerator(
            IReadOnlyList<WindowsDirectoryEntrySnapshot> entries,
            Action<int> beforeYield)
        {
            _entries = entries;
            _beforeYield = beforeYield;
        }

        internal ScriptedEnumerator(Exception failure)
        {
            _entries = [];
            _beforeYield = _ => { };
            _failure = failure;
        }

        internal int InvocationCount { get; private set; }

        internal int YieldCount { get; private set; }

        internal List<string> Locations { get; } = [];

        public IEnumerable<WindowsDirectoryEntrySnapshot> Enumerate(string locationText)
        {
            InvocationCount++;
            Locations.Add(locationText);
            if (_failure is not null)
            {
                throw _failure;
            }
            for (int index = 0; index < _entries.Count; index++)
            {
                _beforeYield(index);
                YieldCount++;
                yield return _entries[index];
            }
        }
    }

    /// <summary>
    /// Rebases the resolver's text for <c>\\wsl.localhost\Ubuntu\owned</c> onto an extended-length
    /// test-owned root and enumerates it with the production enumerator.
    /// </summary>
    private sealed class OwnedEnumerator : IWindowsDirectoryEnumerator
    {
        private const string OwnedLocation = "\\\\?\\UNC\\wsl.localhost\\Ubuntu\\owned";

        private readonly string _verbatimRoot;

        internal OwnedEnumerator(string verbatimRoot)
        {
            _verbatimRoot = verbatimRoot;
        }

        public IEnumerable<WindowsDirectoryEntrySnapshot> Enumerate(string locationText)
        {
            return locationText.Equals(OwnedLocation, StringComparison.Ordinal)
                ? new WindowsDirectoryEnumerator().Enumerate(_verbatimRoot)
                : throw new InvalidOperationException("The reader addressed text other than the resolver's.");
        }
    }
}
