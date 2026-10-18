using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Preserves the local adapter proof for NameEntry.</summary>
[TestClass]
public sealed class WindowsLocalNameEntryTests
{
    /// <summary>Proves a directory is created beneath a revalidated directory location and a second attempt is a conflict.</summary>
    [TestMethod]
    public async Task CreateDirectoryAsyncWhenLocationIsDirectoryCreatesOnceThenConflicts()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));
        FileEntrySnapshot location = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(root.CreateDirectory("parent")));
        FileSystemPath target = WindowsLocalOperationTestContext.ParsePath(root.Resolve("parent\\child"));

        ProviderStepOutcome first = await adapter.CreateDirectoryAsync(location, target, CancellationToken.None);
        FileEntrySnapshot relocated = await WindowsLocalOperationTestContext.InspectAsync(adapter, location.Path);
        ProviderStepOutcome second = await adapter.CreateDirectoryAsync(relocated, target, CancellationToken.None);

        Assert.IsNull(first.Failure);
        Assert.IsTrue(Directory.Exists(root.Resolve("parent\\child")));
        Assert.AreSame(FileOperationFailureKind.Conflict, second.Failure);
    }

    /// <summary>Proves a file location, a junction location, a foreign target, and a target outside the location each create nothing.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-003")]
    public async Task CreateDirectoryAsyncWhenLocationOrTargetIsUnsafeFailsClosedWithoutWriting()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));
        FileEntrySnapshot file = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(root.WriteFile("file.txt", "x")));
        _ = root.CreateDirectory("real");
        FileEntrySnapshot junction = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(root.CreateJunction("link", "real")));
        FileEntrySnapshot parent = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(root.CreateDirectory("parent")));

        ProviderStepOutcome underFile = await adapter.CreateDirectoryAsync(file, WindowsLocalOperationTestContext.ParsePath(root.Resolve("file.txt\\child")), CancellationToken.None);
        ProviderStepOutcome underJunction = await adapter.CreateDirectoryAsync(junction, WindowsLocalOperationTestContext.ParsePath(root.Resolve("link\\child")), CancellationToken.None);
        ProviderStepOutcome foreign = await adapter.CreateDirectoryAsync(parent, WindowsLocalOperationTestContext.ParsePath("\\\\server\\share\\child"), CancellationToken.None);
        ProviderStepOutcome outside = await adapter.CreateDirectoryAsync(parent, WindowsLocalOperationTestContext.ParsePath(root.Resolve("outside")), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.NotFound, underFile.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, underJunction.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, foreign.Failure);
        Assert.AreSame(FileOperationFailureKind.Inspection, outside.Failure);
        Assert.IsFalse(Directory.Exists(root.Resolve("real\\child")));
        Assert.IsFalse(Directory.Exists(root.Resolve("outside")));
    }

    /// <summary>Proves a file and a directory are each renamed inside their own parent.</summary>
    [TestMethod]
    public async Task RenameAsyncWhenEntryIsFileOrDirectoryRenamesInsideTheParent()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));
        FileEntrySnapshot file = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(root.WriteFile("before.txt", "abc")));
        FileEntrySnapshot directory = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(root.CreateDirectory("docs")));

        ProviderStepOutcome renamedFile = await adapter.RenameAsync(file, WindowsLocalOperationTestContext.ParsePath(root.Resolve("after.txt")), CancellationToken.None);
        ProviderStepOutcome renamedDirectory = await adapter.RenameAsync(directory, WindowsLocalOperationTestContext.ParsePath(root.Resolve("papers")), CancellationToken.None);

        Assert.IsNull(renamedFile.Failure);
        Assert.IsNull(renamedDirectory.Failure);
        Assert.IsFalse(File.Exists(root.Resolve("before.txt")));
        Assert.AreEqual("abc", File.ReadAllText(root.Resolve("after.txt")));
        Assert.IsFalse(Directory.Exists(root.Resolve("docs")));
        Assert.IsTrue(Directory.Exists(root.Resolve("papers")));
    }

    /// <summary>Proves a rename that only changes letter case reaches the filesystem instead of being read as a conflict.</summary>
    [TestMethod]
    public async Task RenameAsyncWhenOnlyCaseChangesRenamesToTheNewCase()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));
        FileEntrySnapshot file = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(root.WriteFile("notes.txt", "abc")));

        ProviderStepOutcome outcome = await adapter.RenameAsync(file, WindowsLocalOperationTestContext.ParsePath(root.Resolve("Notes.TXT")), CancellationToken.None);

        Assert.IsNull(outcome.Failure);
        Assert.AreEqual("Notes.TXT", new DirectoryInfo(root.Path.CanonicalText).GetFiles()[0].Name);
    }

    /// <summary>Proves an existing sibling, a changed identity, and a vanished entry each rename nothing.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task RenameAsyncWhenTargetIsTakenOrSourceChangedFailsClosed()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));
        FileEntrySnapshot taken = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(root.WriteFile("a.txt", "abc")));
        _ = root.WriteFile("b.txt", "keep");
        FileEntrySnapshot rewritten = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(root.WriteFile("c.txt", "abc")));
        FileEntrySnapshot vanished = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(root.WriteFile("d.txt", "abc")));
        _ = root.WriteFile("c.txt", "abcdef");
        File.Delete(root.Resolve("d.txt"));

        ProviderStepOutcome conflict = await adapter.RenameAsync(taken, WindowsLocalOperationTestContext.ParsePath(root.Resolve("b.txt")), CancellationToken.None);
        ProviderStepOutcome changed = await adapter.RenameAsync(rewritten, WindowsLocalOperationTestContext.ParsePath(root.Resolve("e.txt")), CancellationToken.None);
        ProviderStepOutcome missing = await adapter.RenameAsync(vanished, WindowsLocalOperationTestContext.ParsePath(root.Resolve("f.txt")), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Conflict, conflict.Failure);
        Assert.AreSame(FileOperationFailureKind.IdentityChanged, changed.Failure);
        Assert.AreSame(FileOperationFailureKind.NotFound, missing.Failure);
        Assert.AreEqual("keep", File.ReadAllText(root.Resolve("b.txt")));
        Assert.IsTrue(File.Exists(root.Resolve("a.txt")));
        Assert.IsFalse(File.Exists(root.Resolve("e.txt")));
    }

    /// <summary>Proves a foreign target, a target in another parent, and a rootless target each rename nothing.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-003")]
    public async Task RenameAsyncWhenTargetLeavesTheParentFailsClosedWithoutWriting()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));
        _ = root.CreateDirectory("parent");
        _ = root.CreateDirectory("other");
        FileEntrySnapshot child = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(root.WriteFile("parent\\a.txt", "abc")));

        ProviderStepOutcome foreign = await adapter.RenameAsync(child, WindowsLocalOperationTestContext.ParsePath("\\\\server\\share\\a.txt"), CancellationToken.None);
        ProviderStepOutcome elsewhere = await adapter.RenameAsync(child, WindowsLocalOperationTestContext.ParsePath(root.Resolve("other\\a.txt")), CancellationToken.None);
        ProviderStepOutcome rootless = await adapter.RenameAsync(child, WindowsLocalOperationTestContext.ParsePath("C:\\"), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, foreign.Failure);
        Assert.AreSame(FileOperationFailureKind.Inspection, elsewhere.Failure);
        Assert.AreSame(FileOperationFailureKind.Inspection, rootless.Failure);
        Assert.IsTrue(File.Exists(root.Resolve("parent\\a.txt")));
        Assert.IsFalse(File.Exists(root.Resolve("other\\a.txt")));
    }
}
