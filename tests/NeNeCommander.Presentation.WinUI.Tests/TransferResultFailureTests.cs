using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Panes;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves every canonical failure is disclosed beside stronger target-content evidence.</summary>
[TestClass]
public sealed class TransferResultFailureTests
{
    /// <summary>Verified target content does not substitute for the concrete operation failure.</summary>
    [TestMethod]
    [DynamicData(nameof(Failures))]
    public async Task ComposeAsyncWhenFailureExistsKeepsItsSpecificReason(FileOperationFailureKind failure, string expected)
    {
        ProviderStepReceipt receipt = ProviderStepReceipt.Create([], TransferResultFixture.Target("desired", TransferPathObservation.LastKnownPath), []);
        TransferResultDetail detail = await TransferResultFixture.RunAsync(OperationKind.Copy,
            ProviderStepOutcome.Create(ProviderStepCompletion.Failed(failure), receipt),
            ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));

        IReadOnlyList<string> rows = TransferResultFacts.Compose(detail, TransferResultFixture.Localize);

        Assert.AreEqual(expected, rows[0]);
        Assert.AreEqual("desired|last:D:\\right\\source", rows[1]);
    }

    /// <summary>Supplies all twelve closed canonical operation failures.</summary>
    public static IEnumerable<object[]> Failures()
    {
        return
        [
            [FileOperationFailureKind.Copy, "TransferFailureCopy"],
            [FileOperationFailureKind.Move, "TransferFailureMove"],
            [FileOperationFailureKind.Verification, "TransferFailureVerification"],
            [FileOperationFailureKind.Delete, "TransferFailureDelete"],
            [FileOperationFailureKind.Conflict, "TransferFailureConflict"],
            [FileOperationFailureKind.IdentityChanged, "TransferFailureIdentityChanged"],
            [FileOperationFailureKind.Reentrant, "TransferFailureReentrant"],
            [FileOperationFailureKind.Inspection, "TransferFailureInspection"],
            [FileOperationFailureKind.ConfirmationRequired, "TransferFailureConfirmation"],
            [FileOperationFailureKind.ProviderUnavailable, "TransferFailureUnavailable"],
            [FileOperationFailureKind.AccessDenied, "TransferFailureAccessDenied"],
            [FileOperationFailureKind.NotFound, "TransferFailureNotFound"],
        ];
    }
}
