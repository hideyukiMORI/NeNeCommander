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
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>
/// Proves UNC share direct enumeration through an injected enumerator. No test touches the network:
/// the scripted enumerator stands in for the share, and the failure table is the normalizer's own.
/// </summary>
[TestClass]
public sealed class WindowsUncDirectoryReaderTests
{
    private const string ShareDirectory = "\\\\server\\share\\docs";

    /// <summary>Proves kinds, canonical UNC children, and directory-first ordering in one listing.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenEntriesAreValidReturnsCanonicalChildrenInOneListing()
    {
        ScriptedEnumerator enumerator = new([
            Entry("beta.txt", DirectoryEntryKind.File),
            Entry("Alpha.txt", DirectoryEntryKind.File),
            Entry("zulu", DirectoryEntryKind.Directory),
        ]);

        DirectoryReadOutcome outcome = await Reader(enumerator).ReadAsync(
            Request(ShareDirectory, 8),
            CancellationToken.None);

        DirectoryListing listing = Assert.IsInstanceOfType<DirectoryReadSucceeded>(outcome).Listing;
        string[] expectedNames = ["zulu", "Alpha.txt", "beta.txt"];
        CollectionAssert.AreEqual(expectedNames, listing.Entries.Select(entry => entry.Name).ToArray());
        Assert.AreEqual("\\\\server\\share\\docs\\zulu", listing.Entries[0].Path.CanonicalText);
        _ = Assert.IsInstanceOfType<WindowsUncPath>(listing.Entries[0].Path);
        Assert.AreSame(DirectoryEntryKind.Directory, listing.Entries[0].Kind);
        Assert.AreSame(DirectoryEntryKind.File, listing.Entries[1].Kind);
        Assert.AreSame(DirectoryListingCompleteness.Complete, listing.Completeness);
        Assert.AreEqual(0, listing.UnrepresentableEntryCount);
        Assert.AreEqual(ShareDirectory, enumerator.RequestedLocation);
    }

    /// <summary>Proves the share root is enumerated by its canonical text and yields share children.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenLocationIsShareRootEnumeratesCanonicalRoot()
    {
        ScriptedEnumerator enumerator = new([Entry("top", DirectoryEntryKind.Directory)]);

        DirectoryListing listing = Assert.IsInstanceOfType<DirectoryReadSucceeded>(
            await Reader(enumerator).ReadAsync(Request("\\\\server\\C$", 8), CancellationToken.None)).Listing;

        Assert.AreEqual("\\\\server\\C$\\", enumerator.RequestedLocation);
        Assert.AreEqual("\\\\server\\C$\\top", listing.Entries[0].Path.CanonicalText);
    }

    /// <summary>
    /// Proves visibility follows the Hidden and System attributes as on the Windows local provider,
    /// and that a leading dot is an ordinary name rather than the WSL rule.
    /// </summary>
    [TestMethod]
    public async Task ReadAsyncWhenAttributesVaryReportsWindowsAttributeVisibility()
    {
        ScriptedEnumerator enumerator = new([
            Entry(".dotted", DirectoryEntryKind.File),
            Entry("hidden", DirectoryEntryKind.File, FileAttributes.Hidden),
            Entry("system", DirectoryEntryKind.File, FileAttributes.System),
            Entry("both", DirectoryEntryKind.Directory, FileAttributes.Hidden | FileAttributes.System),
            Entry("plain", DirectoryEntryKind.File, FileAttributes.ReadOnly | FileAttributes.Archive),
        ]);

        DirectoryListing listing = Assert.IsInstanceOfType<DirectoryReadSucceeded>(
            await Reader(enumerator).ReadAsync(Request(ShareDirectory, 8), CancellationToken.None)).Listing;

        Assert.AreSame(EntryVisibility.Normal, VisibilityOf(listing, ".dotted"));
        Assert.AreSame(EntryVisibility.Hidden, VisibilityOf(listing, "hidden"));
        Assert.AreSame(EntryVisibility.Hidden, VisibilityOf(listing, "system"));
        Assert.AreSame(EntryVisibility.Hidden, VisibilityOf(listing, "both"));
        Assert.AreSame(EntryVisibility.Normal, VisibilityOf(listing, "plain"));
        Assert.HasCount(5, listing.Entries);
    }

