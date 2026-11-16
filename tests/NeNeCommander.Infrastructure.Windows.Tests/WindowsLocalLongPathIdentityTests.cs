using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>
/// Proves a Windows local entry whose full path exceeds the legacy 260-character limit is described
/// and revalidated like any other entry, whether or not the host enables long paths.
/// </summary>
[TestClass]
public sealed class WindowsLocalLongPathIdentityTests
{
    /// <summary>Proves an existing file beyond the legacy limit has an identity that revalidates.</summary>
    [TestMethod]
    public void DescribeWhenFilePathExceedsLegacyLimitReturnsIdentityThatRevalidates()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory(LongDirectory());
        string full = root.WriteFile(LongDirectory() + "\\payload.txt", "original");
        Assert.IsGreaterThan(260, full.Length);

        FileIdentity identity = WindowsLocalEntryIdentity.Describe(new FileInfo(full));

        FileEntrySnapshot snapshot = FileEntrySnapshot.Create(Parse(full), identity, DeletionCapability.PermanentOnly);
        EntryMatched matched = Assert.IsInstanceOfType<EntryMatched>(WindowsLocalEntryIdentity.Revalidate(snapshot));
        Assert.AreEqual(full, matched.Entry.FullName);
    }

    /// <summary>Proves an existing directory beyond the legacy limit has an identity that revalidates.</summary>
    [TestMethod]
    public void DescribeWhenDirectoryPathExceedsLegacyLimitReturnsIdentityThatRevalidates()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string full = root.CreateDirectory(LongDirectory());
        Assert.IsGreaterThan(260, full.Length);

        FileIdentity identity = WindowsLocalEntryIdentity.Describe(new DirectoryInfo(full));

        FileEntrySnapshot snapshot = FileEntrySnapshot.Create(Parse(full), identity, DeletionCapability.PermanentOnly);
        _ = Assert.IsInstanceOfType<EntryMatched>(WindowsLocalEntryIdentity.Revalidate(snapshot));
    }

    /// <summary>Proves a rewritten file beyond the legacy limit no longer matches its earlier identity.</summary>
    [TestMethod]
    public void RevalidateWhenLongPathFileIsRewrittenRejectsTheEarlierIdentity()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory(LongDirectory());
        string full = root.WriteFile(LongDirectory() + "\\payload.txt", "original");
        FileEntrySnapshot snapshot = FileEntrySnapshot.Create(
            Parse(full),
            WindowsLocalEntryIdentity.Describe(new FileInfo(full)),
            DeletionCapability.PermanentOnly);

        File.WriteAllText(full, "rewritten with another length");

        EntryRejected rejected = Assert.IsInstanceOfType<EntryRejected>(WindowsLocalEntryIdentity.Revalidate(snapshot));
        Assert.AreSame(FileOperationFailureKind.IdentityChanged, rejected.Failure);
    }

    /// <summary>
    /// Proves an entry given in the extended-length form describes the same identity as the same
    /// entry given by its ordinary path, so the form of the path is not part of the identity.
    /// </summary>
    [TestMethod]
    public void DescribeWhenPathIsAlreadyExtendedLengthReturnsTheSameIdentity()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string full = root.WriteFile("payload.txt", "original");

        FileIdentity ordinary = WindowsLocalEntryIdentity.Describe(new FileInfo(full));
        FileIdentity extended = WindowsLocalEntryIdentity.Describe(new FileInfo(@"\\?\" + full));

        Assert.AreEqual(ordinary, extended);
    }

    private static string LongDirectory()
    {
        return new string('a', 150) + "\\" + new string('b', 150);
    }

    private static FileSystemPath Parse(string path)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(path)).Path;
    }
}
