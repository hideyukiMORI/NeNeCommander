using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Panes;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves source retention is not inferred from absent deletion acknowledgement after a move reaches deletion.</summary>
[TestClass]
public sealed class TransferResultSourceTests
{
    /// <summary>Copy retains source; moves distinguish acknowledged, partial and unconfirmed deletion.</summary>
    [TestMethod]
    [DataRow("copy", "source-kept")]
    [DataRow("removed", "source-removed")]
    [DataRow("partial", "source-partly-removed")]
    [DataRow("failed", "source-unconfirmed")]
    [DataRow("cancelled", "source-unconfirmed")]
    public async Task ComposeAsyncWhenDeletionKnowledgeVariesDoesNotInventSourceRetention(string mode, string expected)
    {
        ProviderStepReceipt receipt = ProviderStepReceipt.Create([], TransferResultFixture.Target("desired", TransferPathObservation.LastKnownPath), []);
        OperationKind kind = mode == "copy" ? OperationKind.Copy : OperationKind.Move;
        TransferResultDetail detail = await TransferResultFixture.RunAsync(kind,
            ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, receipt), Deletion(mode));

        IReadOnlyList<string> rows = TransferResultFacts.Compose(detail, TransferResultFixture.Localize);

        string source = expected + "|C:\\left\\source";
        CollectionAssert.Contains(System.Linq.Enumerable.ToArray(rows), source);
        Assert.AreEqual(1, detail.Verified);
        Assert.AreEqual(mode == "removed" ? 1 : 0, detail.SourceDeleted);
    }

    private static ProviderStepOutcome Deletion(string mode)
    {
        return mode switch
        {
            "copy" or "removed" => ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty),
            "partial" => ProviderStepOutcome.Create(ProviderStepCompletion.Failed(FileOperationFailureKind.Delete),
                ProviderStepReceipt.Create([ProviderStepEffectKind.SourcePartiallyDeleted], null, [])),
            "failed" => ProviderStepOutcome.Create(ProviderStepCompletion.Failed(FileOperationFailureKind.Delete), ProviderStepReceipt.Empty),
            "cancelled" => ProviderStepOutcome.Create(ProviderStepCompletion.Cancelled, ProviderStepReceipt.Empty),
            _ => throw new ArgumentOutOfRangeException(nameof(mode)),
        };
    }
}
