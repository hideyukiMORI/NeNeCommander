using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves late collisions, denied creation and name limits at the actual Windows boundary.</summary>
[TestClass]
public sealed class WindowsEmptyFileBoundaryTests
{
    /// <summary>Proves a collision inserted after the existence check is refused by exclusive creation.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    [DataRow("file")]
    [DataRow("directory")]
    [DataRow("dangling-link")]
    public void ExecuteWhenTargetAppearsAtFinalRecheckPreservesCollision(string kind)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string parent = root.CreateDirectory("parent");
        FileEntrySnapshot location = WindowsLocalOperationTestContext.LocalSnapshot(parent);
        FileSystemPath target = WindowsLocalOperationTestContext.ParsePath(root.Resolve("parent/new.txt"));
        DateTime originalWriteTime = Directory.GetLastWriteTimeUtc(parent);
        int parentQueries = 0;
        WindowsDriveBoundary boundary = new(path =>
        {
            if (path.CanonicalText.Equals(parent, StringComparison.OrdinalIgnoreCase) && ++parentQueries == 2)
            {
                CreateCollision(root, kind);
                Directory.SetLastWriteTimeUtc(parent, originalWriteTime);
                Assert.AreEqual(location.Identity, WindowsLocalEntryIdentity.Describe(new DirectoryInfo(parent)));
            }
            return DriveType.Fixed;
        });

        ProviderStepOutcome outcome = WindowsLocalEmptyFileCreation.Execute(location, target, boundary);

        Assert.AreEqual(2, parentQueries);
        Assert.AreSame(FileOperationFailureKind.Conflict, outcome.Failure);
        Assert.IsNull(outcome.Effect);
        AssertPreservedCollision(root, kind);
    }

    /// <summary>Proves missing write permission remains AccessDenied when no target exists.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-017")]
    public void CreateWhenParentDeniesFileCreationReportsAccessDeniedWithoutEffect()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        DirectoryInfo directory = new(root.CreateDirectory("denied"));
        DirectorySecurity original = directory.GetAccessControl();
        DirectorySecurity denied = directory.GetAccessControl();
        SecurityIdentifier user = WindowsIdentity.GetCurrent().User ?? throw new InvalidOperationException("Missing test user.");
        denied.AddAccessRule(new FileSystemAccessRule(user, FileSystemRights.CreateFiles, AccessControlType.Deny));
        directory.SetAccessControl(denied);
        ProviderStepOutcome outcome;
        try
        {
            outcome = WindowsEmptyFileCreation.Create(root.ResolveVerbatim("denied\\new.txt"));
        }
        finally
        {
            directory.SetAccessControl(original);
        }

        Assert.AreSame(FileOperationFailureKind.AccessDenied, outcome.Failure);
        Assert.IsNull(outcome.Effect);
        Assert.IsFalse(File.Exists(root.Resolve("denied/new.txt")));
    }

    /// <summary>Proves the local provider uses extended-length text for a valid255-character filename.</summary>
    [TestMethod]
    public void ExecuteWhenFilenameIs255CharactersBeneathLongParentsCreatesExactEmptyFile()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string relative = new string('a', 120) + "\\" + new string('b', 120);
        _ = root.CreateVerbatimDirectory(relative);
        FileEntrySnapshot location = WindowsLocalOperationTestContext.LocalSnapshot(root.Resolve(relative));
        string targetText = root.Resolve(relative + "\\" + new string('n', 255));

        ProviderStepOutcome outcome = WindowsLocalEmptyFileCreation.Execute(location,
            WindowsLocalOperationTestContext.ParsePath(targetText), new WindowsDriveBoundary());

        Assert.IsNull(outcome.Failure);
        Assert.IsTrue(targetText.Length > 260);
        Assert.AreEqual(0L, new FileInfo(@"\\?\" + targetText).Length);
    }

    /// <summary>Proves an unrepresentable256-character filename fails without creating a shorter sibling.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-015")]
    public void CreateWhenFilenameIs256CharactersRefusesWithoutAnyEntry()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string parent = root.CreateDirectory("parent");

        ProviderStepOutcome outcome = WindowsEmptyFileCreation.Create(root.ResolveVerbatim("parent\\" + new string('n', 256)));

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
        Assert.IsNull(outcome.Effect);
        Assert.IsEmpty(Directory.GetFileSystemEntries(parent));
    }

    private static void CreateCollision(TestOwnedTemporaryRoot root, string kind)
    {
        if (kind == "file")
        {
            _ = root.WriteFile("parent/new.txt", "preserve");
        }
        else if (kind == "directory")
        {
            _ = root.CreateDirectory("parent/new.txt");
            _ = root.WriteFile("parent/new.txt/sentinel", "preserve");
        }
        else
        {
            _ = root.CreateFileSymbolicLink("parent/new.txt", "parent/absent.txt");
        }
    }

    private static void AssertPreservedCollision(TestOwnedTemporaryRoot root, string kind)
    {
        if (kind == "dangling-link")
        {
            Assert.IsFalse(File.Exists(root.Resolve("parent/absent.txt")));
            Assert.IsTrue((File.GetAttributes(root.Resolve("parent/new.txt")) & FileAttributes.ReparsePoint) != 0);
        }
        else
        {
            string path = kind == "file" ? "parent/new.txt" : "parent/new.txt/sentinel";
            Assert.AreEqual("preserve", File.ReadAllText(root.Resolve(path)));
        }
    }
}
