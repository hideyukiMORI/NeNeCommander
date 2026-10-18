using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves GatewayDeletion behavior and boundaries.</summary>
[TestClass]
public sealed class GatewayDeletionTests
{
    /// <summary>Proves permanent deletion requires exact confirmation.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-008")]
    public async Task ExecuteAsyncWhenPermanentDeleteIsUnconfirmedRejectsWithoutMutation()
    {
        FileSystemPath path = GatewayTestContext.ParsePath("\\\\server\\share\\source");
        DeleteRequest request = GatewayTestContext.CreateDelete([path], null);
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(GatewayTestContext.Inspection(path, DeletionCapability.PermanentOnly));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(request, RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Rejected, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.ConfirmationRequired, outcome.Failure);
        Assert.HasCount(1, port.Calls);
    }

    /// <summary>Proves one permanent-only source makes a mixed delete batch require confirmation.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-008")]
    public async Task ExecuteAsyncWhenMixedDeleteBatchIsUnconfirmedRejectsWithoutMutation()
    {
        FileSystemPath recyclable = GatewayTestContext.ParsePath("C:\\recyclable");
        FileSystemPath permanent = GatewayTestContext.ParsePath("\\\\server\\share\\permanent");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(GatewayTestContext.Inspection(recyclable, DeletionCapability.Recycle));
        port.EnqueueInspection(GatewayTestContext.Inspection(permanent, DeletionCapability.PermanentOnly));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            GatewayTestContext.CreateDelete([recyclable, permanent], null),
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ConfirmationRequired, outcome.Failure);
        Assert.IsEmpty(outcome.Effects);
        Assert.HasCount(2, port.Calls);
    }

    /// <summary>Proves confirmed permanent deletion uses the permanent provider mode.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenPermanentDeleteIsConfirmedUsesPermanentMode()
    {
        FileSystemPath path = GatewayTestContext.ParsePath("\\\\server\\share\\source");
        DeleteRequest unconfirmed = GatewayTestContext.CreateDelete([path], null);
        PermanentDeletionConfirmation confirmation = PermanentDeletionConfirmation.CreateFor(unconfirmed);
        DeleteRequest confirmed = GatewayTestContext.CreateDelete([path], confirmation);
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(GatewayTestContext.Inspection(path, DeletionCapability.PermanentOnly));
        port.EnqueueDeletion(ProviderStepOutcome.Succeeded());
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(confirmed, RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.AreSame(FileOperationEffectKind.PermanentlyDeleted, outcome.Effects[0].Kind);
        Assert.AreEqual("Delete:" + path.CanonicalText, port.Calls[1]);
    }

    /// <summary>Proves recycle capability avoids permanent deletion.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenRecycleIsSupportedUsesRecycleModeWithoutConfirmation()
    {
        FileSystemPath path = GatewayTestContext.ParsePath("C:\\source");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(GatewayTestContext.Inspection(path, DeletionCapability.Recycle));
        port.EnqueueDeletion(ProviderStepOutcome.Succeeded());
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(GatewayTestContext.CreateDelete([path], null), RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.AreSame(FileOperationEffectKind.Recycled, outcome.Effects[0].Kind);
        Assert.AreEqual("Recycle:" + path.CanonicalText, port.Calls[1]);
    }

    /// <summary>Reports a completed recycle even when the provider subsequently fails.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenRecycleCompletesBeforeFailureRetainsEffect()
    {
        FileSystemPath path = GatewayTestContext.ParsePath("C:\\source");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(GatewayTestContext.Inspection(path, DeletionCapability.Recycle));
        port.EnqueueDeletion(ProviderStepOutcome.FailedAfterEffect(FileOperationFailureKind.Delete, ProviderStepEffectKind.Recycled));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(GatewayTestContext.CreateDelete([path], null), RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.PartiallyCompleted, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.Delete, outcome.Failure);
        Assert.HasCount(1, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.Recycled, outcome.Effects[0].Kind);
        Assert.HasCount(2, port.Calls);
    }

    /// <summary>Normalizes cancellation observed by the provider before a recycle effect.</summary>
    [TestMethod]
    [DataRow("None")]
    [DataRow("Confirmed")]
    public async Task ExecuteAsyncWhenRecycleIsCancelledReportsOnlyConfirmedEffects(string receipt)
    {
        using CancellationTokenSource cancellation = new();
        FileSystemPath path = GatewayTestContext.ParsePath("C:\\source");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(ScriptedCallbackPoint.AfterDeletion, cancellation.Cancel);
        port.EnqueueInspection(GatewayTestContext.Inspection(path, DeletionCapability.Recycle));
        port.EnqueueDeletion(receipt == "Confirmed"
            ? ProviderStepOutcome.FailedAfterEffect(FileOperationFailureKind.Delete, ProviderStepEffectKind.Recycled)
            : ProviderStepOutcome.Failed(FileOperationFailureKind.Delete));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(GatewayTestContext.CreateDelete([path], null), RecordingFileOperationProgress.Create(), cancellation.Token);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.HasCount(receipt == "Confirmed" ? 1 : 0, outcome.Effects);
        if (receipt == "Confirmed")
        {
            Assert.AreSame(FileOperationEffectKind.Recycled, outcome.Effects[0].Kind);
        }
        Assert.IsNull(outcome.Failure);
    }

    /// <summary>Proves delete inspection cancellation stops before the next source.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenDeleteInspectionIsCancelledStopsBeforeNextSource()
    {
        using CancellationTokenSource cancellation = new();
        FileSystemPath first = GatewayTestContext.ParsePath("C:\\first");
        FileSystemPath second = GatewayTestContext.ParsePath("C:\\second");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(
            ScriptedCallbackPoint.AfterInspection,
            cancellation.Cancel);
        port.EnqueueInspection(GatewayTestContext.Inspection(first, DeletionCapability.Recycle));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            GatewayTestContext.CreateDelete([first, second], null),
            RecordingFileOperationProgress.Create(),
            cancellation.Token);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.HasCount(1, port.Calls);
    }

    /// <summary>Proves delete inspection failure starts no mutation.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenDeleteInspectionFailsStartsNoMutation()
    {
        FileSystemPath path = GatewayTestContext.ParsePath("C:\\source");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(FileInspectionOutcome.Failed(FileOperationFailureKind.NotFound));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            GatewayTestContext.CreateDelete([path], null),
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.NotFound, outcome.Failure);
        Assert.IsEmpty(outcome.Effects);
        Assert.HasCount(1, port.Calls);
    }

    /// <summary>Proves delete cancellation between sources starts no second deletion.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenDeleteCancellationArrivesBetweenSourcesStopsBatch()
    {
        using CancellationTokenSource cancellation = new();
        FileSystemPath first = GatewayTestContext.ParsePath("C:\\first");
        FileSystemPath second = GatewayTestContext.ParsePath("C:\\second");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(
            ScriptedCallbackPoint.AfterDeletion,
            cancellation.Cancel);
        port.EnqueueInspection(GatewayTestContext.Inspection(first, DeletionCapability.Recycle));
        port.EnqueueInspection(GatewayTestContext.Inspection(second, DeletionCapability.Recycle));
        port.EnqueueDeletion(ProviderStepOutcome.Succeeded());
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            GatewayTestContext.CreateDelete([first, second], null),
            RecordingFileOperationProgress.Create(),
            cancellation.Token);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.HasCount(1, outcome.Effects);
        Assert.HasCount(3, port.Calls);
    }

    /// <summary>Proves provider deletion failure returns an exact rejection.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenDeleteFailsReturnsProviderFailure()
    {
        FileSystemPath path = GatewayTestContext.ParsePath("C:\\source");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(GatewayTestContext.Inspection(path, DeletionCapability.Recycle));
        port.EnqueueDeletion(ProviderStepOutcome.Failed(FileOperationFailureKind.AccessDenied));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            GatewayTestContext.CreateDelete([path], null),
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.AccessDenied, outcome.Failure);
        Assert.IsEmpty(outcome.Effects);
    }

    /// <summary>Proves confirmation cannot authorize a different source count or ordering.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenPermanentConfirmationDoesNotMatchExactSourcesRejectsMutation()
    {
        FileSystemPath first = GatewayTestContext.ParsePath("\\\\server\\share\\first");
        FileSystemPath second = GatewayTestContext.ParsePath("\\\\server\\share\\second");
        DeleteRequest oneSource = GatewayTestContext.CreateDelete([first], null);
        DeleteRequest ordered = GatewayTestContext.CreateDelete([first, second], null);

        FileOperationOutcome countMismatch = await ExecutePermanentDeleteAsync(
            [first, second],
            PermanentDeletionConfirmation.CreateFor(oneSource));
        FileOperationOutcome orderMismatch = await ExecutePermanentDeleteAsync(
            [second, first],
            PermanentDeletionConfirmation.CreateFor(ordered));

        Assert.AreSame(FileOperationFailureKind.ConfirmationRequired, countMismatch.Failure);
        Assert.AreSame(FileOperationFailureKind.ConfirmationRequired, orderMismatch.Failure);
    }

    private static async Task<FileOperationOutcome> ExecutePermanentDeleteAsync(
        FileSystemPath[] sources,
        PermanentDeletionConfirmation confirmation)
    {
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        foreach (FileSystemPath source in sources)
        {
            port.EnqueueInspection(GatewayTestContext.Inspection(source, DeletionCapability.PermanentOnly));
        }
        using FileOperationGateway gateway = new(port);
        return await gateway.ExecuteAsync(GatewayTestContext.CreateDelete(sources, confirmation), RecordingFileOperationProgress.Create(), CancellationToken.None);
    }
}
