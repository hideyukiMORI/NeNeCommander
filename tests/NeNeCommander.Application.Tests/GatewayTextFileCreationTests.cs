using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves empty-file gateway behavior and boundaries.</summary>
[TestClass]
public sealed class GatewayTextFileCreationTests
{
    /// <summary>Proves a empty-file creation inspects the location, creates once, and reports one effect and full progress.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenTextFileIsCreatedReportsEffectAndProgress()
    {
        FileSystemPath location = GatewayTestContext.ParsePath("C:\\location");
        CreateTextFileRequest request = CreateRequest(location, "child");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(GatewayTestContext.Inspection(location, DeletionCapability.PermanentOnly));
        port.EnqueueFileCreation(ProviderStepOutcome.Succeeded());
        using FileOperationGateway gateway = new(port);
        RecordingFileOperationProgress progress = RecordingFileOperationProgress.Create();

        FileOperationOutcome outcome = await gateway.ExecuteAsync(request, progress, CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.HasCount(1, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.FileCreated, outcome.Effects[0].Kind);
        Assert.AreSame(request.Target, outcome.Effects[0].Source);
        Assert.HasCount(2, port.Calls);
        Assert.AreEqual("Inspect:C:\\location", port.Calls[0]);
        Assert.AreEqual("CreateEmptyFile:C:\\location\\child", port.Calls[1]);
        Assert.HasCount(1, progress.Reports);
        Assert.AreEqual(FileOperationProgress.Create(1, 1), progress.Reports[0]);
    }

    /// <summary>Proves inspection failure, cancellation after inspection, and provider rejection each create nothing.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    [TestProperty("ThreatId", "ADV-018")]
    public async Task ExecuteAsyncWhenTextFileCreationCannotProceedCreatesNothing()
    {
        FileSystemPath location = GatewayTestContext.ParsePath("C:\\location");
        using CancellationTokenSource cancellation = new();
        ScriptedFileOperationPort missing = ScriptedFileOperationPort.Create(null, null);
        missing.EnqueueInspection(FileInspectionOutcome.Failed(FileOperationFailureKind.NotFound));
        ScriptedFileOperationPort cancelled = ScriptedFileOperationPort.Create(ScriptedCallbackPoint.AfterInspection, cancellation.Cancel);
        cancelled.EnqueueInspection(GatewayTestContext.Inspection(location, DeletionCapability.PermanentOnly));
        ScriptedFileOperationPort refused = ScriptedFileOperationPort.Create(null, null);
        refused.EnqueueInspection(GatewayTestContext.Inspection(location, DeletionCapability.PermanentOnly));
        refused.EnqueueFileCreation(ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict));
        using FileOperationGateway missingGateway = new(missing);
        using FileOperationGateway cancelledGateway = new(cancelled);
        using FileOperationGateway refusedGateway = new(refused);

        FileOperationOutcome missingOutcome = await missingGateway.ExecuteAsync(CreateRequest(location, "child"), RecordingFileOperationProgress.Create(), CancellationToken.None);
        FileOperationOutcome cancelledOutcome = await cancelledGateway.ExecuteAsync(CreateRequest(location, "child"), RecordingFileOperationProgress.Create(), cancellation.Token);
        FileOperationOutcome refusedOutcome = await refusedGateway.ExecuteAsync(CreateRequest(location, "child"), RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.NotFound, missingOutcome.Failure);
        Assert.HasCount(1, missing.Calls);
        Assert.AreSame(FileOperationCompletionKind.Cancelled, cancelledOutcome.Completion);
        Assert.HasCount(1, cancelled.Calls);
        Assert.AreSame(FileOperationFailureKind.Conflict, refusedOutcome.Failure);
        Assert.IsEmpty(refusedOutcome.Effects);
        Assert.HasCount(2, refused.Calls);
    }