    /// <summary>Proves the snapshot size and modification time travel onto each entry unchanged.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenSnapshotsCarryMetadataReturnsItOnEachEntry()
    {
        EntrySize size = EntrySize.Create(1024);
        EntryTimestamp modified = EntryTimestamp.Create(new DateTimeOffset(2026, 10, 9, 1, 2, 3, TimeSpan.Zero));
        ScriptedEnumerator enumerator = new([
            new WindowsDirectoryEntrySnapshot("report.pdf", FileAttributes.Hidden, size, modified),
        ]);

        DirectoryListing listing = Assert.IsInstanceOfType<DirectoryReadSucceeded>(
            await Reader(enumerator).ReadAsync(Request(ShareDirectory, 8), CancellationToken.None)).Listing;

        Assert.AreEqual(EntryMetadata.Create(EntryVisibility.Hidden, size, modified), listing.Entries[0].Metadata);
    }

    /// <summary>Proves exactly the requested count is complete while one more entry is bounded.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-011")]
    public async Task ReadAsyncAtAndBeyondEntryBoundaryReportsExactCompleteness()
    {
        WindowsDirectoryEntrySnapshot[] exactEntries = [
            Entry("one", DirectoryEntryKind.File),
            Entry("two", DirectoryEntryKind.File),
        ];
        WindowsDirectoryEntrySnapshot[] excessiveEntries = [
            .. exactEntries,
            Entry("three", DirectoryEntryKind.File),
            Entry("four", DirectoryEntryKind.File),
        ];
        ScriptedEnumerator excessive = new(excessiveEntries);

        DirectoryListing exact = Assert.IsInstanceOfType<DirectoryReadSucceeded>(
            await Reader(new ScriptedEnumerator(exactEntries)).ReadAsync(
                Request(ShareDirectory, 2),
                CancellationToken.None)).Listing;
        DirectoryListing bounded = Assert.IsInstanceOfType<DirectoryReadSucceeded>(
            await Reader(excessive).ReadAsync(Request(ShareDirectory, 2), CancellationToken.None)).Listing;

        Assert.AreSame(DirectoryListingCompleteness.Complete, exact.Completeness);
        Assert.AreSame(DirectoryListingCompleteness.Bounded, bounded.Completeness);
        Assert.HasCount(2, exact.Entries);
        Assert.HasCount(2, bounded.Entries);
        Assert.AreEqual(3, excessive.YieldCount);
    }

    /// <summary>Proves a name the UNC path model rejects is counted and still consumes the bound.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-015")]
    public async Task ReadAsyncWhenNamesCannotBeRepresentedCountsThemWithinTheBound()
    {
        ScriptedEnumerator enumerator = new([
            Entry("trailing.", DirectoryEntryKind.File),
            Entry("CON", DirectoryEntryKind.File),
            Entry("valid", DirectoryEntryKind.File),
            Entry("beyond", DirectoryEntryKind.File),
        ]);

        DirectoryListing listing = Assert.IsInstanceOfType<DirectoryReadSucceeded>(
            await Reader(enumerator).ReadAsync(Request(ShareDirectory, 3), CancellationToken.None)).Listing;

        Assert.HasCount(1, listing.Entries);
        Assert.AreEqual("valid", listing.Entries[0].Name);
        Assert.AreEqual(2, listing.UnrepresentableEntryCount);
        Assert.AreSame(DirectoryListingCompleteness.Bounded, listing.Completeness);
    }

