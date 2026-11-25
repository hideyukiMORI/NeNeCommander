using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves completed operation facts cannot be rewritten through collection synchronization surfaces.</summary>
[TestClass]
public sealed class OperationReceiptOwnershipTests
{
    /// <summary>Proves synchronization objects cannot erase effects or skipped-source facts.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public void OutcomeWhenSynchronizationStorageIsExaminedCannotEraseReportedFacts()
    {
        FileSystemPath source = GatewayTestContext.ParsePath("C:\\source.txt");
        FileOperationEffect effect = FileOperationEffect.Create(source, FileOperationEffectKind.Copied);
        ProviderStepReceipt receipt = ProviderStepReceipt.Create([ProviderStepEffectKind.CopyTargetCreated], null, []);
        OperationProviderStep step = OperationProviderStep.Create(source, receipt);
        OperationProviderStep[] input = [step];
        FileOperationOutcome outcome = FileOperationOutcome.Succeeded(FileOperationEffectSummary.Create([effect], [source], input));
        input[0] = OperationProviderStep.Create(source, ProviderStepReceipt.Empty);

        AssertCannotClear<FileOperationEffect>(outcome.Effects);
        AssertCannotClear<FileSystemPath>(outcome.NotTransferred);
        AssertCannotClear<OperationProviderStep>(outcome.ProviderSteps);

        Assert.AreSame(effect, outcome.Effects[0]);
        Assert.AreSame(source, outcome.NotTransferred[0]);
        Assert.AreSame(step, outcome.ProviderSteps[0]);
        Assert.AreSame(receipt, outcome.ProviderSteps[0].Receipt);
    }

    private static void AssertCannotClear<T>(IReadOnlyList<T> snapshot)
    {
        if (snapshot is ICollection collection && collection.SyncRoot is ICollection<T> exposed)
        {
            _ = Assert.ThrowsExactly<NotSupportedException>(exposed.Clear);
        }
    }
}