    /// <summary>Proves cancellation before admission starts no inspection or creation.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task ExecuteAsyncWhenCancelledBeforeFileCreationProducesNoEffectAsync()
    {
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        using FileOperationGateway gateway = new(port);
        RecordingFileOperationProgress progress = RecordingFileOperationProgress.Create();

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            CreateRequest(GatewayTestContext.ParsePath("C:\\location"), "notes.txt"), progress, new CancellationToken(true));

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.IsEmpty(outcome.Effects);
        Assert.IsEmpty(port.Calls);
        Assert.IsEmpty(progress.Reports);
    }

    /// <summary>Proves a successful admitted creation remains success when cancellation arrives during the step.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task ExecuteAsyncWhenCancelledAfterFileCreationRetainsSuccessAndProgressAsync()
    {
        FileSystemPath location = GatewayTestContext.ParsePath("C:\\location");
        using CancellationTokenSource cancellation = new();
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(ScriptedCallbackPoint.AfterFileCreation, cancellation.Cancel);
        port.EnqueueInspection(GatewayTestContext.Inspection(location, DeletionCapability.NotRequested));
        port.EnqueueFileCreation(ProviderStepOutcome.Succeeded());
        using FileOperationGateway gateway = new(port);
        RecordingFileOperationProgress progress = RecordingFileOperationProgress.Create();
        CreateTextFileRequest request = CreateRequest(location, "notes.txt");

        FileOperationOutcome outcome = await gateway.ExecuteAsync(request, progress, cancellation.Token);

        Assert.IsTrue(cancellation.IsCancellationRequested);
        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.HasCount(1, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.FileCreated, outcome.Effects[0].Kind);
        Assert.AreSame(request.Target, outcome.Effects[0].Source);
        Assert.HasCount(1, progress.Reports);
        Assert.AreEqual(FileOperationProgress.Create(1, 1), progress.Reports[0]);
        Assert.AreSame(FileInspectionPurpose.IdentityOnly, port.InspectionPurposes[0]);
        Assert.IsEmpty(port.DeletionModes);
    }

    /// <summary>Proves a pending creation owns the same gateway and rejects a second request without progress.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-014")]
    public async Task ExecuteAsyncWhenCreationOwnsGatewayRejectsSecondRequestAsync()
    {
        FileSystemPath location = GatewayTestContext.ParsePath("C:\\location");
        BlockingInspectionPort port = BlockingInspectionPort.Create(
            GatewayTestContext.Inspection(location, DeletionCapability.NotRequested));
        using FileOperationGateway gateway = new(port);
        RecordingFileOperationProgress firstProgress = RecordingFileOperationProgress.Create();
        RecordingFileOperationProgress secondProgress = RecordingFileOperationProgress.Create();
        CreateTextFileRequest request = CreateRequest(location, "notes.txt");

        Task<FileOperationOutcome> running = gateway.ExecuteAsync(request, firstProgress, CancellationToken.None);
        FileOperationOutcome rejected = await gateway.ExecuteAsync(request, secondProgress, CancellationToken.None);
        port.Release();
        FileOperationOutcome completed = await running;

        Assert.AreSame(FileOperationCompletionKind.Rejected, rejected.Completion);
        Assert.AreSame(FileOperationFailureKind.Reentrant, rejected.Failure);
        Assert.IsEmpty(rejected.Effects);
        Assert.IsEmpty(secondProgress.Reports);
        Assert.AreSame(FileOperationCompletionKind.Succeeded, completed.Completion);
        Assert.HasCount(1, completed.Effects);
        Assert.AreSame(FileOperationEffectKind.FileCreated, completed.Effects[0].Kind);
        Assert.HasCount(1, firstProgress.Reports);
    }

    private static CreateTextFileRequest CreateRequest(FileSystemPath location, string name)
    {
        return Assert.IsInstanceOfType<CreateTextFileRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(CreateTextFileRequest.Create(location, name)).Request);
    }
}
