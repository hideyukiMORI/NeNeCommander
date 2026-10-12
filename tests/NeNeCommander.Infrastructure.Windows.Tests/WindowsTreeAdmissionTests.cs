using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves destination admission independently of the host directory's case behavior.</summary>
[TestClass]
public sealed class WindowsTreeAdmissionTests
{
    /// <summary>Proves a provider enumeration with distinct case-sensitive names cannot merge on Windows.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-022")]
    [DataRow("file", "file")]
    [DataRow("directory", "directory")]
    [DataRow("file", "directory")]
    public void ChildEnumerationWhenNamesCollideRejectsPreflightCopyAndVerification(string firstKind, string secondKind)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("one");
        _ = root.CreateDirectory("two");
        FileSystemInfo first = CreateEntry(root, "one\\Case", firstKind);
        FileSystemInfo second = CreateEntry(root, "two\\case", secondKind);
        FileSystemInfo[] enumeration = [first, second];
        FileSystemPath target = Parse(root.CreateDirectory("target"));

        FileOperationFailureKind? preflight = WindowsLocalTreeCopy.PreflightChildren(
            enumeration, target, path => path.CanonicalText, WindowsLocalTreeCopy.InspectLocalSource);
        ProviderStepOutcome copy = WindowsLocalTreeCopy.CopyChildren(
            enumeration, target, path => path.CanonicalText, WindowsLocalTreeCopy.InspectLocalSource);
        bool matches = WindowsLocalTreeCopy.MatchesChildren(
            enumeration, target, path => path.CanonicalText, WindowsLocalTreeCopy.InspectLocalSource);

