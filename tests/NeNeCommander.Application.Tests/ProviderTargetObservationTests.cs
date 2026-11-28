using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves exact-object content observations cannot confuse absence, uncertainty and verified empty bytes.</summary>
[TestClass]
public sealed class ProviderTargetObservationTests
{
    /// <summary>Proves all four observations retain the same target while only verified states carry byte evidence.</summary>
    [TestMethod]
    public void ObservationsWhenContentIsEmptyKeepDistinctMutationAndVerificationClaims()
    {
        ObservedTransferEntry target = ReceiptObservationTestContext.Entry("D:\\destination\\source.txt", TransferPathObservation.CurrentPathVerified);
        FileContentEvidence empty = ReceiptObservationTestContext.Content(0);

        ProviderTargetObservation untouched = ProviderTargetObservation.Untouched(target);
        ProviderTargetObservation uncertain = ProviderTargetObservation.MayHaveChanged(target);
        OriginalTargetContentVerified original = Assert.IsInstanceOfType<OriginalTargetContentVerified>(ProviderTargetObservation.OriginalVerified(target, empty));
        DesiredTargetContentVerified desired = Assert.IsInstanceOfType<DesiredTargetContentVerified>(ProviderTargetObservation.DesiredVerified(target, empty));
        ProviderTargetObservation clone = desired with { };

        _ = Assert.IsInstanceOfType<UntouchedTransferTarget>(untouched);
        _ = Assert.IsInstanceOfType<TargetMayHaveChanged>(uncertain);
        Assert.AreSame(target, untouched.Target);
        Assert.AreSame(target, uncertain.Target);
        Assert.AreSame(target, original.Target);
        Assert.AreSame(target, desired.Target);
        Assert.AreSame(empty, original.Content);
        Assert.AreSame(empty, desired.Content);
        Assert.AreEqual(0, desired.Content.Size.Bytes);
        Assert.AreSame(empty.Hash, desired.Content.Hash);
        Assert.AreEqual(desired, clone);
        Assert.AreEqual(desired.GetHashCode(), clone.GetHashCode());
    }

    /// <summary>Proves an unrelated step has no overwrite observation, distinct from an observed untouched target.</summary>
    [TestMethod]
    public void ReceiptWhenTargetWasNeverAdmittedReportsAbsenceRatherThanUntouchedClaim()
    {
        ObservedTransferEntry target = ReceiptObservationTestContext.Entry("D:\\destination\\source.txt", TransferPathObservation.LastKnownPath);

        ProviderStepReceipt untouched = ProviderStepReceipt.Create([], ProviderTargetObservation.Untouched(target), []);

        Assert.IsNull(ProviderStepReceipt.Empty.Target);
        _ = Assert.IsInstanceOfType<UntouchedTransferTarget>(untouched.Target);
        Assert.AreSame(TransferPathObservation.LastKnownPath, untouched.Target.Target.PathObservation);
        Assert.IsEmpty(untouched.Artifacts);
    }

    /// <summary>Proves verification requires both complete byte evidence and a known exact target observation.</summary>
    [TestMethod]
    public void ObservationsWhenRequiredEvidenceIsMissingRefuseTheClaim()
    {
        ObservedTransferEntry target = ReceiptObservationTestContext.Entry("D:\\destination\\source.txt", TransferPathObservation.LastKnownPath);
        FileContentEvidence content = ReceiptObservationTestContext.Content(7);

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => ObservedTransferEntry.Create(null!, TransferPathObservation.LastKnownPath));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => ObservedTransferEntry.Create(target.Entry, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => FileContentEvidence.Create(null!, content.Hash));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => FileContentEvidence.Create(content.Size, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => ProviderTargetObservation.Untouched(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => ProviderTargetObservation.MayHaveChanged(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => ProviderTargetObservation.OriginalVerified(null!, content));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => ProviderTargetObservation.OriginalVerified(target, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => ProviderTargetObservation.DesiredVerified(null!, content));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => ProviderTargetObservation.DesiredVerified(target, null!));
    }
}