    /// <summary>Proves cancellation before or between entries publishes no partial listing.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-011")]
    public async Task ReadAsyncWhenCancelledBeforeOrBetweenEntriesReturnsCancelled()
    {
        using CancellationTokenSource before = new();
        before.Cancel();
        ScriptedEnumerator untouched = new([Entry("one", DirectoryEntryKind.File)]);
        _ = Assert.IsInstanceOfType<DirectoryReadCancelled>(
            await Reader(untouched).ReadAsync(Request(ShareDirectory, 8), before.Token));

        using CancellationTokenSource between = new();
        ScriptedEnumerator cancelling = new(
            [
                Entry("one", DirectoryEntryKind.File),
                Entry("two", DirectoryEntryKind.File),
                Entry("three", DirectoryEntryKind.File),
            ],
            index =>
            {
                if (index == 1)
                {
                    between.Cancel();
                }
            });
        _ = Assert.IsInstanceOfType<DirectoryReadCancelled>(
            await Reader(cancelling).ReadAsync(Request(ShareDirectory, 8), between.Token));

        Assert.AreEqual(0, untouched.InvocationCount);
        Assert.AreEqual(2, cancelling.YieldCount);
    }

    /// <summary>
    /// Proves logon, credential-conflict, unreachable, timeout, and missing-share failures reach the
    /// pane as closed failures through the reader, never as an empty listing.
    /// </summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-017")]
    [TestProperty("ThreatId", "ADV-021")]
    public async Task ReadAsyncWhenShareFailsReturnsClosedFailureWithoutListing()
    {
        DirectoryReadFailed logon = await FailureAsync(
            new IOException("Synthetic logon failure.", unchecked((int)0x8007052E)));
        DirectoryReadFailed conflict = await FailureAsync(
            new IOException("Synthetic credential conflict.", unchecked((int)0x800704C3)));
        DirectoryReadFailed denied = await FailureAsync(new UnauthorizedAccessException());
        DirectoryReadFailed host = await FailureAsync(
            new IOException("Synthetic host loss.", unchecked((int)0x800704D0)));
        DirectoryReadFailed badPath = await FailureAsync(
            new IOException("Synthetic bad path.", unchecked((int)0x80070035)));
        DirectoryReadFailed timeout = await FailureAsync(
            new IOException("Synthetic timeout.", unchecked((int)0x80070079)));
        DirectoryReadFailed missing = await FailureAsync(new DirectoryNotFoundException());

        Assert.AreSame(FileOperationFailureKind.AccessDenied, logon.Failure);
        Assert.AreSame(FileOperationFailureKind.AccessDenied, conflict.Failure);
        Assert.AreSame(FileOperationFailureKind.AccessDenied, denied.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, host.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, badPath.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, timeout.Failure);
        Assert.AreSame(FileOperationFailureKind.NotFound, missing.Failure);
    }

