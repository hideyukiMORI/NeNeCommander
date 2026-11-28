using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves receipt validation and retained storage refer to the same owned observations.</summary>
[TestClass]
public sealed class ReceiptSnapshotAdmissionTests
{
    /// <summary>Proves repeated caller enumeration cannot insert an unvalidated effect into a provider receipt.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public void CreateWhenProviderInputChangesOnSecondEnumerationRetainsOnlyValidatedSnapshot()
    {
        ChangingReceiptCollection<ProviderStepEffectKind> input = new(ProviderStepEffectKind.CopyTargetCreated, null!);

        ProviderStepReceipt receipt = ProviderStepReceipt.Create(input, null, []);

        Assert.HasCount(1, receipt.Effects);
        Assert.AreSame(ProviderStepEffectKind.CopyTargetCreated, receipt.Effects[0]);
    }

    /// <summary>Proves effects, skips and qualified provider steps are validated from their stored snapshots.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public void CreateWhenOutcomeInputsChangeOnSecondEnumerationRetainsOnlyValidatedSnapshots()
    {
        FileSystemPath source = GatewayTestContext.ParsePath("C:\\source.txt");
        FileOperationEffect effect = FileOperationEffect.Create(source, FileOperationEffectKind.Copied);
        OperationProviderStep step = OperationProviderStep.Create(source, ProviderStepReceipt.Empty);
        ChangingReceiptCollection<FileOperationEffect> effects = new(effect, null!);
        ChangingReceiptCollection<FileSystemPath> skipped = new(source, null!);
        ChangingReceiptCollection<OperationProviderStep> steps = new(step, null!);

        FileOperationOutcome outcome = FileOperationOutcome.Succeeded(FileOperationEffectSummary.Create(effects, skipped, steps));

        Assert.AreSame(effect, outcome.Effects[0]);
        Assert.AreSame(source, outcome.NotTransferred[0]);
        Assert.AreSame(step, outcome.ProviderSteps[0]);
    }
}
