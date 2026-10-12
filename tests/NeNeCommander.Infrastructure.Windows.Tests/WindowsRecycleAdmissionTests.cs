using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves filesystem admission and routing before a recycle request reaches Shell.</summary>
[TestClass]
public sealed class WindowsRecycleAdmissionTests
{
    /// <summary>Missing and drive-root entries cannot be offered as recoverable.</summary>
    [TestMethod]
    public void GetCapabilityWhenEntryIsMissingOrRootReturnsPermanentOnly()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsRecycleBin bin = new(new ScriptedWindowsRecycleShell());
        Assert.AreSame(DeletionCapability.PermanentOnly, bin.GetCapability(WindowsRecycleBinTests.Path(root.Resolve("missing"))));
        Assert.AreSame(DeletionCapability.PermanentOnly, bin.GetCapability(WindowsRecycleBinTests.Path(root.Path.Drive + "\\")));
    }

    /// <summary>Neither a junction nor a tree containing one is admitted.</summary>
    [TestMethod]
    public void GetCapabilityWhenSourceOrAncestorIsJunctionRefusesRecycle()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("target");
        _ = root.WriteFile("target\\payload", "owned");
        WindowsLocalPath link = WindowsRecycleBinTests.Path(root.CreateJunction("link", "target"));
        WindowsRecycleBin bin = new(new ScriptedWindowsRecycleShell());
        Assert.AreSame(DeletionCapability.PermanentOnly, bin.GetCapability(link));
        Assert.AreSame(DeletionCapability.PermanentOnly, bin.GetCapability(root.Path));
        Assert.AreSame(DeletionCapability.PermanentOnly, bin.GetCapability(WindowsRecycleBinTests.Path(root.Resolve("link\\payload"))));
        Assert.AreEqual("owned", File.ReadAllText(root.Resolve("target\\payload")));
    }

    /// <summary>A changed or vanished source is refused before opening a Shell operation.</summary>
    [TestMethod]
    [DataRow("Rewritten")]
    [DataRow("Missing")]
    public void RecycleWhenSourceNoLongerMatchesRefusesEffect(string change)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = WindowsRecycleBinTests.Source(root);
        if (change == "Missing")
        {
            File.Delete(source.Path.CanonicalText);
        }
        else
        {
            _ = root.WriteFile("source", "changed owned content");
        }
        ScriptedWindowsRecycleShell shell = new();
        ProviderStepOutcome result = new WindowsRecycleBin(shell).Recycle(source, CancellationToken.None);
        Assert.AreSame(change == "Missing" ? FileOperationFailureKind.NotFound : FileOperationFailureKind.IdentityChanged, result.Failure);
        Assert.AreEqual(0, shell.Operation.PerformCount);
    }

    /// <summary>Recycle uses Shell while an explicit permanent mode retains the ordinary delete path.</summary>
    [TestMethod]
    public async Task DeleteAsyncWhenCapabilityIsRecycleRoutesOnlyRequestedModeAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath path = WindowsRecycleBinTests.Path(root.WriteFile("source", "owned"));
        ScriptedWindowsRecycleShell shell = new();
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(shell));
        FileInspectionSucceeded inspected = Assert.IsInstanceOfType<FileInspectionSucceeded>(await adapter.InspectAsync(path, CancellationToken.None));
        Assert.AreSame(DeletionCapability.Recycle, inspected.Snapshot.DeletionCapability);
        Assert.IsNull((await adapter.DeleteAsync(inspected.Snapshot, DeletionExecutionMode.Recycle, CancellationToken.None)).Failure);
        Assert.AreEqual(1, shell.Operation.PerformCount);
        Assert.IsTrue(File.Exists(path.CanonicalText));
        Assert.IsNull((await adapter.DeleteAsync(inspected.Snapshot, DeletionExecutionMode.Permanent, CancellationToken.None)).Failure);
        Assert.AreEqual(1, shell.Operation.PerformCount);
        Assert.IsFalse(File.Exists(path.CanonicalText));
    }
}
