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
        Assert.AreEqual("Destination contents verified against the source.\nLast known path: D:\\right\\source", rows[1]);
    }

    /// <summary>Supplies all twelve closed canonical operation failures.</summary>
    public static IEnumerable<object[]> Failures()
    {
        return
        [
            [FileOperationFailureKind.Copy, "The copy stage did not complete."],
            [FileOperationFailureKind.Move, "The move stage did not complete."],
            [FileOperationFailureKind.Verification, "The copied contents could not be verified."],
            [FileOperationFailureKind.Delete, "Source removal did not complete."],
            [FileOperationFailureKind.Conflict, "The destination conflicts with another entry."],
            [FileOperationFailureKind.IdentityChanged, "A file or its location changed after confirmation."],
            [FileOperationFailureKind.Reentrant, "Another operation already owns this request."],
            [FileOperationFailureKind.Inspection, "Required file facts could not be inspected."],
            [FileOperationFailureKind.ConfirmationRequired, "Required deletion confirmation is missing."],
            [FileOperationFailureKind.ProviderUnavailable, "The required file operation is unavailable."],
            [FileOperationFailureKind.AccessDenied, "Access to a required file or folder was denied."],
            [FileOperationFailureKind.NotFound, "A required file or folder was not found."],
        ];
    }
}
