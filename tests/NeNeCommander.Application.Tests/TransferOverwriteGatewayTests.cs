using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves overwrite move cannot downgrade verification or escape the original confirmed object.</summary>
[TestClass]
public sealed class TransferOverwriteGatewayTests
{
    /// <summary>The exact strict file receipt is passed through the existing verified-deletion route without asking for atomic move.</summary>
    [TestMethod]
    public async Task ResumeAsyncWhenStrictEvidenceMatchesDeletesOnlyThroughVerifiedCopy()
    {
        TransferOverwriteGatewayFixture fixture = new();
        VerifiedCopyReceipt receipt = fixture.Receipt("valid");

        FileOperationOutcome outcome = await fixture.MoveAsync(receipt);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        CollectionAssert.AreEqual(new[] { FileOperationEffectKind.Copied, FileOperationEffectKind.Verified, FileOperationEffectKind.SourceDeleted },
            outcome.Effects.Select(effect => effect.Kind).ToArray());
        Assert.IsFalse(fixture.Port.Calls.Any(call => call.StartsWith("Atomic", System.StringComparison.Ordinal)));
        DeletionExecutionMode.VerifiedCopyMode mode = Assert.IsInstanceOfType<DeletionExecutionMode.VerifiedCopyMode>(fixture.Port.DeletionModes.Single());
        Assert.AreSame(receipt, mode.Receipt);
    }

    /// <summary>Weaker requirements, substituted objects and directory evidence cannot authorize removal.</summary>
    [TestMethod]
    [DataRow("defaultStream")]
    [DataRow("foreignSource")]
    [DataRow("foreignTarget")]
    [DataRow("directory")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    public async Task ResumeAsyncWhenOverwriteProofIsDowngradedRefusesSourceDeletion(string kind)
    {
        TransferOverwriteGatewayFixture fixture = new();

        FileOperationOutcome outcome = await fixture.MoveAsync(fixture.Receipt(kind));

        Assert.AreSame(FileOperationFailureKind.Verification, outcome.Failure);
        Assert.HasCount(1, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.Copied, outcome.Effects[0].Kind);
        Assert.IsEmpty(fixture.Port.DeletionModes);
        Assert.IsFalse(fixture.Port.Calls.Any(call => call.StartsWith("Atomic", System.StringComparison.Ordinal)));
    }

    /// <summary>A cancellation choice cannot authorize either transfer or skip in a provider plan.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    public void IsValidPlanWhenChoiceIsCancelledRejectsTransferAndSkip()
    {
        TransferOverwriteGatewayFixture fixture = new();
        FileEntrySnapshot cancelled = fixture.Conflict.Source.WithConflictChoice(
            TransferConflictChoice.Create(fixture.Conflict, TransferConflictDecision.Cancel));

        Assert.IsFalse(TransferPreflightValidation.IsValidPlan(
            [TransferPlanEntry.Transfer(cancelled, fixture.Conflict.ExistingTarget)], [cancelled], fixture.Destination));
        Assert.IsFalse(TransferPreflightValidation.IsValidPlan(
            [TransferPlanEntry.Skip(cancelled, fixture.Conflict.ExistingTarget)], [cancelled], fixture.Destination));
    }

    /// <summary>Only the original ordinary target can be used with the same overwrite consent.</summary>
    [TestMethod]
    public void IsValidPlanWhenOverwriteTargetIsSubstitutedRejectsIt()
    {
        TransferOverwriteGatewayFixture fixture = new();
        TransferPlanEntry original = TransferPlanEntry.Transfer(fixture.ResolvedSource, fixture.Conflict.ExistingTarget);
        TransferPlanEntry substituted = TransferPlanEntry.Transfer(fixture.ResolvedSource, fixture.Conflict.KeepBothCandidate);

        Assert.IsTrue(TransferPreflightValidation.IsValidPlan([original], [fixture.ResolvedSource], fixture.Destination));
        Assert.IsFalse(TransferPreflightValidation.IsValidPlan([substituted], [fixture.ResolvedSource], fixture.Destination));
    }
}
