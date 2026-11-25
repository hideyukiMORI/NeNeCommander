using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves append and flush failures do not erase already verified recovery-frame evidence.</summary>
[TestClass]
public sealed class RecoveryMappingContentTests
{
    /// <summary>Proves incomplete or unconfirmed appends keep the same complete initial prefix evidence.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public void RecoveryMappingWhenAppendFailsOrFlushIsUnconfirmedPreservesInitialFrame()
    {
        RecoveryFrameObservation initial = ReceiptObservationTestContext.RecoveryFrame(127, RecoveryFlushObservation.Confirmed);
        RecoveryFrameObservation append = ReceiptObservationTestContext.RecoveryFrame(83, RecoveryFlushObservation.OutcomeUnconfirmed);
        RecoveryAppendObservation[] observations = [
            RecoveryAppendObservation.NotAttempted,
            RecoveryAppendObservation.OutcomeUnconfirmed,
            RecoveryAppendObservation.Verified(append),
            RecoveryAppendObservation.Verified(ReceiptObservationTestContext.RecoveryFrame(83, RecoveryFlushObservation.Confirmed))];

        foreach (RecoveryAppendObservation observation in observations)
        {
            RecoveryMappingContent content = Assert.IsInstanceOfType<RecoveryMappingContent>(ArtifactContentObservation.RecoveryMapping(initial, observation));
            Assert.AreSame(initial, content.InitialFrame);
            Assert.AreSame(observation, content.Append);
            Assert.AreEqual(127, content.InitialFrame.Frame.Size.Bytes);
            Assert.AreSame(RecoveryFlushObservation.Confirmed, content.InitialFrame.Flush);
        }
        VerifiedRecoveryAppend unconfirmedFlush = Assert.IsInstanceOfType<VerifiedRecoveryAppend>(observations[2]);
        Assert.AreSame(append, unconfirmedFlush.Frame);
        Assert.AreSame(RecoveryFlushObservation.OutcomeUnconfirmed, unconfirmedFlush.Frame.Flush);
        Assert.AreEqual(83, unconfirmedFlush.Frame.Frame.Size.Bytes);
    }

    /// <summary>Proves an initial readback success survives a failed flush without allowing a later append claim.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public void RecoveryMappingWhenInitialFlushIsNotConfirmedRetainsBytesAndRefusesAppend()
    {
        foreach (RecoveryFlushObservation flush in new[] { RecoveryFlushObservation.NotAttempted, RecoveryFlushObservation.OutcomeUnconfirmed })
        {
            RecoveryFrameObservation initial = ReceiptObservationTestContext.RecoveryFrame(127, flush);
            RecoveryMappingContent content = Assert.IsInstanceOfType<RecoveryMappingContent>(ArtifactContentObservation.RecoveryMapping(initial, RecoveryAppendObservation.NotAttempted));

            _ = Assert.ThrowsExactly<ArgumentException>(() => ArtifactContentObservation.RecoveryMapping(initial, RecoveryAppendObservation.OutcomeUnconfirmed));
            _ = Assert.ThrowsExactly<ArgumentException>(() => ArtifactContentObservation.RecoveryMapping(initial, RecoveryAppendObservation.Verified(ReceiptObservationTestContext.RecoveryFrame(83, RecoveryFlushObservation.Confirmed))));

            Assert.AreSame(initial.Frame, content.InitialFrame.Frame);
            Assert.AreSame(flush, content.InitialFrame.Flush);
            Assert.AreSame(RecoveryAppendObservation.NotAttempted, content.Append);
        }
    }

    /// <summary>Proves an absent or empty frame is never accepted as a complete recovery record.</summary>
    [TestMethod]
    public void RecoveryFrameWhenEvidenceIsAbsentOrEmptyRejectsTheClaim()
    {
        FileContentEvidence bytes = ReceiptObservationTestContext.Content(4);
        RecoveryFrameEvidence frame = RecoveryFrameEvidence.Create(bytes.Size, bytes.Hash);
        RecoveryFrameObservation observed = RecoveryFrameObservation.Create(frame, RecoveryFlushObservation.Confirmed);

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => RecoveryFrameEvidence.Create(null!, bytes.Hash));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => RecoveryFrameEvidence.Create(bytes.Size, null!));
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => RecoveryFrameEvidence.Create(ReceiptObservationTestContext.Content(0).Size, bytes.Hash));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => RecoveryFrameObservation.Create(null!, RecoveryFlushObservation.Confirmed));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => RecoveryFrameObservation.Create(frame, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => RecoveryAppendObservation.Verified(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => ArtifactContentObservation.RecoveryMapping(null!, RecoveryAppendObservation.NotAttempted));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => ArtifactContentObservation.RecoveryMapping(observed, null!));
        Assert.AreSame(bytes.Hash, frame.Hash);
    }

}
