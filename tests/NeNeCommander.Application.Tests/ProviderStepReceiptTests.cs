using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves mandatory provider receipts own ordered evidence on every return.</summary>
[TestClass]
public sealed class ProviderStepReceiptTests
{
    /// <summary>Proves caller changes and synchronization casts cannot erase or substitute provider facts.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public void CreateWhenCallerChangesCollectionsRetainsEveryOriginalEffect()
    {
        ProviderStepEffectKind[] input = [ProviderStepEffectKind.CopyTargetCreated, ProviderStepEffectKind.SourcePartiallyDeleted];
        ProviderStepReceipt receipt = ProviderStepReceipt.Create(input, null, []);

        input[0] = ProviderStepEffectKind.Recycled;
        _ = Assert.ThrowsExactly<NotSupportedException>(() => ((IList<ProviderStepEffectKind>)receipt.Effects).Clear());
        ICollection collection = Assert.IsInstanceOfType<ICollection>(receipt.Effects);
        ICollection<ProviderStepEffectKind> sync = Assert.IsInstanceOfType<ICollection<ProviderStepEffectKind>>(collection.SyncRoot);
        _ = Assert.ThrowsExactly<NotSupportedException>(sync.Clear);

        Assert.HasCount(2, receipt.Effects);
        Assert.AreSame(ProviderStepEffectKind.CopyTargetCreated, receipt.Effects[0]);
        Assert.AreSame(ProviderStepEffectKind.SourcePartiallyDeleted, receipt.Effects[1]);
        IEnumerator enumerator = ((IEnumerable)receipt.Effects).GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreSame(receipt.Effects[0], enumerator.Current);
    }

    /// <summary>Proves failure and success both keep the same required evidence instead of null effect sentinels.</summary>
    [TestMethod]
    public void CreateWhenSuccessOrFailureKeepsTheCompleteReceiptAndRecordCopy()
    {
        ProviderStepReceipt receipt = ProviderStepReceipt.Create([ProviderStepEffectKind.CopyTargetCreated], null, []);

        ProviderStepOutcome success = ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, receipt);
        ProviderStepOutcome failed = ProviderStepOutcome.Create(ProviderStepCompletion.Failed(FileOperationFailureKind.Copy), receipt);
        ProviderStepOutcome clone = failed with { };

        ProviderStepOutcome cancelled = ProviderStepOutcome.Create(ProviderStepCompletion.Cancelled, receipt);
        Assert.AreSame(ProviderStepCompletion.Succeeded, success.Completion);
        Assert.AreSame(ProviderStepCompletion.Cancelled, cancelled.Completion);
        Assert.AreNotEqual(success.Completion, cancelled.Completion);
        Assert.IsNull(cancelled.Failure);
        Assert.AreSame(receipt, cancelled.Receipt);
        Assert.IsNull(success.Failure);
        Assert.AreSame(receipt, success.Receipt);
        Assert.AreSame(receipt, failed.Receipt);
        Assert.AreSame(FileOperationFailureKind.Copy, failed.Failure);
        Assert.AreEqual(failed, clone);
        Assert.AreEqual(failed.GetHashCode(), clone.GetHashCode());
        Assert.AreSame(receipt, clone.Receipt);
        Assert.IsEmpty(ProviderStepReceipt.Empty.Effects);
    }

    /// <summary>Proves an absent receipt or absent effect cannot enter operation evidence.</summary>
    [TestMethod]
    public void CreateWhenRequiredEvidenceIsAbsentRejectsTheMissingComponent()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => ProviderStepOutcome.Create(null!, ProviderStepReceipt.Empty));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => ProviderStepCompletion.Failed(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => ProviderStepReceipt.Create(null!, null, []));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => ProviderStepReceipt.Create([null!], null, []));
    }
}
