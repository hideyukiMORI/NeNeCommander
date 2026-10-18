using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Preserves WslFileOperationAdapterTests evidence for WslDeletionAdmissionTests.</summary>
[TestClass]
public sealed class WslDeletionAdmissionTests
{
    /// <summary>Proves permanent deletion refuses a link introduced into a source tree.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-003")]
    public async Task DeleteAsyncWhenSourceTreeGainsLinkReturnsFailureWithoutEffect()
    {
        ScriptedWslFileSystem fileSystem = new() { ContainsNestedReparsePoint = true };
        WslFileSystemEntry sourceEntry = WslOperationTestContext.Entry(
            WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home\\source"),
            "source",
            DirectoryEntryKind.Directory);
        fileSystem.Set(sourceEntry);

        ProviderStepOutcome outcome = await WslOperationTestContext.Adapter(fileSystem).DeleteAsync(
            WslOperationTestContext.Snapshot(sourceEntry),
            DeletionExecutionMode.Permanent,
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
        Assert.HasCount(0, fileSystem.Deleted);
    }
}