    /// <summary>Proves every row of the Windows failure table, including the UNC logon and network rows.</summary>
    /// <param name="hResult">HRESULT_FROM_WIN32 value captured from the adapter exception.</param>
    /// <param name="expected">Name of the canonical failure kind.</param>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-021")]
    [DataRow(unchecked((int)0x80070005), "AccessDenied")] // ERROR_ACCESS_DENIED (5)
    [DataRow(unchecked((int)0x8007052E), "AccessDenied")] // ERROR_LOGON_FAILURE (1326)
    [DataRow(unchecked((int)0x800704C3), "AccessDenied")] // ERROR_SESSION_CREDENTIAL_CONFLICT (1219)
    [DataRow(unchecked((int)0x80070002), "NotFound")] // ERROR_FILE_NOT_FOUND (2)
    [DataRow(unchecked((int)0x80070003), "NotFound")] // ERROR_PATH_NOT_FOUND (3)
    [DataRow(unchecked((int)0x80070035), "ProviderUnavailable")] // ERROR_BAD_NETPATH (53)
    [DataRow(unchecked((int)0x80070043), "ProviderUnavailable")] // ERROR_BAD_NET_NAME (67)
    [DataRow(unchecked((int)0x800704CF), "ProviderUnavailable")] // ERROR_NETWORK_UNREACHABLE (1231)
    [DataRow(unchecked((int)0x800704D0), "ProviderUnavailable")] // ERROR_HOST_UNREACHABLE (1232)
    [DataRow(unchecked((int)0x80070079), "ProviderUnavailable")] // ERROR_SEM_TIMEOUT (121)
    [DataRow(unchecked((int)0x80070040), "ProviderUnavailable")] // ERROR_NETNAME_DELETED (64)
    [DataRow(unchecked((int)0x8007003B), "ProviderUnavailable")] // ERROR_UNEXP_NET_ERR (59)
    [DataRow(unchecked((int)0x80004005), "ProviderUnavailable")] // E_FAIL: an unknown value stays closed
    public void NormalizeWhenWindowsFailureIsKnownReturnsItsClosedKind(int hResult, string expected)
    {
        FileOperationFailureKind actual = WindowsFileFailureNormalizer.Normalize(hResult);

        Assert.AreEqual(expected, actual.GetType().Name.Replace("Failure", string.Empty, StringComparison.Ordinal));
    }

    /// <summary>Proves the UNC adapter serves only UNC locations and never reinterprets another provider.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenLocationIsNotUncReturnsProviderUnavailable()
    {
        ScriptedEnumerator enumerator = new([Entry("one", DirectoryEntryKind.File)]);
        WindowsUncDirectoryReader reader = Reader(enumerator);

        DirectoryReadOutcome local = await reader.ReadAsync(Request("C:\\", 8), CancellationToken.None);
        DirectoryReadOutcome wsl = await reader.ReadAsync(
            Request("\\\\wsl.localhost\\Ubuntu\\home", 8),
            CancellationToken.None);

        Assert.AreSame(
            FileOperationFailureKind.ProviderUnavailable,
            Assert.IsInstanceOfType<DirectoryReadFailed>(local).Failure);
        Assert.AreSame(
            FileOperationFailureKind.ProviderUnavailable,
            Assert.IsInstanceOfType<DirectoryReadFailed>(wsl).Failure);
        Assert.AreEqual(0, enumerator.InvocationCount);
    }

    /// <summary>Proves every required UNC adapter argument is rejected at its boundary.</summary>
    [TestMethod]
    public void ConstructorsAndReadAsyncWhenArgumentIsNullRejectDefect()
    {
        ScriptedEnumerator enumerator = new([]);

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WindowsUncDirectoryReader(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => new WindowsUncDirectoryReader(null!, enumerator));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => new WindowsUncDirectoryReader(new WindowsLocalIoExecutionBoundary(), null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => Reader(enumerator).ReadAsync(null!, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => WindowsDirectoryReadOperation.ClassifyByAttributes(null!));
    }

    private static EntryVisibility VisibilityOf(DirectoryListing listing, string name)
    {
        return listing.Entries.Single(entry => entry.Name == name).Metadata.Visibility;
    }

    private static async Task<DirectoryReadFailed> FailureAsync(Exception failure)
    {
        DirectoryReadOutcome outcome = await Reader(new ScriptedEnumerator(failure)).ReadAsync(
            Request(ShareDirectory, 8),
            CancellationToken.None);
        return Assert.IsInstanceOfType<DirectoryReadFailed>(outcome);
    }

    private static WindowsUncDirectoryReader Reader(ScriptedEnumerator enumerator)
    {
        return new WindowsUncDirectoryReader(new WindowsLocalIoExecutionBoundary(), enumerator);
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

        internal string? RequestedLocation { get; private set; }

        public IEnumerable<WindowsDirectoryEntrySnapshot> Enumerate(string canonicalLocation)
        {
            InvocationCount++;
            RequestedLocation = canonicalLocation;
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
}
