using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Directories;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the Windows local directory adapter against a test-owned temporary root.</summary>
[TestClass]
public sealed class WindowsLocalDirectoryReaderTests
{
    /// <summary>Proves an empty directory yields a complete empty listing.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenDirectoryIsEmptyReturnsCompleteEmptyListing()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalDirectoryReader reader = new();

        DirectoryReadOutcome outcome = await reader.ReadAsync(Request(root.Path, 8), CancellationToken.None);

        DirectoryListing listing = Assert.IsInstanceOfType<DirectoryReadSucceeded>(outcome).Listing;
        Assert.IsEmpty(listing.Entries);
        Assert.AreSame(DirectoryListingCompleteness.Complete, listing.Completeness);
        Assert.AreEqual(0, listing.UnrepresentableEntryCount);
        Assert.AreSame(root.Path, listing.Location);
    }

    /// <summary>Proves files and directories are classified, named, and ordered deterministically.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenDirectoryHasFilesAndDirectoriesOrdersDirectoriesFirstThenByName()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateFile("beta.txt");
        _ = root.CreateFile("Alpha.txt");
        _ = root.CreateDirectory("zulu");
        _ = root.CreateDirectory("Charlie");
        _ = root.CreateFile("zulu\\nested.txt");
        WindowsLocalDirectoryReader reader = new();

        DirectoryReadOutcome outcome = await reader.ReadAsync(Request(root.Path, 8), CancellationToken.None);

        DirectoryListing listing = Assert.IsInstanceOfType<DirectoryReadSucceeded>(outcome).Listing;
        string[] names = [.. listing.Entries.Select(entry => entry.Name)];
        string[] expected = ["Charlie", "zulu", "Alpha.txt", "beta.txt"];
        CollectionAssert.AreEqual(expected, names);
        Assert.AreSame(DirectoryEntryKind.Directory, listing.Entries[0].Kind);
        Assert.AreSame(DirectoryEntryKind.Directory, listing.Entries[1].Kind);
        Assert.AreSame(DirectoryEntryKind.File, listing.Entries[2].Kind);
        Assert.AreSame(DirectoryEntryKind.File, listing.Entries[3].Kind);
        foreach (DirectoryEntry entry in listing.Entries)
        {
            FileSystemPath expectedPath = ParsePath(root.Resolve(entry.Name));
            Assert.IsTrue(FileSystemPathIdentityComparer.Instance.Equals(expectedPath, entry.Path));
        }
    }

    /// <summary>Proves hidden entries are reported rather than silently skipped, and marked hidden.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenEntryIsHiddenListsItWithHiddenVisibility()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateHiddenFile("hidden.txt");
        WindowsLocalDirectoryReader reader = new();

        DirectoryReadOutcome outcome = await reader.ReadAsync(Request(root.Path, 8), CancellationToken.None);

        DirectoryListing listing = Assert.IsInstanceOfType<DirectoryReadSucceeded>(outcome).Listing;
        Assert.HasCount(1, listing.Entries);
        Assert.AreEqual("hidden.txt", listing.Entries[0].Name);
        Assert.AreSame(EntryVisibility.Hidden, listing.Entries[0].Visibility);
    }

    /// <summary>Proves the reported visibility comes from the attributes of every entry class.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenEntriesCarryAttributesReportsVisibilityFromThemAlone()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateHiddenDirectory("hidden-directory");
        _ = root.CreateDirectory("plain-directory");
        _ = root.CreateHiddenFile("hidden.txt");
        _ = root.CreateSystemFile("system.txt");
        _ = root.CreateFile("plain.txt");
        _ = root.CreateFile(".dotted.txt");
        WindowsLocalDirectoryReader reader = new();

        DirectoryReadOutcome outcome = await reader.ReadAsync(Request(root.Path, 8), CancellationToken.None);

        DirectoryListing listing = Assert.IsInstanceOfType<DirectoryReadSucceeded>(outcome).Listing;
        Assert.AreSame(EntryVisibility.Hidden, VisibilityOf(listing, "hidden-directory"));
        Assert.AreSame(EntryVisibility.Normal, VisibilityOf(listing, "plain-directory"));
        Assert.AreSame(EntryVisibility.Hidden, VisibilityOf(listing, "hidden.txt"));
        Assert.AreSame(EntryVisibility.Hidden, VisibilityOf(listing, "system.txt"));
        Assert.AreSame(EntryVisibility.Normal, VisibilityOf(listing, "plain.txt"));
        Assert.AreSame(EntryVisibility.Normal, VisibilityOf(listing, ".dotted.txt"));
    }

    /// <summary>Proves enumeration stops at the requested boundary and reports it.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-011")]
    public async Task ReadAsyncWhenEntriesExceedBoundaryReturnsBoundedListing()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateFile("one.txt");
        _ = root.CreateFile("two.txt");
        _ = root.CreateFile("three.txt");
        WindowsLocalDirectoryReader reader = new();

        DirectoryReadOutcome outcome = await reader.ReadAsync(Request(root.Path, 2), CancellationToken.None);

        DirectoryListing listing = Assert.IsInstanceOfType<DirectoryReadSucceeded>(outcome).Listing;
        Assert.HasCount(2, listing.Entries);
        Assert.AreSame(DirectoryListingCompleteness.Bounded, listing.Completeness);
    }

    /// <summary>Proves a directory with exactly the boundary count is complete.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenEntryCountEqualsBoundaryReturnsCompleteListing()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateFile("one.txt");
        _ = root.CreateFile("two.txt");
        WindowsLocalDirectoryReader reader = new();

        DirectoryReadOutcome outcome = await reader.ReadAsync(Request(root.Path, 2), CancellationToken.None);

        DirectoryListing listing = Assert.IsInstanceOfType<DirectoryReadSucceeded>(outcome).Listing;
        Assert.HasCount(2, listing.Entries);
        Assert.AreSame(DirectoryListingCompleteness.Complete, listing.Completeness);
    }

    /// <summary>Proves a listing is a frozen snapshot and a later read observes change.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-011")]
    public async Task ReadAsyncWhenDirectoryChangesAfterReadListingStaysFrozenUntilNextRead()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateFile("first.txt");
        WindowsLocalDirectoryReader reader = new();

        DirectoryListing before = Assert.IsInstanceOfType<DirectoryReadSucceeded>(
            await reader.ReadAsync(Request(root.Path, 8), CancellationToken.None)).Listing;
        _ = root.CreateFile("second.txt");
        DirectoryListing after = Assert.IsInstanceOfType<DirectoryReadSucceeded>(
            await reader.ReadAsync(Request(root.Path, 8), CancellationToken.None)).Listing;

        Assert.HasCount(1, before.Entries);
        Assert.HasCount(2, after.Entries);
    }

    /// <summary>Proves cancellation observed before enumeration touches no directory.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenAlreadyCancelledReturnsCancelledWithoutAccessingDirectory()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        FileSystemPath missing = ParsePath(root.Resolve("missing"));
        WindowsLocalDirectoryReader reader = new();

        DirectoryReadOutcome outcome = await reader.ReadAsync(Request(missing, 8), cancellation.Token);

        _ = Assert.IsInstanceOfType<DirectoryReadCancelled>(outcome);
    }

    /// <summary>Proves a vanished location is a normalized not-found outcome.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-017")]
    public async Task ReadAsyncWhenDirectoryDoesNotExistReturnsNotFound()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileSystemPath missing = ParsePath(root.Resolve("missing"));
        WindowsLocalDirectoryReader reader = new();

        DirectoryReadOutcome outcome = await reader.ReadAsync(Request(missing, 8), CancellationToken.None);

        Assert.AreSame(
            FileOperationFailureKind.NotFound,
            Assert.IsInstanceOfType<DirectoryReadFailed>(outcome).Failure);
    }

    /// <summary>Proves a file cannot be read as a directory.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenLocationIsFileReturnsNotFound()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileSystemPath file = ParsePath(root.CreateFile("file.txt"));
        WindowsLocalDirectoryReader reader = new();

        DirectoryReadOutcome outcome = await reader.ReadAsync(Request(file, 8), CancellationToken.None);

        Assert.AreSame(
            FileOperationFailureKind.NotFound,
            Assert.IsInstanceOfType<DirectoryReadFailed>(outcome).Failure);
    }

    /// <summary>Proves a denied directory fails closed instead of appearing empty.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-017")]
    public async Task ReadAsyncWhenListingIsDeniedReturnsAccessDeniedWithoutEntries()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileSystemPath denied = ParsePath(root.DenyDirectoryListing("denied"));
        WindowsLocalDirectoryReader reader = new();

        DirectoryReadOutcome outcome = await reader.ReadAsync(Request(denied, 8), CancellationToken.None);

        Assert.AreSame(
            FileOperationFailureKind.AccessDenied,
            Assert.IsInstanceOfType<DirectoryReadFailed>(outcome).Failure);
    }

    /// <summary>Proves the adapter serves only its own provider and never guesses from text.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenLocationIsNotWindowsLocalReturnsProviderUnavailable()
    {
        WindowsLocalDirectoryReader reader = new();

        DirectoryReadOutcome unc = await reader.ReadAsync(
            Request(ParsePath("\\\\server\\share\\root"), 8),
            CancellationToken.None);
        DirectoryReadOutcome wsl = await reader.ReadAsync(
            Request(ParsePath("\\\\wsl.localhost\\Ubuntu\\home"), 8),
            CancellationToken.None);

        Assert.AreSame(
            FileOperationFailureKind.ProviderUnavailable,
            Assert.IsInstanceOfType<DirectoryReadFailed>(unc).Failure);
        Assert.AreSame(
            FileOperationFailureKind.ProviderUnavailable,
            Assert.IsInstanceOfType<DirectoryReadFailed>(wsl).Failure);
    }

    /// <summary>Proves an entry the path model rejects is counted, not shown or silently dropped.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-015")]
    public async Task ReadAsyncWhenEntryNameCannotBeRepresentedCountsItWithoutFailing()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        root.CreateFileWithUnrepresentableName("trailing.");
        _ = root.CreateFile("normal.txt");
        WindowsLocalDirectoryReader reader = new();

        DirectoryReadOutcome outcome = await reader.ReadAsync(Request(root.Path, 8), CancellationToken.None);

        DirectoryListing listing = Assert.IsInstanceOfType<DirectoryReadSucceeded>(outcome).Listing;
        Assert.HasCount(1, listing.Entries);
        Assert.AreEqual("normal.txt", listing.Entries[0].Name);
        Assert.AreEqual(1, listing.UnrepresentableEntryCount);
    }

    /// <summary>Proves unrepresentable entries still count toward the enumeration boundary.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-011")]
    public async Task ReadAsyncWhenUnrepresentableEntriesReachBoundaryStopsEnumerating()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        root.CreateFileWithUnrepresentableName("one.");
        root.CreateFileWithUnrepresentableName("two.");
        root.CreateFileWithUnrepresentableName("three.");
        WindowsLocalDirectoryReader reader = new();

        DirectoryReadOutcome outcome = await reader.ReadAsync(Request(root.Path, 2), CancellationToken.None);

        DirectoryListing listing = Assert.IsInstanceOfType<DirectoryReadSucceeded>(outcome).Listing;
        Assert.IsEmpty(listing.Entries);
        Assert.AreEqual(2, listing.UnrepresentableEntryCount);
        Assert.AreSame(DirectoryListingCompleteness.Bounded, listing.Completeness);
    }

    /// <summary>Proves a listing the application rejects is a fail-closed provider outcome.</summary>
    [TestMethod]
    public void TranslateListingCreationWhenListingIsRejectedReturnsProviderUnavailable()
    {
        FileSystemPath location = ParsePath("C:\\same");
        DirectoryEntry first = DirectoryEntry.Create(
            ParsePath("C:\\same\\Same"),
            "Same",
            DirectoryEntryKind.File,
            EntryVisibility.Normal);
        DirectoryEntry second = DirectoryEntry.Create(
            ParsePath("c:\\same\\same"),
            "same",
            DirectoryEntryKind.File,
            EntryVisibility.Normal);
        DirectoryListingCreation rejected = DirectoryListing.Create(
            location,
            [first, second],
            DirectoryListingCompleteness.Complete,
            0);

        DirectoryReadOutcome outcome = WindowsLocalDirectoryReader.TranslateListingCreation(rejected);

        Assert.AreSame(
            FileOperationFailureKind.ProviderUnavailable,
            Assert.IsInstanceOfType<DirectoryReadFailed>(outcome).Failure);
    }

    /// <summary>Proves only the non-directory HRESULT is special-cased before canonical normalization.</summary>
    [TestMethod]
    public void NormalizeEnumerationFailureWhenHResultVariesSpecialCasesOnlyNonDirectoryHandles()
    {
        Assert.AreSame(
            FileOperationFailureKind.NotFound,
            WindowsLocalDirectoryReader.NormalizeEnumerationFailure(unchecked((int)0x80070057)));
        Assert.AreSame(
            FileOperationFailureKind.AccessDenied,
            WindowsLocalDirectoryReader.NormalizeEnumerationFailure(unchecked((int)0x80070005)));
        Assert.AreSame(
            FileOperationFailureKind.ProviderUnavailable,
            WindowsLocalDirectoryReader.NormalizeEnumerationFailure(unchecked((int)0x80070035)));
    }

    /// <summary>Proves child text never doubles the separator after a drive root.</summary>
    [TestMethod]
    public void BuildChildTextWhenLocationIsDriveRootOrDirectoryUsesOneSeparator()
    {
        WindowsLocalPath driveRoot = Assert.IsInstanceOfType<WindowsLocalPath>(ParsePath("C:\\"));
        WindowsLocalPath directory = Assert.IsInstanceOfType<WindowsLocalPath>(ParsePath("C:\\dir"));

        Assert.AreEqual("C:\\child", WindowsLocalDirectoryReader.BuildChildText(driveRoot, "child"));
        Assert.AreEqual("C:\\dir\\child", WindowsLocalDirectoryReader.BuildChildText(directory, "child"));
    }

    /// <summary>Proves the adapter rejects an absent request before any provider access.</summary>
    [TestMethod]
    public void ReadAsyncWhenRequestIsNullThrowsArgumentNullException()
    {
        WindowsLocalDirectoryReader reader = new();
        MethodInfo method = typeof(WindowsLocalDirectoryReader).GetMethod(
            nameof(WindowsLocalDirectoryReader.ReadAsync),
            BindingFlags.Public | BindingFlags.Instance) ??
            throw new AssertFailedException("The read method was not found.");

        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => method.Invoke(reader, [null, CancellationToken.None]));

        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }

    private static EntryVisibility VisibilityOf(DirectoryListing listing, string name)
    {
        DirectoryEntry entry = listing.Entries.Single(candidate => candidate.Name == name);
        return entry.Visibility;
    }

    private static DirectoryReadRequest Request(FileSystemPath location, int entryBoundary)
    {
        DirectoryReadRequestCreation creation = DirectoryReadRequest.Create(location, entryBoundary);
        return Assert.IsInstanceOfType<DirectoryReadRequestAccepted>(creation).Request;
    }

    private static FileSystemPath ParsePath(string input)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input)).Path;
    }
}
