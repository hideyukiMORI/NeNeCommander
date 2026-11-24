using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves WSL empty-file admission through the existing filesystem seam.</summary>
[TestClass]
public sealed class WslEmptyFileCreationTests
{
    /// <summary>Proves canonical WSL child names remain exact and case sensitive.</summary>
    [TestMethod]
    [DataRow("notes.txt")]
    [DataRow("NOTES.txt")]
    [DataRow("foo.")]
    [DataRow("COM1")]
    public async Task CreateEmptyFileAsyncWhenDirectChildIsAdmittedCreatesExactNameAsync(string name)
    {
        WslPath location = Location();
        ScriptedWslFileSystem fileSystem = Prepare(location);
        WslFileOperationAdapter adapter = WslOperationTestContext.Adapter(fileSystem);
        FileEntrySnapshot snapshot = Snapshot(fileSystem, location);
        FileSystemPath target = Assert.IsInstanceOfType<PathParseSuccess>(location.Child(name)).Path;

        ProviderStepOutcome outcome = await adapter.CreateEmptyFileAsync(snapshot, target, CancellationToken.None);

        Assert.IsNull(outcome.Failure);
        Assert.HasCount(1, fileSystem.CreatedFiles);
        Assert.AreSame(target, fileSystem.CreatedFiles[0]);
        Assert.IsEmpty(fileSystem.Created);
        Assert.IsEmpty(fileSystem.Deleted);
    }

    /// <summary>Proves another provider, distribution, parent or deeper descendant is refused.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-003")]
    [DataRow("C:\\new.txt", "provider")]
    [DataRow("\\\\server\\share\\new.txt", "provider")]
    [DataRow("\\\\wsl.localhost\\Debian\\tmp\\fixture\\new.txt", "parent")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\tmp\\fixture\\deeper\\new.txt", "parent")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\tmp\\new.txt", "parent")]
    public async Task CreateEmptyFileAsyncWhenTargetIsNotDirectChildRejectsWithoutEffectAsync(string targetText, string expectedFailure)
    {
        WslPath location = Location();
        ScriptedWslFileSystem fileSystem = Prepare(location);
        WslFileOperationAdapter adapter = WslOperationTestContext.Adapter(fileSystem);

        ProviderStepOutcome outcome = await adapter.CreateEmptyFileAsync(
            Snapshot(fileSystem, location), WslOperationTestContext.Path(targetText), CancellationToken.None);

        Assert.AreSame(expectedFailure == "provider" ? FileOperationFailureKind.ProviderUnavailable : FileOperationFailureKind.Inspection, outcome.Failure);
        Assert.IsEmpty(fileSystem.CreatedFiles);
    }

    /// <summary>Proves a linked or non-directory ancestor refuses the operation.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-003")]
    [DataRow("link")]
    [DataRow("file")]
    public async Task CreateEmptyFileAsyncWhenAncestorIsUnsafeRefusesWithoutEffectAsync(string kind)
    {
        WslPath location = Location();
        ScriptedWslFileSystem fileSystem = Prepare(location);
        WslPath parent = Assert.IsInstanceOfType<WslPath>(location.Parent);
        fileSystem.Set(ScriptedWslFileSystem.Entry(parent, "unsafe",
            kind == "link" ? DirectoryEntryKind.Directory : DirectoryEntryKind.File,
            kind == "link" ? FileAttributes.ReparsePoint : FileAttributes.Normal));
        WslFileOperationAdapter adapter = WslOperationTestContext.Adapter(fileSystem);

        ProviderStepOutcome outcome = await adapter.CreateEmptyFileAsync(
            Snapshot(fileSystem, location), Assert.IsInstanceOfType<PathParseSuccess>(location.Child("new.txt")).Path, CancellationToken.None);

        Assert.AreSame(kind == "link" ? FileOperationFailureKind.ProviderUnavailable : FileOperationFailureKind.NotFound, outcome.Failure);
        Assert.IsEmpty(fileSystem.CreatedFiles);
    }

    /// <summary>Proves a location replacement after target preflight is caught by final identity revalidation.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task CreateEmptyFileAsyncWhenLocationChangesAfterTargetCheckRejectsWithoutEffectAsync()
    {
        WslPath location = Location();
        ScriptedWslFileSystem fileSystem = Prepare(location);
        FileEntrySnapshot snapshot = Snapshot(fileSystem, location);
        fileSystem.ReplaceSourceWhenTargetChecked = ScriptedWslFileSystem.Entry(location, "replacement", DirectoryEntryKind.Directory);
        WslFileOperationAdapter adapter = WslOperationTestContext.Adapter(fileSystem);

        ProviderStepOutcome outcome = await adapter.CreateEmptyFileAsync(
            snapshot, Assert.IsInstanceOfType<PathParseSuccess>(location.Child("new.txt")).Path, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.IdentityChanged, outcome.Failure);
        Assert.IsEmpty(fileSystem.CreatedFiles);
    }

    /// <summary>Proves every known existing target kind is a conflict without a create or delete.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    [DataRow("file")]
    [DataRow("directory")]
    [DataRow("link")]
    public async Task CreateEmptyFileAsyncWhenTargetExistsReportsConflictAsync(string kind)
    {
        WslPath location = Location();
        ScriptedWslFileSystem fileSystem = Prepare(location);
        WslPath target = Assert.IsInstanceOfType<WslPath>(Assert.IsInstanceOfType<PathParseSuccess>(location.Child("new.txt")).Path);
        WslFileSystemEntry existing = ScriptedWslFileSystem.Entry(target, "existing",
            kind == "directory" ? DirectoryEntryKind.Directory : DirectoryEntryKind.File,
            kind == "link" ? FileAttributes.ReparsePoint : FileAttributes.Normal);
        fileSystem.Set(existing);
        WslFileOperationAdapter adapter = WslOperationTestContext.Adapter(fileSystem);

        ProviderStepOutcome outcome = await adapter.CreateEmptyFileAsync(Snapshot(fileSystem, location), target, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Conflict, outcome.Failure);
        Assert.AreSame(existing, fileSystem.Find(target));
        Assert.IsEmpty(fileSystem.CreatedFiles);
        Assert.IsEmpty(fileSystem.Deleted);
    }

    private static WslPath Location()
    {
        return WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\tmp\\fixture");
    }

    private static ScriptedWslFileSystem Prepare(WslPath location)
    {
        ScriptedWslFileSystem fileSystem = new();
        FileSystemPath? path = location;
        while (path is WslPath current)
        {
            fileSystem.Set(ScriptedWslFileSystem.Entry(current, "directory:" + current.LinuxPath, DirectoryEntryKind.Directory));
            path = current.Parent;
        }
        return fileSystem;
    }

    private static FileEntrySnapshot Snapshot(ScriptedWslFileSystem fileSystem, WslPath location)
    {
        return WslOperationTestContext.Snapshot(Assert.IsInstanceOfType<WslFileSystemEntry>(fileSystem.Find(location)));
    }
}
