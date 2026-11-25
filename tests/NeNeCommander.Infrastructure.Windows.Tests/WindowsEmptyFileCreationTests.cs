using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves exclusive local file creation and preserved collision targets.</summary>
[TestClass]
public sealed class WindowsEmptyFileCreationTests
{
    /// <summary>Proves creation makes exactly zero bytes and never invokes overwrite.</summary>
    [TestMethod]
    public async Task CreateEmptyFileAsyncWhenTargetIsAbsentCreatesZeroBytesAndSecondAttemptConflictsAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));
        FileSystemPath parent = WindowsLocalOperationTestContext.ParsePath(root.CreateDirectory("parent"));
        FileEntrySnapshot location = await WindowsLocalOperationTestContext.InspectAsync(adapter, parent);
        FileSystemPath target = WindowsLocalOperationTestContext.ParsePath(root.Resolve("parent\\notes.txt"));

        ProviderStepOutcome created = await adapter.CreateEmptyFileAsync(location, target, CancellationToken.None);
        FileEntrySnapshot recaptured = await WindowsLocalOperationTestContext.InspectAsync(adapter, parent);
        ProviderStepOutcome conflict = await adapter.CreateEmptyFileAsync(recaptured, target, CancellationToken.None);

        Assert.IsNull(created.Failure);
        Assert.AreEqual(0L, new FileInfo(target.CanonicalText).Length);
        Assert.AreSame(FileOperationFailureKind.Conflict, conflict.Failure);
        Assert.IsEmpty(conflict.Receipt.Effects);
    }

    /// <summary>Proves ordinary, hidden, readonly and hard-linked files retain their exact bytes.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    [DataRow("ordinary")]
    [DataRow("hidden")]
    [DataRow("readonly")]
    [DataRow("hardlink")]
    public async Task CreateEmptyFileAsyncWhenFileExistsPreservesItsBytesAsync(string kind)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string targetText = root.WriteFile("notes.txt", "preserved sentinel");
        if (kind == "hidden")
        {
            File.SetAttributes(targetText, FileAttributes.Hidden);
        }
        if (kind == "readonly")
        {
            File.SetAttributes(targetText, FileAttributes.ReadOnly);
        }
        if (kind == "hardlink")
        {
            targetText = root.CreateHardLink("alias.txt", "notes.txt");
        }
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));
        FileEntrySnapshot location = await WindowsLocalOperationTestContext.InspectAsync(adapter, root.Path);

        FileAttributes attributes = File.GetAttributes(targetText);
        try
        {
            ProviderStepOutcome outcome = await adapter.CreateEmptyFileAsync(location, WindowsLocalOperationTestContext.ParsePath(targetText), CancellationToken.None);

            Assert.AreSame(FileOperationFailureKind.Conflict, outcome.Failure);
            Assert.AreEqual("preserved sentinel", File.ReadAllText(targetText));
            Assert.AreEqual(attributes, File.GetAttributes(targetText));
            Assert.IsEmpty(outcome.Receipt.Effects);
        }
        finally
        {
            File.SetAttributes(root.Resolve("notes.txt"), FileAttributes.Normal);
        }
    }

    /// <summary>Proves a dangling final link cannot create its absent target.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-003")]
    public void CreateWhenFinalLinkIsDanglingPreservesLinkAndAbsentTarget()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string link = root.CreateFileSymbolicLink("link.txt", "absent.txt");

        ProviderStepOutcome outcome = WindowsEmptyFileCreation.Create(@"\\?\" + link);

        Assert.AreSame(FileOperationFailureKind.Conflict, outcome.Failure);
        Assert.IsFalse(File.Exists(root.Resolve("absent.txt")));
        Assert.IsTrue((File.GetAttributes(link) & FileAttributes.ReparsePoint) != 0);
    }

    /// <summary>Proves a directory collision preserves its child even at the final native boundary.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void CreateWhenTargetIsDirectoryReportsConflictAndPreservesChild()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string target = root.CreateDirectory("notes.txt");
        _ = root.WriteFile("notes.txt\\sentinel", "keep");

        ProviderStepOutcome outcome = WindowsEmptyFileCreation.Create(@"\\?\" + target);

        Assert.AreSame(FileOperationFailureKind.Conflict, outcome.Failure);
        Assert.AreEqual("keep", File.ReadAllText(root.Resolve("notes.txt\\sentinel")));
    }
}
