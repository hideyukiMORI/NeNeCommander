using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves GatewayNameOperation behavior and boundaries.</summary>
[TestClass]
public sealed class GatewayNameOperationTests
{
    /// <summary>Proves a directory creation inspects the location, creates once, and reports one effect and full progress.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenDirectoryIsCreatedReportsEffectAndProgress()
    {
        FileSystemPath location = GatewayTestContext.ParsePath("C:\\location");
        CreateDirectoryRequest request = GatewayTestContext.CreateCreateDirectory(location, "child");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(GatewayTestContext.Inspection(location, DeletionCapability.PermanentOnly));
        port.EnqueueDirectoryCreation(ProviderStepOutcome.Succeeded());
        using FileOperationGateway gateway = new(port);
        RecordingFileOperationProgress progress = RecordingFileOperationProgress.Create();

        FileOperationOutcome outcome = await gateway.ExecuteAsync(request, progress, CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.HasCount(1, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.DirectoryCreated, outcome.Effects[0].Kind);
        Assert.AreSame(request.Target, outcome.Effects[0].Source);
        Assert.HasCount(2, port.Calls);
        Assert.AreEqual("Inspect:C:\\location", port.Calls[0]);
        Assert.AreEqual("CreateDirectory:C:\\location\\child", port.Calls[1]);
        Assert.HasCount(1, progress.Reports);
        Assert.AreEqual(FileOperationProgress.Create(1, 1), progress.Reports[0]);
    }

    /// <summary>Proves inspection failure, cancellation after inspection, and provider rejection each create nothing.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    [TestProperty("ThreatId", "ADV-018")]
    public async Task ExecuteAsyncWhenDirectoryCreationCannotProceedCreatesNothing()
    {
        FileSystemPath location = GatewayTestContext.ParsePath("C:\\location");
        using CancellationTokenSource cancellation = new();
        ScriptedFileOperationPort missing = ScriptedFileOperationPort.Create(null, null);
        missing.EnqueueInspection(FileInspectionOutcome.Failed(FileOperationFailureKind.NotFound));
        ScriptedFileOperationPort cancelled = ScriptedFileOperationPort.Create(ScriptedCallbackPoint.AfterInspection, cancellation.Cancel);
        cancelled.EnqueueInspection(GatewayTestContext.Inspection(location, DeletionCapability.PermanentOnly));
        ScriptedFileOperationPort refused = ScriptedFileOperationPort.Create(null, null);
        refused.EnqueueInspection(GatewayTestContext.Inspection(location, DeletionCapability.PermanentOnly));
        refused.EnqueueDirectoryCreation(ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict));
        using FileOperationGateway missingGateway = new(missing);
        using FileOperationGateway cancelledGateway = new(cancelled);
        using FileOperationGateway refusedGateway = new(refused);

        FileOperationOutcome missingOutcome = await missingGateway.ExecuteAsync(GatewayTestContext.CreateCreateDirectory(location, "child"), RecordingFileOperationProgress.Create(), CancellationToken.None);
        FileOperationOutcome cancelledOutcome = await cancelledGateway.ExecuteAsync(GatewayTestContext.CreateCreateDirectory(location, "child"), RecordingFileOperationProgress.Create(), cancellation.Token);
        FileOperationOutcome refusedOutcome = await refusedGateway.ExecuteAsync(GatewayTestContext.CreateCreateDirectory(location, "child"), RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.NotFound, missingOutcome.Failure);
        Assert.HasCount(1, missing.Calls);
        Assert.AreSame(FileOperationCompletionKind.Cancelled, cancelledOutcome.Completion);
        Assert.HasCount(1, cancelled.Calls);
        Assert.AreSame(FileOperationFailureKind.Conflict, refusedOutcome.Failure);
        Assert.IsEmpty(refusedOutcome.Effects);
        Assert.HasCount(2, refused.Calls);
    }

    /// <summary>Proves a rename inspects the source, renames once, and reports one effect and full progress.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenEntryIsRenamedReportsEffectAndProgress()
    {
        FileSystemPath source = GatewayTestContext.ParsePath("C:\\location\\before.txt");
        RenameRequest request = GatewayTestContext.CreateRename(source, "after.txt");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(GatewayTestContext.Inspection(source, DeletionCapability.PermanentOnly));
        port.EnqueueRename(ProviderStepOutcome.Succeeded());
        using FileOperationGateway gateway = new(port);
        RecordingFileOperationProgress progress = RecordingFileOperationProgress.Create();

        FileOperationOutcome outcome = await gateway.ExecuteAsync(request, progress, CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.HasCount(1, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.Renamed, outcome.Effects[0].Kind);
        Assert.AreSame(request.Source, outcome.Effects[0].Source);
        Assert.HasCount(2, port.Calls);
        Assert.AreEqual("Inspect:C:\\location\\before.txt", port.Calls[0]);
        Assert.AreEqual("Rename:C:\\location\\before.txt>C:\\location\\after.txt", port.Calls[1]);
        Assert.HasCount(1, progress.Reports);
        Assert.AreEqual(FileOperationProgress.Create(1, 1), progress.Reports[0]);
    }

    /// <summary>Proves inspection failure, cancellation after inspection, and provider rejection each rename nothing.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    [TestProperty("ThreatId", "ADV-018")]
    public async Task ExecuteAsyncWhenRenameCannotProceedRenamesNothing()
    {
        FileSystemPath source = GatewayTestContext.ParsePath("C:\\location\\before.txt");
        using CancellationTokenSource cancellation = new();
        ScriptedFileOperationPort missing = ScriptedFileOperationPort.Create(null, null);
        missing.EnqueueInspection(FileInspectionOutcome.Failed(FileOperationFailureKind.NotFound));
        ScriptedFileOperationPort cancelled = ScriptedFileOperationPort.Create(ScriptedCallbackPoint.AfterInspection, cancellation.Cancel);
        cancelled.EnqueueInspection(GatewayTestContext.Inspection(source, DeletionCapability.PermanentOnly));
        ScriptedFileOperationPort refused = ScriptedFileOperationPort.Create(null, null);
        refused.EnqueueInspection(GatewayTestContext.Inspection(source, DeletionCapability.PermanentOnly));
        refused.EnqueueRename(ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict));
        using FileOperationGateway missingGateway = new(missing);
        using FileOperationGateway cancelledGateway = new(cancelled);
        using FileOperationGateway refusedGateway = new(refused);

        FileOperationOutcome missingOutcome = await missingGateway.ExecuteAsync(GatewayTestContext.CreateRename(source, "after.txt"), RecordingFileOperationProgress.Create(), CancellationToken.None);
        FileOperationOutcome cancelledOutcome = await cancelledGateway.ExecuteAsync(GatewayTestContext.CreateRename(source, "after.txt"), RecordingFileOperationProgress.Create(), cancellation.Token);
        FileOperationOutcome refusedOutcome = await refusedGateway.ExecuteAsync(GatewayTestContext.CreateRename(source, "after.txt"), RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.NotFound, missingOutcome.Failure);
        Assert.HasCount(1, missing.Calls);
        Assert.AreSame(FileOperationCompletionKind.Cancelled, cancelledOutcome.Completion);
        Assert.HasCount(1, cancelled.Calls);
        Assert.AreSame(FileOperationFailureKind.Conflict, refusedOutcome.Failure);
        Assert.IsEmpty(refusedOutcome.Effects);
        Assert.HasCount(2, refused.Calls);
    }
}
