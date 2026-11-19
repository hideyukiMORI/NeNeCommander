using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves request-specific inspection and complete ordinary-deletion admission.</summary>
[TestClass]
public sealed class FileInspectionPurposeTests
{
    /// <summary>Only an ordinary deletion requests a deletion-capability observation.</summary>
    [TestMethod]
    [DataRow("Move")]
    [DataRow("Copy")]
    [DataRow("Rename")]
    [DataRow("CreateDirectory")]
    [DataRow("Delete")]
    public async Task ExecuteAsyncWhenRequestIsInspectedPassesItsRequiredPurpose(string operation)
    {
        FileSystemPath path = GatewayTestContext.ParsePath("C:\\owned");
        FileOperationRequest request = operation switch
        {
            "Move" => GatewayTestContext.CreateMove([path]),
            "Copy" => GatewayTestContext.CreateCopy([path]),
            "Rename" => GatewayTestContext.CreateRename(path, "renamed"),
            "CreateDirectory" => GatewayTestContext.CreateCreateDirectory(path, "child"),
            "Delete" => GatewayTestContext.CreateDelete([path], null),
            _ => throw new InvalidOperationException("Unknown test operation."),
        };
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(FileInspectionOutcome.Failed(FileOperationFailureKind.NotFound));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(request, RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.NotFound, outcome.Failure);
        CollectionAssert.AreEqual(new[] { operation == "Delete" ? FileInspectionPurpose.Deletion : FileInspectionPurpose.IdentityOnly },
            new System.Collections.Generic.List<FileInspectionPurpose>(port.InspectionPurposes));
        Assert.HasCount(1, port.Calls);
    }

    /// <summary>A later uninspected deletion entry prevents every earlier effect, even with confirmation.</summary>
    [TestMethod]
    [DataRow("Unconfirmed")]
    [DataRow("Confirmed")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-008")]
    public async Task DeleteWhenLaterCapabilityWasNotRequestedRejectsWholeBatch(string confirmationState)
    {
        FileSystemPath first = GatewayTestContext.ParsePath("C:\\first");
        FileSystemPath second = GatewayTestContext.ParsePath("C:\\second");
        PermanentDeletionConfirmation? confirmation = confirmationState == "Confirmed"
            ? PermanentDeletionConfirmation.CreateFor(GatewayTestContext.CreateDelete([first, second], null))
            : null;
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(GatewayTestContext.Inspection(first, DeletionCapability.Recycle));
        port.EnqueueInspection(GatewayTestContext.Inspection(second, DeletionCapability.NotRequested));
        port.EnqueueDeletion(ProviderStepOutcome.Succeeded());
        port.EnqueueDeletion(ProviderStepOutcome.Succeeded());
        RecordingFileOperationProgress progress = RecordingFileOperationProgress.Create();
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(GatewayTestContext.CreateDelete([first, second], confirmation), progress, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Inspection, outcome.Failure);
        Assert.AreSame(FileOperationCompletionKind.Rejected, outcome.Completion);
        Assert.IsEmpty(outcome.Effects);
        Assert.IsEmpty(progress.Reports);
        Assert.IsEmpty(port.DeletionModes);
        Assert.HasCount(2, port.Calls);
        CollectionAssert.AreEqual(new[] { FileInspectionPurpose.Deletion, FileInspectionPurpose.Deletion },
            new System.Collections.Generic.List<FileInspectionPurpose>(port.InspectionPurposes));
    }

    /// <summary>The unrequested case is distinct from both observed capabilities and keeps record semantics.</summary>
    [TestMethod]
    public void CapabilityAndPurposeWhenCopiedPreserveTheirClosedValues()
    {
        Assert.IsFalse(DeletionCapability.NotRequested.IsInspected);
        Assert.IsTrue(DeletionCapability.Recycle.IsInspected);
        Assert.IsTrue(DeletionCapability.PermanentOnly.IsInspected);
        Assert.AreNotEqual(DeletionCapability.NotRequested, DeletionCapability.Recycle);
        Assert.AreNotEqual(DeletionCapability.NotRequested, DeletionCapability.PermanentOnly);
        Assert.AreEqual(DeletionCapability.NotRequested, DeletionCapability.NotRequested with { });
        Assert.AreEqual(DeletionCapability.NotRequested.GetHashCode(), (DeletionCapability.NotRequested with { }).GetHashCode());
        Assert.AreEqual(FileInspectionPurpose.IdentityOnly, FileInspectionPurpose.IdentityOnly with { });
        Assert.AreEqual(FileInspectionPurpose.Deletion, FileInspectionPurpose.Deletion with { });
        Assert.AreNotEqual(FileInspectionPurpose.IdentityOnly, FileInspectionPurpose.Deletion);
        Assert.AreEqual(FileInspectionPurpose.IdentityOnly.GetHashCode(), (FileInspectionPurpose.IdentityOnly with { }).GetHashCode());
    }
}
