using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves unavailable capabilities retain one reason without inventing original object evidence.</summary>
[TestClass]
public sealed class TransferOverwriteCapabilityTests
{
    /// <summary>A refusal owns its exact reason and provides no destructive choice.</summary>
    [TestMethod]
    public void CreateWhenCapabilityIsUnavailableRetainsOnlyRefusal()
    {
        TransferConflict original = TransferConflictTestContext.Conflict(GatewayTestContext.ParsePath(@"C:\source.txt"),
            GatewayTestContext.ParsePath(@"D:\destination"), "source (2).txt");
        TransferOverwriteCapability unavailable = TransferOverwriteCapability.Unavailable(TransferOverwriteUnavailableReason.ReservedTarget);

        TransferConflict conflict = TransferConflict.Create(original.Source, original.ExistingTarget, original.KeepBothCandidate, unavailable);

        Assert.AreSame(unavailable, conflict.Target.Overwrite);
        Assert.AreSame(TransferOverwriteUnavailableReason.ReservedTarget,
            Assert.IsInstanceOfType<TransferOverwriteCapability.UnavailableOverwrite>(conflict.Target.Overwrite).Reason);
        Assert.IsNull(conflict.Target.OverwriteEvidence);
        CollectionAssert.DoesNotContain(System.Linq.Enumerable.ToArray(conflict.AllowedDecisions), TransferConflictDecision.OverwriteContents);
    }

    /// <summary>Missing observations, reasons or capabilities cannot masquerade as an unavailable case.</summary>
    [TestMethod]
    public void CreateWhenRequiredCapabilityIsAbsentRejectsIt()
    {
        TransferConflict original = TransferConflictTestContext.Conflict(GatewayTestContext.ParsePath(@"C:\source.txt"),
            GatewayTestContext.ParsePath(@"D:\destination"), "source (2).txt");

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferOverwriteCapability.Available(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferOverwriteCapability.Unavailable(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferConflict.Create(original.Source, original.ExistingTarget, original.KeepBothCandidate, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferConflictTarget.Create(original.ExistingTarget, null!));
    }
}
