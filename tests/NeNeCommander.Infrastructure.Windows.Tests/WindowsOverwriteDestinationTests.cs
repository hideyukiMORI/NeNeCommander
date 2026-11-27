using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the provider refuses a destination outside the exact accepted conflict without beginning preparation.</summary>
[TestClass]
public sealed class WindowsOverwriteDestinationTests
{
    /// <summary>An otherwise valid gateway-created snapshot cannot authorize writing the original target under a different destination request.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-006")]
    public async Task CopyAsyncWhenDestinationDiffersFromConsentRefusesBeforeEffectsAsync()
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("desired", "original");
        FileEntrySnapshot source = await OverwriteSnapshotPort.CaptureAsync(files);
        FileSystemPath destination = WindowsTransferRecoveryTestFixture.Path(files.Root.CreateDirectory("different"));
        files.Pair.Dispose();
        DeterministicIdentifierSource identifiers = new();
        WindowsLocalFileOperationAdapter adapter = new(new(), identifiers);

        ProviderStepOutcome outcome = await adapter.CopyAsync(source, destination, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Conflict, outcome.Failure);
        Assert.IsNull(outcome.Receipt.Target);
        Assert.IsEmpty(outcome.Receipt.Artifacts);
        Assert.IsEmpty(outcome.Receipt.Effects);
        Assert.AreEqual(0, identifiers.IssuedCount);
        Assert.AreEqual("desired", files.Read(source.Path));
        Assert.AreEqual("original", files.Read(source.TransferTarget!));
        Assert.IsEmpty(Directory.GetFileSystemEntries(destination.CanonicalText));
        Assert.HasCount(2, Directory.GetFiles(files.Root.Path.CanonicalText));
    }
}