        Assert.AreSame(FileOperationFailureKind.Conflict, preflight);
        Assert.AreSame(FileOperationFailureKind.Conflict, copy.Failure);
        Assert.AreSame(ProviderStepEffectKind.CopyTargetCreated, copy.Effect);
        Assert.IsFalse(matches);
        Assert.HasCount(1, Directory.GetFileSystemEntries(root.Resolve("target")));
        Assert.IsTrue(first.Exists);
        Assert.IsTrue(second.Exists);
    }

    /// <summary>Proves a directory appearing after the existence check is never reused by native creation.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-022")]
    [DataRow("directory")]
    [DataRow("file")]
    [DataRow("missing-parent")]
    public void CopyWhenDestinationChangesBeforeCreateReportsNoOwnedEffect(string change)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        DirectoryInfo source = new(root.CreateDirectory("source"));
        _ = root.WriteFile("source\\payload.txt", "original");
        _ = root.CreateDirectory("dest");
        FileSystemPath target = Parse(root.Resolve("dest\\source"));
        int resolutions = 0;
        string Resolve(FileSystemPath path)
        {
            if (FileSystemPathIdentityComparer.Instance.Equals(path, target) && ++resolutions == 2)
            {
                ChangeDestination(root, change);
            }
            return path.CanonicalText;
        }

        ProviderStepOutcome outcome = WindowsLocalTreeCopy.Copy(
            source, target, Resolve, WindowsLocalTreeCopy.InspectLocalSource);

        Assert.AreSame(change == "missing-parent" ? FileOperationFailureKind.NotFound : FileOperationFailureKind.Conflict,
            outcome.Failure);
        Assert.IsNull(outcome.Effect);
        Assert.IsFalse(File.Exists(root.Resolve("dest\\source\\payload.txt")));
        Assert.AreEqual("original", File.ReadAllText(root.Resolve("source\\payload.txt")));
    }

    /// <summary>Proves a competing file or directory is not reported as a target created by this copy.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-022")]
    [DataRow("file")]
    [DataRow("directory")]
    public void CopyFileWhenTargetAppearsBeforeExclusiveCreateReportsNoOwnedEffect(string kind)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileInfo source = new(root.WriteFile("source", "original"));
        _ = root.CreateDirectory("dest");
        FileSystemPath target = Parse(root.Resolve("dest\\source"));
        int resolutions = 0;
        string Resolve(FileSystemPath path)
        {
            if (FileSystemPathIdentityComparer.Instance.Equals(path, target) && ++resolutions == 2)
            {
                ChangeDestination(root, kind);
            }
            return path.CanonicalText;
        }

        ProviderStepOutcome outcome = WindowsLocalTreeCopy.Copy(
            source, target, Resolve, WindowsLocalTreeCopy.InspectLocalSource);

        Assert.AreSame(FileOperationFailureKind.Conflict, outcome.Failure);
        Assert.IsNull(outcome.Effect);
        Assert.AreEqual("original", File.ReadAllText(source.FullName));
        if (kind == "file")
        {
            Assert.AreEqual("existing", File.ReadAllText(target.CanonicalText));
        }
        else
        {
            Assert.HasCount(0, Directory.GetFileSystemEntries(target.CanonicalText));
        }
    }

    /// <summary>Proves exclusive native directory creation preserves support for long Windows paths.</summary>
    [TestMethod]
    public void CopyWhenLocalTargetExceedsLegacyPathLimitCreatesEveryDirectory()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string nested = new string('a', 150) + "\\" + new string('b', 150);
        DirectoryInfo source = new(root.CreateDirectory("source"));
        _ = root.CreateDirectory("source\\" + nested);
        _ = root.WriteFile("source\\" + nested + "\\payload.txt", "original");
        FileSystemPath target = Parse(root.Resolve("copied"));

        ProviderStepOutcome outcome = WindowsLocalTreeCopy.Copy(
            source, target, path => path.CanonicalText, WindowsLocalTreeCopy.InspectLocalSource);

        Assert.IsNull(outcome.Failure);
        Assert.IsTrue(WindowsLocalTreeCopy.Matches(
            source, target, path => path.CanonicalText, WindowsLocalTreeCopy.InspectLocalSource));
        Assert.AreEqual("original", File.ReadAllText(root.Resolve("copied\\" + nested + "\\payload.txt")));
    }

    /// <summary>Proves the exact Windows component boundary and canonical total path boundary.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-015")]
    public void DeriveChildWhenWindowsNameOrTotalPathIsTooLongHasNoAdmissibleTarget()
    {
        FileSystemPath parent = Parse("C:\\dest");
        Assert.IsNotNull(WindowsLocalTreeCopy.DeriveChild(parent, new string('a', 255)));
        Assert.IsNull(WindowsLocalTreeCopy.DeriveChild(parent, new string('a', 256)));
        FileSystemPath maximum = Parse("C:\\" + new string('a', 32764));
        Assert.IsNull(WindowsLocalTreeCopy.DeriveChild(maximum, "b"));
        Assert.IsNull(WindowsLocalTreeCopy.DeriveChild(parent, "CON"));
        Assert.IsNull(WindowsLocalTreeCopy.DeriveChild(parent, "trailing."));
        Assert.IsNull(WindowsLocalTreeCopy.DeriveChild(parent, "trailing "));
        Assert.IsNotNull(WindowsLocalTreeCopy.DeriveChild(parent, "a\uF03Ab"));
    }

    private static FileSystemInfo CreateEntry(TestOwnedTemporaryRoot root, string name, string kind)
    {
        return kind == "directory" ? new DirectoryInfo(root.CreateDirectory(name))
            : new FileInfo(root.WriteFile(name, "original"));
    }

    private static void ChangeDestination(TestOwnedTemporaryRoot root, string change)
    {
        if (change == "directory")
        {
            _ = root.CreateDirectory("dest\\source");
        }
        else if (change == "file")
        {
            _ = root.WriteFile("dest\\source", "existing");
        }
        else
        {
            Directory.Delete(root.Resolve("dest"));
        }
    }

    private static FileSystemPath Parse(string path)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(path)).Path;
    }
}
