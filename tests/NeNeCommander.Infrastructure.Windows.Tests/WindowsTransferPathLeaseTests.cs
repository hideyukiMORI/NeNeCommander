using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves exact existing-entry ancestry, failed-open release and the explicit limits of sharing protection.</summary>
[TestClass]
public sealed class WindowsTransferPathLeaseTests
{
    /// <summary>Proves ordinary file handles retain the canonical object and remain valid across unrelated sibling creation.</summary>
    [TestMethod]
    [DataRow(FileAccess.Read)]
    [DataRow(FileAccess.ReadWrite)]
    public void OpenWhenFileIsStableRetainsExactObjectAndAncestors(FileAccess access)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("parent");
        WindowsLocalPath path = Parse(root.WriteFile("parent/file.txt", "original"));
        string expected = WindowsFileIdentifier.Describe(path.CanonicalText);
        using WindowsTransferPathLease lease = WindowsTransferPathLease.Open(path, access, new WindowsDriveBoundary());

        Assert.AreEqual(expected, WindowsFileIdentifier.DescribeHandle(lease.Handle));
        _ = root.WriteFile("parent/sibling.txt", "sibling");
        lease.Revalidate();
        Assert.AreEqual("original", ReadSharingWriter(path.CanonicalText));
    }

    /// <summary>Proves an original file or ancestor cannot be renamed, and a data writer cannot open while the lease lives.</summary>
    [TestMethod]
    [DataRow(FileAccess.Read)]
    [DataRow(FileAccess.ReadWrite)]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void OpenWhenLeaseLivesExcludesDataWriterAndNameReplacement(FileAccess access)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("parent");
        WindowsLocalPath path = Parse(root.WriteFile("parent/file.txt", "original"));
        using WindowsTransferPathLease lease = WindowsTransferPathLease.Open(path, access, new WindowsDriveBoundary());

        _ = Assert.Throws<IOException>(() =>
        {
            using SafeFileHandle writer = File.OpenHandle(path.CanonicalText, FileMode.Open, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
        });
        _ = Assert.Throws<IOException>(() => File.Move(path.CanonicalText, root.Resolve("parent/moved.txt")));
        Assert.IsFalse(RenameDirectory(root.Resolve("parent"), root.Resolve("renamed")));
        lease.Revalidate();
        Assert.AreEqual("original", ReadSharingWriter(path.CanonicalText));
    }

    /// <summary>Proves both intermediate and final junction entries refuse without touching their real target.</summary>
    [TestMethod]
    [DataRow("ancestor")]
    [DataRow("leaf")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void OpenWhenPathContainsJunctionRefusesIt(string position)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("target");
        string target = root.WriteFile("target/file.txt", "foreign");
        string link = root.CreateJunction("link", "target");
        WindowsLocalPath path = Parse(position == "ancestor" ? System.IO.Path.Join(link, "file.txt") : link);

        _ = Assert.Throws<IOException>(() =>
        {
            using WindowsTransferPathLease lease = WindowsTransferPathLease.Open(path, FileAccess.ReadWrite, new WindowsDriveBoundary());
        });
        Assert.AreEqual("foreign", File.ReadAllText(target));
        Assert.IsTrue(RenameDirectory(root.Resolve("target"), root.Resolve("target-renamed")));
        Assert.IsTrue(RenameDirectory(root.Resolve("target-renamed"), root.Resolve("target")));
    }

    /// <summary>Proves a missing intermediate or leaf releases all already opened ancestors.</summary>
    [TestMethod]
    [DataRow("missing/file.txt")]
    [DataRow("file.txt")]
    public void OpenWhenDescendantIsMissingReleasesAncestors(string suffix)
    {
        ArgumentNullException.ThrowIfNull(suffix);
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("parent");
        WindowsLocalPath path = Parse(root.Resolve("parent/" + suffix));

        _ = Assert.Throws<IOException>(() =>
        {
            using WindowsTransferPathLease lease = WindowsTransferPathLease.Open(path, FileAccess.Read, new WindowsDriveBoundary());
        });
        Assert.IsTrue(RenameDirectory(root.Resolve("parent"), root.Resolve("renamed")));
    }

    /// <summary>Proves a directory in the file position refuses and releases the chain.</summary>
    [TestMethod]
    public void OpenWhenLeafIsDirectoryRefusesAndReleasesIt()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath path = Parse(root.CreateDirectory("directory"));
        _ = Assert.Throws<IOException>(() =>
        {
            using WindowsTransferPathLease lease = WindowsTransferPathLease.Open(path, FileAccess.Read, new WindowsDriveBoundary());
        });
        Assert.IsTrue(RenameDirectory(path.CanonicalText, root.Resolve("renamed")));
    }

    /// <summary>Proves prior data writers prevent admission and no partial lease survives.</summary>
    [TestMethod]
    public void OpenWhenWriterAlreadyExistsRefusesAndReleasesAncestors()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("parent");
        WindowsLocalPath path = Parse(root.WriteFile("parent/file.txt", "original"));
        using (SafeFileHandle writer = File.OpenHandle(path.CanonicalText, FileMode.Open, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete))
        {
            Assert.IsFalse(writer.IsInvalid);
            _ = Assert.Throws<IOException>(() =>
            {
                using WindowsTransferPathLease lease = WindowsTransferPathLease.Open(path, FileAccess.Read, new WindowsDriveBoundary());
            });
        }
        Assert.IsTrue(RenameDirectory(root.Resolve("parent"), root.Resolve("renamed")));
    }

    /// <summary>Proves attribute-only timestamp changes remain possible and invalidate the original full identity.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void RevalidateWhenMetadataChangesDoesNotResetBaseline()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath path = Parse(root.WriteFile("file.txt", "original"));
        using WindowsTransferPathLease lease = WindowsTransferPathLease.Open(path, FileAccess.Read, new WindowsDriveBoundary());
        root.SetLastWriteTimeUtc("file.txt", new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc));
        _ = Assert.Throws<IOException>(lease.Revalidate);
        _ = Assert.Throws<IOException>(lease.Revalidate);
        Assert.AreEqual("original", ReadSharingWriter(path.CanonicalText));
    }

    /// <summary>Proves disposal is idempotent and releases both leaf and ancestor sharing exclusions.</summary>
    [TestMethod]
    public void DisposeWhenCalledTwiceReleasesAllOwnedHandles()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("parent");
        WindowsLocalPath path = Parse(root.WriteFile("parent/file.txt", "original"));
        using WindowsTransferPathLease lease = WindowsTransferPathLease.Open(path, FileAccess.Read, new WindowsDriveBoundary());
        lease.Dispose();
        lease.Dispose();
        _ = Assert.Throws<ObjectDisposedException>(lease.Revalidate);
        File.WriteAllText(path.CanonicalText, "after");
        Assert.IsTrue(RenameDirectory(root.Resolve("parent"), root.Resolve("renamed")));
        Assert.AreEqual("after", File.ReadAllText(root.Resolve("renamed/file.txt")));
    }

    /// <summary>Proves roots and unsupported file access do not enter native traversal.</summary>
    [TestMethod]
    [DataRow("root")]
    [DataRow("write")]
    [DataRow("invalid")]
    public void OpenWhenRequestShapeIsInvalidRefusesIt(string defect)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath path = defect == "root" ? Parse(root.Path.Drive + "\\") : Parse(root.WriteFile("file.txt", "original"));
        FileAccess access = defect switch
        {
            "write" => FileAccess.Write,
            "invalid" => (FileAccess)42,
            _ => FileAccess.Read
        };
        _ = Assert.Throws<ArgumentException>(() =>
        {
            using WindowsTransferPathLease lease = WindowsTransferPathLease.Open(path, access, new WindowsDriveBoundary());
        });
    }

    /// <summary>Proves drive reclassification is rejected both initially and when an existing lease revalidates.</summary>
    [TestMethod]
    [DataRow("initial")]
    [DataRow("later")]
    public void OpenAndRevalidateWhenDriveBecomesRemoteRefuseIt(string timing)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath path = Parse(root.WriteFile("file.txt", "original"));
        DriveType type = timing == "initial" ? DriveType.Network : DriveType.Fixed;
        WindowsDriveBoundary boundary = new(_ => type);
        if (timing == "initial")
        {
            _ = Assert.Throws<WindowsDriveBoundaryRejectedException>(() =>
            {
                using WindowsTransferPathLease refused = WindowsTransferPathLease.Open(path, FileAccess.Read, boundary);
            });
            return;
        }
        using WindowsTransferPathLease lease = WindowsTransferPathLease.Open(path, FileAccess.Read, boundary);
        type = DriveType.Network;
        _ = Assert.Throws<WindowsDriveBoundaryRejectedException>(lease.Revalidate);
        Assert.AreEqual("original", ReadSharingWriter(path.CanonicalText));
    }

    /// <summary>Proves one-component traversal and final observation support a validated path beyond legacy MAX_PATH.</summary>
    [TestMethod]
    public void OpenWhenPathExceedsLegacyLengthRetainsExactFile()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string relative = new string('a', 100) + "/" + new string('b', 100);
        _ = root.CreateDirectory(relative);
        WindowsLocalPath path = Parse(root.WriteFile(relative + "/file.txt", "original"));
        Assert.IsGreaterThan(260, path.CanonicalText.Length);
        using WindowsTransferPathLease lease = WindowsTransferPathLease.Open(path, FileAccess.Read, new WindowsDriveBoundary());
        lease.Revalidate();
        Assert.AreEqual(8L, WindowsFileIdentifier.ReadLocalFacts(lease.Handle).Standard.EndOfFile);
    }

    private static string ReadSharingWriter(string path)
    {
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    private static bool RenameDirectory(string source, string destination)
    {
        try
        {
            Directory.Move(source, destination);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static WindowsLocalPath Parse(string text)
    {
        return Assert.IsInstanceOfType<WindowsLocalPath>(Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path);
    }
}
